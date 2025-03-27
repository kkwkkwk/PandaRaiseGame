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
using PlayFab.ServerModels;
using PlayFab.GroupsModels;
using PlayFab.AuthenticationModels;
using CommonLibrary;

namespace Guild
{
    public class BanGuildMember
    {
        private readonly ILogger<BanGuildMember> _logger;

        public BanGuildMember(ILogger<BanGuildMember> logger)
        {
            _logger = logger;
        }

        [Function("BanGuildMember")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("[BanGuildMember] Function invoked.");

                // 1) 요청 바디 파싱
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation("[BanGuildMember] Request body: " + requestBody);

                var input = JsonConvert.DeserializeObject<BanGuildMemberRequest>(requestBody);
                if (input == null ||
                    string.IsNullOrEmpty(input.requestorId) ||
                    string.IsNullOrEmpty(input.targetId))
                {
                    _logger.LogWarning("[BanGuildMember] Missing required parameters.");
                    return new BadRequestObjectResult("Invalid request: requestorId, targetId are required.");
                }

                // 2) 표시명 ↔ 실제 역할 ID 매핑
                //    예: "길드마스터" -> "admins", "부마스터" -> "subadmins", "길드원" -> "members"
                var displayToId = new Dictionary<string, string>
                {
                    {"길드마스터", "admins"},
                    {"부마스터", "subadmins"},
                    {"길드원", "members"}
                };
                // 실제 역할 ID -> 표시명 역매핑이 필요하면 별도 Dictionary 준비

                // 3) PlayFab 초기 설정
                PlayFabConfig.Configure();
                _logger.LogInformation("[BanGuildMember] PlayFab configured.");

                // 4) 서버 토큰 획득 (DeveloperSecretKey 기반)
                var tokenRequest = new GetEntityTokenRequest();
                var tokenResult = await PlayFabAuthenticationAPI.GetEntityTokenAsync(tokenRequest);
                if (tokenResult.Error != null)
                {
                    _logger.LogError("[BanGuildMember] Error fetching server entity token: " + tokenResult.Error.GenerateErrorReport());
                    return new BadRequestObjectResult("Error fetching server entity token");
                }
                var serverAuthContext = new PlayFabAuthenticationContext
                {
                    EntityId = tokenResult.Result.Entity.Id,
                    EntityType = tokenResult.Result.Entity.Type,
                    EntityToken = tokenResult.Result.EntityToken
                };

                // 5) 요청자가 속한 그룹 조회 (ListMembership) -> 첫 번째 그룹 사용
                var listMembershipReq = new ListMembershipRequest
                {
                    AuthenticationContext = serverAuthContext,
                    Entity = new PlayFab.GroupsModels.EntityKey
                    {
                        Id = input.requestorId,
                        Type = "title_player_account"
                    }
                };
                var listMembershipRes = await PlayFabGroupsAPI.ListMembershipAsync(listMembershipReq);
                if (listMembershipRes.Error != null || listMembershipRes.Result.Groups.Count == 0)
                {
                    _logger.LogError("[BanGuildMember] Requestor's group not found.");
                    return new BadRequestObjectResult("Requestor is not in any group.");
                }
                var group = listMembershipRes.Result.Groups[0].Group;
                string groupId = group.Id;
                _logger.LogInformation($"[BanGuildMember] Requestor's group: {groupId}");

                // 6) 그룹 멤버 조회: ListGroupMembers
                var listMembersReq = new ListGroupMembersRequest
                {
                    AuthenticationContext = serverAuthContext,
                    Group = group
                };
                var listMembersRes = await PlayFabGroupsAPI.ListGroupMembersAsync(listMembersReq);
                if (listMembersRes.Error != null || listMembersRes.Result.Members == null)
                {
                    _logger.LogError("[BanGuildMember] Failed to retrieve group members.");
                    return new BadRequestObjectResult("Failed to retrieve group members.");
                }

                string? requestorDisplayRole = null;
                string? targetDisplayRole = null;
                var roleBlocks = listMembersRes.Result.Members;

                foreach (var block in roleBlocks)
                {
                    // block.RoleName = 표시 역할 (예: "길드마스터"/"부마스터"/"길드원")
                    if (block.Members != null)
                    {
                        foreach (var mem in block.Members)
                        {
                            if (mem.Key.Id == input.requestorId)
                            {
                                requestorDisplayRole = block.RoleName;
                            }
                            if (mem.Key.Id == input.targetId)
                            {
                                targetDisplayRole = block.RoleName;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(requestorDisplayRole))
                {
                    _logger.LogWarning("[BanGuildMember] Requestor not in group members list.");
                    return new BadRequestObjectResult("Requestor is not a member of the group.");
                }
                if (string.IsNullOrEmpty(targetDisplayRole))
                {
                    _logger.LogWarning("[BanGuildMember] Target not in group members list.");
                    return new BadRequestObjectResult("Target is not a member of the group.");
                }

                _logger.LogInformation($"[BanGuildMember] RequestorRole={requestorDisplayRole}, TargetRole={targetDisplayRole}");

                // 7) 권한 체크
                //    길드마스터("길드마스터") -> 부마스터, 길드원 추방 가능
                //    부마스터("부마스터") -> 길드원만 추방 가능
                //    길드원("길드원") -> 추방 불가능
                bool canBan = false;

                if (requestorDisplayRole == "길드마스터")
                {
                    // 부마스터 or 길드원 추방 가능
                    // target이 "길드마스터"이면 안됨(본인을 추방?)
                    if (targetDisplayRole == "부마스터" || targetDisplayRole == "길드원")
                        canBan = true;
                }
                else if (requestorDisplayRole == "부마스터")
                {
                    // 길드원만 추방 가능
                    if (targetDisplayRole == "길드원")
                        canBan = true;
                }
                else
                {
                    // 길드원은 추방 불가
                    canBan = false;
                }

                if (!canBan)
                {
                    _logger.LogWarning($"[BanGuildMember] 권한 부족: {requestorDisplayRole} cannot ban {targetDisplayRole}");
                    return new BadRequestObjectResult($"You cannot ban a {targetDisplayRole} with role {requestorDisplayRole}.");
                }

                // 8) RemoveMembers
                //    실제 역할 ID로 역매핑, 또는 block.RoleName이 곧 roleId라고 가정
                //    여기선 block.RoleName = "길드마스터"/"부마스터"/"길드원" → 아래 Dictionary를 사용해 실제 roleId 얻음
                string targetRoleId = displayToId.ContainsKey(targetDisplayRole)
                    ? displayToId[targetDisplayRole]
                    : targetDisplayRole; // 혹시나 매핑 없는 경우 fallback

                var removeReq = new RemoveMembersRequest
                {
                    AuthenticationContext = serverAuthContext,
                    Group = new PlayFab.GroupsModels.EntityKey { Id = groupId },
                    // RoleId = null; // 혹은 아예 지정하지 않음
                    Members = new List<PlayFab.GroupsModels.EntityKey>
                    {
                        new PlayFab.GroupsModels.EntityKey { Id = input.targetId, Type = "title_player_account" }
                    }
                };

                var removeRes = await PlayFabGroupsAPI.RemoveMembersAsync(removeReq);
                if (removeRes.Error != null)
                {
                    _logger.LogError("[BanGuildMember] RemoveMembers error: " + removeRes.Error.GenerateErrorReport());
                    return new BadRequestObjectResult("Failed to remove member from group: " + removeRes.Error.ErrorMessage);
                }

                // 9) 응답
                _logger.LogInformation("[BanGuildMember] Member removed successfully.");
                var resp = new BanGuildMemberResponse
                {
                    success = true,
                    message = $"{targetDisplayRole} has been banned by {requestorDisplayRole}."
                };
                return new OkObjectResult(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError("[BanGuildMember] Exception: " + ex.Message + "\n" + ex.StackTrace);
                return new BadRequestObjectResult("Internal server error in BanGuildMember.");
            }
        }
    }

    // ------------------------------------------------------------
    // 요청 DTO
    // ------------------------------------------------------------
    public class BanGuildMemberRequest
    {
        public string? requestorId { get; set; }  // 추방 명령을 내리는 유저의 엔터티 ID
        public string? targetId { get; set; }     // 추방 대상 멤버의 엔터티 ID
    }

    // ------------------------------------------------------------
    // 응답 DTO
    // ------------------------------------------------------------
    public class BanGuildMemberResponse
    {
        public bool success { get; set; }
        public string? message { get; set; }
    }
}
