using System;
using System.IO;
using System.Linq;
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
    public class LeaveGuild
    {
        private readonly ILogger<LeaveGuild> _logger;

        public LeaveGuild(ILogger<LeaveGuild> logger)
        {
            _logger = logger;
        }

        [Function("LeaveGuild")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("[LeaveGuild] Function invoked.");

                // 1) 요청 바디 읽기
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation($"[LeaveGuild] Request body: {requestBody}");

                // 2) JSON 파싱
                var input = JsonConvert.DeserializeObject<LeaveGuildRequest>(requestBody);
                if (input == null ||
                    string.IsNullOrEmpty(input.PlayFabId) ||
                    input.EntityToken == null ||
                    string.IsNullOrEmpty(input.EntityToken.EntityToken) ||
                    input.EntityToken.Entity == null ||
                    string.IsNullOrEmpty(input.EntityToken.Entity.Id) ||
                    string.IsNullOrEmpty(input.EntityToken.Entity.Type))
                {
                    _logger.LogWarning("[LeaveGuild] Missing required fields (PlayFabId / entityToken).");
                    return new BadRequestObjectResult("Invalid request: must provide PlayFabId and entityToken data.");
                }

                // 3) PlayFab 설정 (SecretKey 등)
                PlayFabConfig.Configure();
                _logger.LogInformation("[LeaveGuild] PlayFab configured.");

                // 4) 서버 토큰(DeveloperSecretKey) 획득
                var serverTokenReq = new GetEntityTokenRequest();
                var serverTokenRes = await PlayFabAuthenticationAPI.GetEntityTokenAsync(serverTokenReq);
                if (serverTokenRes.Error != null)
                {
                    _logger.LogError("[LeaveGuild] Error fetching server entity token: " + serverTokenRes.Error.GenerateErrorReport());
                    return new BadRequestObjectResult("Error fetching server entity token");
                }
                var serverAuthContext = new PlayFabAuthenticationContext
                {
                    EntityId = serverTokenRes.Result.Entity.Id,
                    EntityType = serverTokenRes.Result.Entity.Type,
                    EntityToken = serverTokenRes.Result.EntityToken
                };

                // 5) 유저(본인) 그룹 조회: ListMembership
                //    entity ID = input.EntityToken.Entity.Id
                //    user might be in multiple groups -> 여기서는 첫 번째 그룹만 사용 (혹은 필요 시 특정 Guild ID 탐색)
                var listMembershipReq = new ListMembershipRequest
                {
                    AuthenticationContext = serverAuthContext,
                    Entity = new PlayFab.GroupsModels.EntityKey
                    {
                        Id = input.EntityToken.Entity.Id,
                        Type = input.EntityToken.Entity.Type
                    }
                };
                var listMembershipRes = await PlayFabGroupsAPI.ListMembershipAsync(listMembershipReq);
                if (listMembershipRes.Error != null)
                {
                    _logger.LogError("[LeaveGuild] ListMembership error: " + listMembershipRes.Error.GenerateErrorReport());
                    return new BadRequestObjectResult("Failed to check group membership.");
                }

                var groups = listMembershipRes.Result.Groups;
                if (groups == null || groups.Count == 0)
                {
                    // 이미 어떤 길드에도 속해 있지 않음
                    _logger.LogInformation("[LeaveGuild] No group found for the entity => user is not in a guild");
                    return new OkObjectResult(new LeaveGuildResponse
                    {
                        success = false,
                        message = "길드에 소속되어 있지 않습니다."
                    });
                }

                // 첫 번째 그룹만 사용
                var firstGroup = groups[0].Group;
                string groupId = firstGroup.Id;
                _logger.LogInformation($"[LeaveGuild] Found group: {groupId}");

                // 6) RemoveMembers (RoleId 없이) -> 해당 그룹에서 완전히 제거
                var removeReq = new RemoveMembersRequest
                {
                    AuthenticationContext = serverAuthContext,
                    Group = new PlayFab.GroupsModels.EntityKey { Id = groupId },
                    // RoleId 생략 -> 그룹 완전 탈퇴
                    Members = new System.Collections.Generic.List<PlayFab.GroupsModels.EntityKey>
                    {
                        new PlayFab.GroupsModels.EntityKey
                        {
                            Id = input.EntityToken.Entity.Id,
                            Type = input.EntityToken.Entity.Type
                        }
                    }
                };
                var removeRes = await PlayFabGroupsAPI.RemoveMembersAsync(removeReq);
                if (removeRes.Error != null)
                {
                    _logger.LogError("[LeaveGuild] RemoveMembers error: " + removeRes.Error.GenerateErrorReport());
                    return new BadRequestObjectResult("Failed to remove user from group: " + removeRes.Error.ErrorMessage);
                }

                // 7) 성공 응답
                _logger.LogInformation("[LeaveGuild] Successfully left the group.");
                return new OkObjectResult(new LeaveGuildResponse
                {
                    success = true,
                    message = "길드 탈퇴가 완료되었습니다."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("[LeaveGuild] Exception: " + ex.Message + "\n" + ex.StackTrace);
                return new BadRequestObjectResult("Internal server error in LeaveGuild.");
            }
        }
    }

    // 요청 DTO
    public class LeaveGuildRequest
    {
        public string? PlayFabId { get; set; }
        public EntityTokenData? EntityToken { get; set; }
    }

    // 응답 DTO
    public class LeaveGuildResponse
    {
        public bool success { get; set; }
        public string? message { get; set; }
    }
}
