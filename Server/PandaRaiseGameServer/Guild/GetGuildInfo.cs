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
using PlayFab.ProfilesModels; // Profiles API
using PlayFab.ServerModels;
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
            // 1) 요청 바디 파싱
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<GetGuildInfoRequest>(requestBody);
            if (input == null || string.IsNullOrEmpty(input.playFabId))
            {
                return new BadRequestObjectResult("Invalid request: playFabId is required.");
            }

            // 2) PlayFab 설정
            PlayFabConfig.Configure();

            // 3) playFabId -> entityId 가져오기 (GetUserAccountInfo)
            var accountInfoRes = await PlayFabServerAPI.GetUserAccountInfoAsync(
                new GetUserAccountInfoRequest { PlayFabId = input.playFabId }
            );
            if (accountInfoRes.Error != null ||
                accountInfoRes.Result.UserInfo?.TitleInfo?.TitlePlayerAccount == null)
            {
                _logger.LogWarning("[GetGuildInfo] title_player_account가 없음. 미로그인 유저?");
                return new OkObjectResult(new GuildInfoResponse { isJoined = false });
            }

            string entityId = accountInfoRes.Result.UserInfo.TitleInfo.TitlePlayerAccount.Id;
            _logger.LogInformation($"[GetGuildInfo] entityId = {entityId}");

            // 4) ListMembership: 이 엔터티가 속한 그룹(길드) 조회
            var listMembershipReq = new ListMembershipRequest
            {
                Entity = new PlayFab.GroupsModels.EntityKey
                {
                    Id = entityId,
                    Type = "title_player_account"
                }
            };
            var listMembershipRes = await PlayFabGroupsAPI.ListMembershipAsync(listMembershipReq);
            if (listMembershipRes.Error != null)
            {
                _logger.LogError("[GetGuildInfo] ListMembership 오류: " + listMembershipRes.Error.GenerateErrorReport());
                return new BadRequestObjectResult("ListMembership 오류");
            }

            var userGroups = listMembershipRes.Result.Groups;
            if (userGroups == null || userGroups.Count == 0)
            {
                // 길드 미가입
                var noGuildResp = new GuildInfoResponse
                {
                    isJoined = false,
                    groupId = null,
                    groupName = null,
                    memberList = null
                };
                return new OkObjectResult(noGuildResp);
            }

            // (예시) 첫 번째 그룹만
            var firstGroup = userGroups[0];
            string groupId = firstGroup.Group.Id;
            string groupName = firstGroup.GroupName ?? "(NoName)";

            // 5) ListGroupMembers: 결과는 List<EntityMemberRole>
            var listMembersGroupReq = new ListGroupMembersRequest
            {
                Group = new PlayFab.GroupsModels.EntityKey { Id = groupId }
            };
            var listMembersGroupRes = await PlayFabGroupsAPI.ListGroupMembersAsync(listMembersGroupReq);
            if (listMembersGroupRes.Error != null || listMembersGroupRes.Result.Members == null)
            {
                _logger.LogWarning("[GetGuildInfo] 그룹 멤버(역할) 정보 없음?");
                var emptyResp = new GuildInfoResponse
                {
                    isJoined = true,
                    groupId = groupId,
                    groupName = groupName,
                    memberList = new List<GuildMemberData>()
                };
                return new OkObjectResult(emptyResp);
            }

            // "roleBlocks" = List<EntityMemberRole>
            var roleBlocks = listMembersGroupRes.Result.Members;

            // ========== (A) Flatten 엔터티 ID 목록 추출 ==========
            // 여러 역할에 속한 엔터티가 중복될 수 있음 => 합칠지, 중복 OK인지 정책 결정
            var entityKeySet = new HashSet<PlayFab.GroupsModels.EntityKey>();
            // HashSet으로 중복 제거 (엔티티 중복 가입 시 1회만 프로필 조회)

            foreach (var roleBlock in roleBlocks)
            {
                if (roleBlock.Members != null)
                {
                    foreach (var ewl in roleBlock.Members) // ewl = EntityWithLineage
                    {
                        entityKeySet.Add(ewl.Key);
                    }
                }
            }

            // 6) Profiles API로 엔티티 프로필 가져오기 => DisplayName
            var getProfilesReq = new GetEntityProfilesRequest
            {
                Entities = entityKeySet.Select(k => new PlayFab.ProfilesModels.EntityKey
                {
                    Id = k.Id,
                    Type = k.Type
                }).ToList()
            };
            var getProfilesRes = await PlayFabProfilesAPI.GetProfilesAsync(getProfilesReq);
            if (getProfilesRes.Error != null || getProfilesRes.Result.Profiles == null)
            {
                _logger.LogError("[GetGuildInfo] GetProfilesAsync 오류: " + getProfilesRes.Error.GenerateErrorReport());
                return new BadRequestObjectResult("GetProfiles 오류");
            }

            // entityId -> displayName 매핑
            var entityIdToDisplayName = new Dictionary<string, string>();
            foreach (var profile in getProfilesRes.Result.Profiles)
            {
                string? eId = profile.Entity?.Id;
                // profile.DisplayName: 프로필에 설정된 표시 이름(없을 수도 있음)
                // NOTE: 실제로 DisplayName을 채우려면 "SetEntityProfilePolicy" 등으로 권한을 열고 
                //       "SetProfile"에서 DisplayName을 설정해두어야 할 수도 있음
                string? disp = profile.DisplayName;
                if (!string.IsNullOrEmpty(eId))
                {
                    entityIdToDisplayName[eId] = !string.IsNullOrEmpty(disp) ? disp : eId;
                }
            }

            // ========== (B) 역할 블록 순회하여 실제 멤버 목록 구성 ==========
            var memberList = new List<GuildMemberData>();
            foreach (var roleBlock in roleBlocks)
            {
                string roleName = roleBlock.RoleName ?? "(NoRoleName)";
                if (roleBlock.Members == null) continue;

                foreach (var ewl in roleBlock.Members)
                {
                    string eId = ewl.Key.Id;
                    // displayName 매핑
                    string finalDisplayName = entityIdToDisplayName.ContainsKey(eId)
                        ? entityIdToDisplayName[eId]
                        : eId;

                    memberList.Add(new GuildMemberData
                    {
                        entityId = eId,
                        role = roleName,
                        displayName = finalDisplayName,
                        power = 0 // 일단 생략
                    });
                }
            }

            // 7) 최종 응답
            var resp = new GuildInfoResponse
            {
                isJoined = true,
                groupId = groupId,
                groupName = groupName,
                memberList = memberList
            };

            return new OkObjectResult(resp);
        }
    }

    // -----------------------
    // DTO
    // -----------------------

    public class GetGuildInfoRequest
    {
        public string? playFabId;
    }

    public class GuildInfoResponse
    {
        public bool isJoined;
        public string? groupId;
        public string? groupName;
        public List<GuildMemberData>? memberList;
    }

    public class GuildMemberData
    {
        public string? entityId;
        public string? role;
        public string? displayName;
        public int power;
    }
}
