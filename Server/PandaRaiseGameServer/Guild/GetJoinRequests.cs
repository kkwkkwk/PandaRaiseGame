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
using PlayFab.DataModels;
using PlayFab.GroupsModels;
using PlayFab.AuthenticationModels;
using CommonLibrary;

namespace Guild
{
    public class GetJoinRequests
    {
        private readonly ILogger<GetJoinRequests> _logger;

        public GetJoinRequests(ILogger<GetJoinRequests> logger)
        {
            _logger = logger;
        }

        [Function("GetJoinRequests")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("[GetJoinRequests] Function invoked.");
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation("[GetJoinRequests] RequestBody: " + requestBody);

                var input = JsonConvert.DeserializeObject<GetJoinRequestsRequest>(requestBody);
                if (input == null || string.IsNullOrEmpty(input.playFabId))
                {
                    _logger.LogWarning("[GetJoinRequests] Missing playFabId.");
                    return new BadRequestObjectResult("Invalid request: playFabId is required.");
                }

                // 1) 서버 인증
                PlayFabConfig.Configure();
                var tokenRes = await PlayFabAuthenticationAPI.GetEntityTokenAsync(new GetEntityTokenRequest());
                if (tokenRes.Error != null)
                {
                    _logger.LogError("[GetJoinRequests] token error: " + tokenRes.Error.GenerateErrorReport());
                    return new BadRequestObjectResult("Error fetching server token");
                }
                var serverAuth = new PlayFabAuthenticationContext
                {
                    EntityId = tokenRes.Result.Entity.Id,
                    EntityType = tokenRes.Result.Entity.Type,
                    EntityToken = tokenRes.Result.EntityToken
                };

                // 2) 요청자(마스터/부마스터) -> ListMembership -> 첫 번째 그룹
                //    실제 권한 체크는 생략 (필요 시 roleName != "길드마스터"/"부마스터" 시 거부)
                var convReq = new PlayFab.ServerModels.GetUserAccountInfoRequest { PlayFabId = input.playFabId };
                var convRes = await PlayFabServerAPI.GetUserAccountInfoAsync(convReq);
                if (convRes.Error != null)
                {
                    _logger.LogWarning("[GetJoinRequests] GetUserAccountInfo error: " + convRes.Error.GenerateErrorReport());
                    return new BadRequestObjectResult("Cannot find user info");
                }
                string entityId = convRes.Result.UserInfo.TitleInfo.TitlePlayerAccount.Id;

                var listMemReq = new ListMembershipRequest
                {
                    AuthenticationContext = serverAuth,
                    Entity = new PlayFab.GroupsModels.EntityKey
                    {
                        Id = entityId,
                        Type = "title_player_account"
                    }
                };
                var listMemRes = await PlayFabGroupsAPI.ListMembershipAsync(listMemReq);
                if (listMemRes.Error != null || listMemRes.Result.Groups == null || listMemRes.Result.Groups.Count == 0)
                {
                    return new OkObjectResult(new List<GuildApplicationItem>()); // 길드 없음 => 빈 목록
                }

                var groupId = listMemRes.Result.Groups[0].Group.Id;

                // 3) GetObjects -> "GuildApplications"
                var getObjReq = new GetObjectsRequest
                {
                    AuthenticationContext = serverAuth,
                    Entity = new PlayFab.DataModels.EntityKey
                    {
                        Id = groupId,
                        Type = "group"
                    }
                };
                var getObjRes = await PlayFabDataAPI.GetObjectsAsync(getObjReq, serverAuth);
                if (getObjRes.Error != null)
                {
                    _logger.LogWarning("[GetJoinRequests] GetObjects error: " + getObjRes.Error.GenerateErrorReport());
                    return new OkObjectResult(new List<GuildApplicationItem>());
                }

                // GuildApplicationsData
                GuildApplicationsData? currentApps = null;
                if (getObjRes.Result.Objects != null && getObjRes.Result.Objects.ContainsKey("GuildApplications"))
                {
                    var appObj = getObjRes.Result.Objects["GuildApplications"];
                    if (appObj.DataObject != null)
                    {
                        var rawJson = JsonConvert.SerializeObject(appObj.DataObject);
                        _logger.LogInformation("[GetJoinRequests] raw apps: " + rawJson);
                        try
                        {
                            currentApps = JsonConvert.DeserializeObject<GuildApplicationsData>(rawJson);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("[GetJoinRequests] parse error: " + ex.Message);
                        }
                    }
                }
                if (currentApps == null)
                {
                    currentApps = new GuildApplicationsData
                    {
                        applications = new List<GuildApplicationItem>()
                    };
                }

                // 4) 응답: applications 리스트
                return new OkObjectResult(currentApps.applications);
            }
            catch (Exception ex)
            {
                _logger.LogError("[GetJoinRequests] Exception: " + ex.Message);
                return new BadRequestObjectResult("Internal server error in GetJoinRequests.");
            }
        }
    }

    // DTO
    public class GetJoinRequestsRequest
    {
        public string? playFabId { get; set; }
    }
}
