using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
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

                // 2) JSON 파싱: 플레이어의 PlayFabId, 탈퇴할 그룹의 GroupId, 그리고 EntityToken 정보
                var input = JsonConvert.DeserializeObject<LeaveGuildRequest>(requestBody);
                if (input == null || string.IsNullOrEmpty(input.PlayFabId) || string.IsNullOrEmpty(input.GroupId))
                {
                    _logger.LogWarning("[LeaveGuild] PlayFabId or GroupId missing in request.");
                    return new BadRequestObjectResult("Invalid request: PlayFabId and GroupId are required.");
                }
                if (input.EntityToken == null ||
                    string.IsNullOrEmpty(input.EntityToken.EntityToken) ||
                    input.EntityToken.Entity == null ||
                    string.IsNullOrEmpty(input.EntityToken.Entity.Id) ||
                    string.IsNullOrEmpty(input.EntityToken.Entity.Type))
                {
                    _logger.LogWarning("[LeaveGuild] entityToken is missing or incomplete.");
                    return new BadRequestObjectResult("Invalid request: entityToken is required.");
                }

                // 3) PlayFab 초기 설정
                PlayFabConfig.Configure();
                _logger.LogInformation("[LeaveGuild] PlayFab configured.");

                // 4) 클라이언트 토큰을 기반으로 AuthenticationContext 준비
                var clientAuthContext = new PlayFabAuthenticationContext
                {
                    EntityId = input.EntityToken.Entity.Id,
                    EntityType = input.EntityToken.Entity.Type,
                    EntityToken = input.EntityToken.EntityToken
                };

                // 5) 탈퇴할 그룹의 EntityKey 준비
                // 그룹 생성 시 반환된 Group.Id를 그대로 사용합니다.
                var groupKey = new PlayFab.GroupsModels.EntityKey { Id = input.GroupId };

                // 6) 탈퇴할 멤버의 엔터티 키 준비 (클라이언트가 탈퇴하므로 본인의 엔터티 정보)
                var memberKey = new PlayFab.GroupsModels.EntityKey
                {
                    Id = input.EntityToken.Entity.Id, // 이 값은 본인의 title_player_account ID임
                    Type = input.EntityToken.Entity.Type
                };

                // 7) RemoveMembers 요청 준비
                var removeMembersReq = new RemoveMembersRequest
                {
                    AuthenticationContext = clientAuthContext,
                    Group = groupKey,
                    // 본인이 그룹에서 탈퇴하므로, 자기 자신만 포함합니다.
                    Members = new List<PlayFab.GroupsModels.EntityKey> { memberKey }
                    // RoleId는 지정하지 않아도 본인은 항상 자신을 제거할 수 있습니다.
                };

                _logger.LogInformation("[LeaveGuild] Calling RemoveMembersAsync to leave group...");

                var removeMembersRes = await PlayFabGroupsAPI.RemoveMembersAsync(removeMembersReq);
                if (removeMembersRes.Error != null)
                {
                    _logger.LogError("[LeaveGuild] RemoveMembers error: " + removeMembersRes.Error.GenerateErrorReport());
                    return new BadRequestObjectResult("Failed to leave group: " + removeMembersRes.Error.ErrorMessage);
                }

                // 8) 최종 응답 구성
                var resp = new LeaveGuildResponse
                {
                    groupId = input.GroupId,
                    message = "Successfully left the group."
                };

                string finalJson = JsonConvert.SerializeObject(resp);
                _logger.LogInformation("[LeaveGuild] Final response: " + finalJson);

                return new OkObjectResult(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError("[LeaveGuild] Exception: " + ex.Message + "\n" + ex.StackTrace);
                return new BadRequestObjectResult("Internal server error in LeaveGuild.");
            }
        }
    }

    // -----------------------------
    // 요청 DTO
    // -----------------------------
    public class LeaveGuildRequest
    {
        public string? PlayFabId { get; set; }
        public string? GroupId { get; set; }
        public EntityTokenData? EntityToken { get; set; }
    }

    // -----------------------------
    // 응답 DTO
    // -----------------------------
    public class LeaveGuildResponse
    {
        public string? groupId { get; set; }
        public string? message { get; set; }
    }
}
