using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using PlayFab;
using PlayFab.GroupsModels;
using PlayFab.ProfilesModels;
using PlayFab.AuthenticationModels;  // 추가: GetEntityTokenRequest 등
using CommonLibrary;

namespace Guild
{
    public class GetGuildInfo
    {
        private readonly ILogger<GetGuildInfo> _logger;

        public GetGuildInfo(ILogger<GetGuildInfo> logger)
        {
            _logger = logger;
        }

        [Function("GetGuildInfo")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("[GetGuildInfo] Function invoked.");

                // 1) 요청 바디 읽기
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation($"[GetGuildInfo] Request body: {requestBody}");

                // 2) JSON 파싱
                var input = JsonConvert.DeserializeObject<GetGuildInfoRequest>(requestBody);
                if (input == null || string.IsNullOrEmpty(input.PlayFabId))
                {
                    _logger.LogWarning("[GetGuildInfo] playFabId missing in request.");
                    return new BadRequestObjectResult("Invalid request: playFabId is required.");
                }

                // entityToken 체크
                if (input.EntityToken == null
                    || string.IsNullOrEmpty(input.EntityToken.EntityToken)
                    || input.EntityToken.Entity == null
                    || string.IsNullOrEmpty(input.EntityToken.Entity.Id)
                    || string.IsNullOrEmpty(input.EntityToken.Entity.Type))
                {
                    _logger.LogWarning("[GetGuildInfo] entityToken is missing or incomplete.");
                    return new BadRequestObjectResult("Invalid request: entityToken is required.");
                }

                // 값 추출
                string clientEntityToken = input.EntityToken.EntityToken;
                string entityId = input.EntityToken.Entity.Id;
                string entityType = input.EntityToken.Entity.Type;

                // 3) PlayFab 초기 설정 (SecretKey, TitleId 등)
                PlayFabConfig.Configure();
                _logger.LogInformation("[GetGuildInfo] PlayFab configured.");

                // 4) 클라이언트 토큰을 기반으로 AuthenticationContext 준비 (멤버 조회용)
                var clientAuthContext = new PlayFabAuthenticationContext
                {
                    EntityId = entityId,
                    EntityType = entityType,
                    EntityToken = clientEntityToken
                };

                // 5) ListMembership: 이 엔터티가 속한 그룹 조회
                _logger.LogInformation($"[GetGuildInfo] Listing memberships for entityId: {entityId}");
                var listMembershipReq = new ListMembershipRequest
                {
                    AuthenticationContext = clientAuthContext,
                    Entity = new PlayFab.GroupsModels.EntityKey
                    {
                        Id = entityId,
                        Type = entityType
                    }
                };
                var listMembershipRes = await PlayFabGroupsAPI.ListMembershipAsync(listMembershipReq);
                if (listMembershipRes.Error != null)
                {
                    _logger.LogError($"[GetGuildInfo] ListMembership error: {listMembershipRes.Error.GenerateErrorReport()}");
                    return new BadRequestObjectResult("ListMembership 오류");
                }

                var userGroups = listMembershipRes.Result.Groups;
                if (userGroups == null || userGroups.Count == 0)
                {
                    // 길드 미가입 상태
                    _logger.LogInformation("[GetGuildInfo] No groups found for the entity.");
                    var noGuildResp = new GuildInfoResponse
                    {
                        IsJoined = false,
                        GroupId = null,
                        GroupName = null,
                        MemberList = null
                    };

                    string debugJson = JsonConvert.SerializeObject(noGuildResp);
                    _logger.LogInformation("[GetGuildInfo] Final response JSON (no guild): " + debugJson);

                    return new OkObjectResult(noGuildResp);
                }

                // 첫 번째 그룹만 사용
                var firstGroup = userGroups[0];
                string groupId = firstGroup.Group.Id;
                string groupName = firstGroup.GroupName ?? "(NoName)";
                _logger.LogInformation($"[GetGuildInfo] Selected group: groupId = {groupId}, groupName = {groupName}");

                // 6) ListGroupMembers: 그룹 멤버 조회 (클라이언트 토큰 사용)
                _logger.LogInformation($"[GetGuildInfo] Listing group members for groupId: {groupId}");
                var listMembersGroupReq = new ListGroupMembersRequest
                {
                    AuthenticationContext = clientAuthContext,
                    Group = new PlayFab.GroupsModels.EntityKey { Id = groupId }
                };
                var listMembersGroupRes = await PlayFabGroupsAPI.ListGroupMembersAsync(listMembersGroupReq);
                if (listMembersGroupRes.Error != null || listMembersGroupRes.Result.Members == null)
                {
                    _logger.LogWarning("[GetGuildInfo] No group member info found.");
                    var emptyMemberResp = new GuildInfoResponse
                    {
                        IsJoined = true,
                        GroupId = groupId,
                        GroupName = groupName,
                        MemberList = new List<GuildMemberData>()
                    };

                    string debugJson = JsonConvert.SerializeObject(emptyMemberResp);
                    _logger.LogInformation("[GetGuildInfo] Final response JSON (no members): " + debugJson);

                    return new OkObjectResult(emptyMemberResp);
                }

                var roleBlocks = listMembersGroupRes.Result.Members;
                _logger.LogInformation($"[GetGuildInfo] Retrieved {roleBlocks.Count} role blocks.");

                // (A) 그룹 멤버들의 엔터티 ID 목록 추출
                var entityKeySet = new HashSet<PlayFab.GroupsModels.EntityKey>();
                foreach (var roleBlock in roleBlocks)
                {
                    if (roleBlock.Members == null) continue;
                    foreach (var ewl in roleBlock.Members)
                    {
                        if (ewl?.Key?.Id != null)
                            entityKeySet.Add(ewl.Key);
                    }
                }
                _logger.LogInformation($"[GetGuildInfo] Total unique entity keys: {entityKeySet.Count}");

                // 7) 이제 프로필 API 호출 시 클라이언트 토큰 대신 서버 토큰 사용
                // 2) 서버 토큰 획득
                var tokenRequest = new GetEntityTokenRequest();
                var tokenResult = await PlayFabAuthenticationAPI.GetEntityTokenAsync(tokenRequest);
                if (tokenResult.Error != null)
                {
                    _logger.LogError("[GetGuildInfo] Error fetching server entity token: " + tokenResult.Error.GenerateErrorReport());
                    return new BadRequestObjectResult("Error fetching server entity token");
                }
                var serverAuthContext = new PlayFabAuthenticationContext
                {
                    EntityId = tokenResult.Result.Entity.Id,
                    EntityType = tokenResult.Result.Entity.Type,
                    EntityToken = tokenResult.Result.EntityToken
                };

                // 3) Profiles API 호출 (서버 토큰 사용) → DisplayName + Lineage 조회
                var getProfilesReq = new GetEntityProfilesRequest
                {
                    AuthenticationContext = serverAuthContext,
                    Entities = entityKeySet.Select(k => new PlayFab.ProfilesModels.EntityKey
                    {
                        Id = k.Id,
                        Type = k.Type
                    }).ToList()
                };
                _logger.LogInformation("[GetGuildInfo] Calling GetProfilesAsync with server token...");
                var getProfilesRes = await PlayFabProfilesAPI.GetProfilesAsync(getProfilesReq);
                if (getProfilesRes.Error != null || getProfilesRes.Result?.Profiles == null)
                {
                    _logger.LogError($"[GetGuildInfo] GetProfilesAsync error: {getProfilesRes.Error?.GenerateErrorReport()}");
                    return new BadRequestObjectResult("GetProfiles 오류");
                }

                // 4) 프로필에서 DisplayName과 MasterPlayerAccount Id를 추출
                //    entityIdToDisplayName:  엔터티 ID → DisplayName
                //    entityIdToMasterId:     엔터티 ID → 실제 PlayFabId (Lineage.MasterPlayerAccount.Id)
                var entityIdToDisplayName = new Dictionary<string, string>();
                var entityIdToMasterId = new Dictionary<string, string?>();
                foreach (var profile in getProfilesRes.Result.Profiles)
                {
                    string eId = profile.Entity.Id;
                    string disp = profile.DisplayName;
                    entityIdToDisplayName[eId] = !string.IsNullOrEmpty(disp) ? disp : eId;

                    // Lineage에서 마스터 플레이어 계정 ID 추출
                    string? masterId = profile.Lineage?.MasterPlayerAccountId;
                    if (!string.IsNullOrEmpty(masterId))
                    {
                        entityIdToMasterId[eId] = masterId;
                        _logger.LogInformation($"[GetGuildInfo] {eId} → MasterPlayerId: {masterId}");
                    }
                    else
                    {
                        // 없는 경우 fallback 처리 (엔터티 ID가 곧 PlayFabId는 아님)
                        _logger.LogWarning($"[GetGuildInfo] {eId}: MasterPlayerAccount Id가 없습니다.");
                        entityIdToMasterId[eId] = null;
                    }
                }

                // 5) UserData 조회 (MasterPlayerAccount Id 기준)
                var additionalUserData = new Dictionary<string, (int UserPower, bool IsOnline)>();
                foreach (var eKey in entityKeySet)
                {
                    string eId = eKey.Id;

                    // 마스터 플레이어 ID가 있으면 그걸 사용, 없으면 UserData 조회 불가 → 기본값
                    string? masterId = entityIdToMasterId.ContainsKey(eId) ? entityIdToMasterId[eId] : null;
                    if (string.IsNullOrEmpty(masterId))
                    {
                        additionalUserData[eId] = (0, false);
                        continue;
                    }

                    // GetUserData 호출
                    var getUserDataReq = new PlayFab.ServerModels.GetUserDataRequest
                    {
                        PlayFabId = masterId,
                        Keys = new List<string> { "UserPower", "isOnline" }
                    };
                    var getUserDataRes = await PlayFabServerAPI.GetUserDataAsync(getUserDataReq);
                    if (getUserDataRes.Error != null)
                    {
                        _logger.LogWarning($"[GetGuildInfo] GetUserData 실패 for {masterId}: {getUserDataRes.Error.GenerateErrorReport()}");
                        additionalUserData[eId] = (0, false);
                    }
                    else
                    {
                        int userPower = 0;
                        bool isOnline = false;
                        if (getUserDataRes.Result.Data != null)
                        {
                            if (getUserDataRes.Result.Data.ContainsKey("UserPower"))
                                int.TryParse(getUserDataRes.Result.Data["UserPower"].Value, out userPower);

                            if (getUserDataRes.Result.Data.ContainsKey("isOnline"))
                                bool.TryParse(getUserDataRes.Result.Data["isOnline"].Value, out isOnline);
                        }
                        additionalUserData[eId] = (userPower, isOnline);
                    }
                }

                // 6) 최종 멤버 리스트 구성
                var memberList = new List<GuildMemberData>();
                foreach (var roleBlock in roleBlocks)
                {
                    string roleName = roleBlock.RoleName ?? "(NoRoleName)";
                    if (roleBlock.Members == null) continue;

                    foreach (var ewl in roleBlock.Members)
                    {
                        string eId = ewl.Key.Id;
                        // DisplayName
                        string finalDisplayName = entityIdToDisplayName.ContainsKey(eId)
                            ? entityIdToDisplayName[eId]
                            : eId;

                        // Power, isOnline
                        int userPower = 0;
                        bool isOnline = false;
                        if (additionalUserData.ContainsKey(eId))
                        {
                            userPower = additionalUserData[eId].UserPower;
                            isOnline = additionalUserData[eId].IsOnline;
                        }

                        memberList.Add(new GuildMemberData
                        {
                            EntityId = eId,
                            Role = roleName,
                            DisplayName = finalDisplayName,
                            Power = userPower,
                            IsOnline = isOnline
                        });
                    }
                }
                _logger.LogInformation($"[GetGuildInfo] Final member list count: {memberList.Count}");


                // 8) 최종 응답 구성
                var resp = new GuildInfoResponse
                {
                    IsJoined = true, 
                    GroupId = groupId,
                    GroupName = groupName,
                    MemberList = memberList
                };

                string finalJson = JsonConvert.SerializeObject(resp);
                _logger.LogInformation("[GetGuildInfo] Final response JSON: " + finalJson);

                return new OkObjectResult(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[GetGuildInfo] Exception: {ex.Message}\n{ex.StackTrace}");
                return new BadRequestObjectResult("Internal server error in GetGuildInfo.");
            }
        }
    }

    // ------------------------------------------------------------
    // [REQUEST DTO]
    // ------------------------------------------------------------
    public class GetGuildInfoRequest
    {
        public string? PlayFabId { get; set; }
        public EntityTokenData? EntityToken { get; set; }
    }

    public class EntityTokenData
    {
        public EntityKeyData? Entity { get; set; }
        public string? EntityToken { get; set; }
        public DateTime? TokenExpiration { get; set; }
    }

    public class EntityKeyData
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
    }

    // ------------------------------------------------------------
    // [RESPONSE DTO]
    // ------------------------------------------------------------
    public class GuildInfoResponse
    {
        public bool IsJoined { get; set; }
        public string? GroupId { get; set; }
        public string? GroupName { get; set; }
        public List<GuildMemberData>? MemberList { get; set; }
    }

    public class GuildMemberData
    {
        public string? EntityId { get; set; }
        public string? Role { get; set; }
        public string? DisplayName { get; set; }
        public int Power { get; set; }
        public bool IsOnline { get; set; }
    }
}
