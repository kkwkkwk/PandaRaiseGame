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
using PlayFab.AuthenticationModels;
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
                string entityTokenFromClient = input.EntityToken.EntityToken;
                string entityId = input.EntityToken.Entity.Id;
                string entityType = input.EntityToken.Entity.Type;

                // 3) PlayFab 초기 설정 (SecretKey, TitleId 등)
                PlayFabConfig.Configure();
                _logger.LogInformation("[GetGuildInfo] PlayFab configured.");

                // 4) AuthenticationContext 준비
                var authContext = new PlayFabAuthenticationContext
                {
                    EntityId = entityId,
                    EntityType = entityType,
                    EntityToken = entityTokenFromClient
                };

                // 5) ListMembership
                _logger.LogInformation($"[GetGuildInfo] Listing memberships for entityId: {entityId}");
                var listMembershipReq = new ListMembershipRequest
                {
                    AuthenticationContext = authContext,
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

                    // (디버그) 서버에서 직렬화된 JSON 확인
                    string debugJson = JsonConvert.SerializeObject(noGuildResp);
                    _logger.LogInformation("[GetGuildInfo] Final response JSON (no guild): " + debugJson);

                    return new OkObjectResult(noGuildResp);
                }

                // 첫 번째 그룹만 사용
                var firstGroup = userGroups[0];
                string groupId = firstGroup.Group.Id;
                string groupName = firstGroup.GroupName ?? "(NoName)";
                _logger.LogInformation($"[GetGuildInfo] Selected group: groupId = {groupId}, groupName = {groupName}");

                // 6) ListGroupMembers
                _logger.LogInformation($"[GetGuildInfo] Listing group members for groupId: {groupId}");
                var listMembersGroupReq = new ListGroupMembersRequest
                {
                    AuthenticationContext = authContext,
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

                    // (디버그)
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

                // 7) Profiles API (DisplayName 등 조회)
                var getProfilesReq = new GetEntityProfilesRequest
                {
                    AuthenticationContext = authContext,
                    Entities = entityKeySet.Select(k => new PlayFab.ProfilesModels.EntityKey
                    {
                        Id = k.Id,
                        Type = k.Type
                    }).ToList()
                };
                _logger.LogInformation("[GetGuildInfo] Calling GetProfilesAsync...");
                var getProfilesRes = await PlayFabProfilesAPI.GetProfilesAsync(getProfilesReq);
                if (getProfilesRes.Error != null || getProfilesRes.Result?.Profiles == null)
                {
                    _logger.LogError($"[GetGuildInfo] GetProfilesAsync error: {getProfilesRes.Error?.GenerateErrorReport()}");
                    return new BadRequestObjectResult("GetProfiles 오류");
                }

                // 엔터티 ID -> displayName 맵
                var entityIdToDisplayName = new Dictionary<string, string>();
                foreach (var profile in getProfilesRes.Result.Profiles)
                {
                    string eId = profile.Entity.Id;
                    string disp = profile.DisplayName;
                    if (!string.IsNullOrEmpty(eId))
                        entityIdToDisplayName[eId] = !string.IsNullOrEmpty(disp) ? disp : eId;
                }

                // (B) 최종 멤버 리스트 생성
                var memberList = new List<GuildMemberData>();
                foreach (var roleBlock in roleBlocks)
                {
                    string roleName = roleBlock.RoleName ?? "(NoRoleName)";
                    if (roleBlock.Members == null) continue;

                    foreach (var ewl in roleBlock.Members)
                    {
                        string eId = ewl.Key.Id;
                        string finalDisplayName = entityIdToDisplayName.ContainsKey(eId)
                            ? entityIdToDisplayName[eId]
                            : eId;

                        memberList.Add(new GuildMemberData
                        {
                            EntityId = eId,
                            Role = roleName,
                            DisplayName = finalDisplayName,
                            Power = 0 // 필요 시 수정
                        });
                    }
                }
                _logger.LogInformation($"[GetGuildInfo] Final member list count: {memberList.Count}");

                // 8) 최종 응답
                var resp = new GuildInfoResponse
                {
                    IsJoined = true,
                    GroupId = groupId,
                    GroupName = groupName,
                    MemberList = memberList
                };

                // (디버그) 직렬화된 결과를 서버 로그에 남기기
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
    // *요청 DTO도 가능하면 프로퍼티 사용 권장. (필드는 무시될 수 있음)
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
    }
}
