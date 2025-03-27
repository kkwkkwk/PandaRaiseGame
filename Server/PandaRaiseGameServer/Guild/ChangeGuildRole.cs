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
    public class ChangeGuildRole
    {
        private readonly ILogger<ChangeGuildRole> _logger;

        public ChangeGuildRole(ILogger<ChangeGuildRole> logger)
        {
            _logger = logger;
        }

        [Function("ChangeGuildRole")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("[ChangeGuildRole] Function invoked.");

                // 1) 요청 본문 파싱
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation("[ChangeGuildRole] Request body: " + requestBody);
                var input = JsonConvert.DeserializeObject<ChangeGuildRoleRequest>(requestBody);
                if (input == null ||
                    string.IsNullOrEmpty(input.requestorId) ||
                    string.IsNullOrEmpty(input.targetId) ||
                    string.IsNullOrEmpty(input.newRole))
                {
                    _logger.LogWarning("[ChangeGuildRole] Missing required parameters.");
                    return new BadRequestObjectResult("Invalid request: requestorId, targetId, and newRole are required.");
                }

                // 2) 역할 표시명 → 실제 역할 ID (역매핑)
                var roleMapping = new Dictionary<string, string>()
                {
                    {"길드마스터", "admins"},
                    {"부마스터", "subadmins"},
                    {"길드원", "members"}
                };
                if (!roleMapping.ContainsKey(input.newRole))
                {
                    return new BadRequestObjectResult("Invalid newRole value.");
                }
                string desiredRoleId = roleMapping[input.newRole];

                // 3) PlayFab 초기 설정
                PlayFabConfig.Configure();
                _logger.LogInformation("[ChangeGuildRole] PlayFab configured.");

                // 4) 서버 토큰 획득 (DeveloperSecretKey 기반)
                var tokenRequest = new GetEntityTokenRequest();
                var tokenResult = await PlayFabAuthenticationAPI.GetEntityTokenAsync(tokenRequest);
                if (tokenResult.Error != null)
                {
                    _logger.LogError("[ChangeGuildRole] Error fetching server entity token: " + tokenResult.Error.GenerateErrorReport());
                    return new BadRequestObjectResult("Error fetching server entity token");
                }
                var serverAuthContext = new PlayFabAuthenticationContext
                {
                    EntityId = tokenResult.Result.Entity.Id,
                    EntityType = tokenResult.Result.Entity.Type,
                    EntityToken = tokenResult.Result.EntityToken
                };

                // 5) 요청자(길드마스터) 그룹 조회 : ListMembership API 사용하여 요청자가 속한 그룹(길드)를 찾음.
                var listMembershipReq = new ListMembershipRequest
                {
                    AuthenticationContext = serverAuthContext,
                    Entity = new PlayFab.GroupsModels.EntityKey
                    {
                        Id = input.requestorId,  // 요청자 엔터티 ID (title_player_account)
                        Type = "title_player_account"
                    }
                };
                var listMembershipRes = await PlayFabGroupsAPI.ListMembershipAsync(listMembershipReq);
                if (listMembershipRes.Error != null || listMembershipRes.Result.Groups.Count == 0)
                {
                    _logger.LogError("[ChangeGuildRole] Requestor's group not found.");
                    return new BadRequestObjectResult("Group not found for the requestor.");
                }
                // 여기서는 첫 번째 그룹만 사용
                var group = listMembershipRes.Result.Groups[0].Group;
                string groupId = group.Id;
                _logger.LogInformation($"[ChangeGuildRole] Requestor's group found: GroupId = {groupId}");

                // 6) 그룹 멤버 조회 : ListGroupMembers API를 사용하여 각 멤버의 현재 역할(표시명)을 확인
                var listMembersReq = new ListGroupMembersRequest
                {
                    AuthenticationContext = serverAuthContext,
                    Group = new PlayFab.GroupsModels.EntityKey { Id = groupId }
                };
                var listMembersRes = await PlayFabGroupsAPI.ListGroupMembersAsync(listMembersReq);
                if (listMembersRes.Error != null || listMembersRes.Result.Members == null)
                {
                    _logger.LogError("[ChangeGuildRole] Failed to retrieve group members.");
                    return new BadRequestObjectResult("Failed to retrieve group members.");
                }
                var roleBlocks = listMembersRes.Result.Members;
                string? requestorCurrentDisplayRole = null;
                string? targetCurrentDisplayRole = null;
                foreach (var block in roleBlocks)
                {
                    string displayRole = block.RoleName; // 여기서 RoleName은 표시 역할 (예: "길드마스터", "부마스터", "길드원")
                    if (block.Members != null)
                    {
                        foreach (var member in block.Members)
                        {
                            if (member.Key.Id == input.requestorId)
                            {
                                requestorCurrentDisplayRole = displayRole;
                            }
                            if (member.Key.Id == input.targetId)
                            {
                                targetCurrentDisplayRole = displayRole;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(requestorCurrentDisplayRole))
                {
                    _logger.LogError("[ChangeGuildRole] Requestor's role not found.");
                    return new BadRequestObjectResult("Requestor's role not found.");
                }
                if (string.IsNullOrEmpty(targetCurrentDisplayRole))
                {
                    _logger.LogWarning("[ChangeGuildRole] Target member's role not found.");
                    return new BadRequestObjectResult("Target member's role not found.");
                }

                // 7) 실제 역할 ID로 변환 (역매핑)
                string requestorRoleId = roleMapping.ContainsKey(requestorCurrentDisplayRole)
                    ? roleMapping[requestorCurrentDisplayRole]
                    : requestorCurrentDisplayRole;
                string targetRoleId = roleMapping.ContainsKey(targetCurrentDisplayRole)
                    ? roleMapping[targetCurrentDisplayRole]
                    : targetCurrentDisplayRole;

                // 8) 요청자가 길드마스터(실제 "admins")여야만 직위 변경 가능
                if (requestorRoleId != "admins")
                {
                    _logger.LogWarning($"[ChangeGuildRole] Requestor is not guild master. Requestor display role: {requestorCurrentDisplayRole} (mapped: {requestorRoleId}), required: admins");
                    return new BadRequestObjectResult("Only guild master can change roles.");
                }

                // 9) 대상 멤버의 현재 역할과 새 역할이 동일하면 변경할 필요 없음
                if (targetRoleId == desiredRoleId)
                {
                    _logger.LogInformation("[ChangeGuildRole] Target member already has the desired role.");
                    return new OkObjectResult(new ChangeGuildRoleResponse { success = true, message = "No change needed." });
                }

                // 10) 변경 요청: ChangeMemberRole API 호출
                // 공식 문서에 따라, 이 API는 OriginRoleId, DestinationRoleId, Group, Members 배열을 필요로 함.
                var changeRoleRequest = new ChangeMemberRoleRequest
                {
                    AuthenticationContext = serverAuthContext,
                    Group = new PlayFab.GroupsModels.EntityKey { Id = groupId, Type = "group" },
                    Members = new List<PlayFab.GroupsModels.EntityKey>
                    {
                        new PlayFab.GroupsModels.EntityKey { Id = input.targetId, Type = "title_player_account" }
                    },
                    OriginRoleId = targetRoleId,
                    DestinationRoleId = desiredRoleId,
                    CustomTags = new Dictionary<string, string>()
                };

                _logger.LogInformation("[ChangeGuildRole] Calling ChangeMemberRoleAsync for target member...");
                var changeRoleRes = await PlayFabGroupsAPI.ChangeMemberRoleAsync(changeRoleRequest);
                if (changeRoleRes.Error != null)
                {
                    _logger.LogError("[ChangeGuildRole] ChangeMemberRole error: " + changeRoleRes.Error.GenerateErrorReport());
                    return new BadRequestObjectResult("Failed to change member role: " + changeRoleRes.Error.ErrorMessage);
                }

                // 11) 만약 새 역할이 "길드마스터"인 경우 (즉, target becomes guild master), 요청자는 자동 강등 처리
                if (input.newRole == "길드마스터")
                {
                    // 요청자(길드마스터)를 "admins"에서 제거 후 "members"로 추가
                    var demoteRequest = new ChangeMemberRoleRequest
                    {
                        AuthenticationContext = serverAuthContext,
                        Group = new PlayFab.GroupsModels.EntityKey { Id = groupId, Type = "group" },
                        Members = new List<PlayFab.GroupsModels.EntityKey>
                        {
                            new PlayFab.GroupsModels.EntityKey { Id = input.requestorId, Type = "title_player_account" }
                        },
                        OriginRoleId = "admins", // 길드마스터의 실제 역할
                        DestinationRoleId = "members", // 강등 후 기본 역할
                        CustomTags = new Dictionary<string, string>()
                    };

                    _logger.LogInformation("[ChangeGuildRole] Demoting requestor (guild master) to members...");
                    var demoteRes = await PlayFabGroupsAPI.ChangeMemberRoleAsync(demoteRequest);
                    if (demoteRes.Error != null)
                    {
                        _logger.LogError("[ChangeGuildRole] Demote requestor error: " + demoteRes.Error.GenerateErrorReport());
                        // 진행 여부 선택: 여기서 실패해도 target 역할 변경은 완료되었으므로 경고만 남김.
                    }
                }

                _logger.LogInformation("[ChangeGuildRole] Role change successful.");
                return new OkObjectResult(new ChangeGuildRoleResponse { success = true, message = "Role changed successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError("[ChangeGuildRole] Exception: " + ex.Message + "\n" + ex.StackTrace);
                return new BadRequestObjectResult("Internal server error in ChangeGuildRole.");
            }
        }
    }

    public class ChangeGuildRoleRequest
    {
        public string? requestorId { get; set; }
        public string? targetId { get; set; }
        public string? newRole { get; set; }
    }

    public class ChangeGuildRoleResponse
    {
        public bool success { get; set; }
        public string? message { get; set; }
    }
}
