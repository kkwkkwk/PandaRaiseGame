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
using PlayFab.ServerModels;  // TitleData 관련
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

                // 1) 요청 DTO 파싱
                var input = JsonConvert.DeserializeObject<GetJoinRequestsRequest>(requestBody);
                if (input == null || string.IsNullOrEmpty(input.playFabId))
                {
                    _logger.LogWarning("[GetJoinRequests] Missing playFabId.");
                    return new BadRequestObjectResult("Invalid request: playFabId is required.");
                }

                // 2) 서버 인증
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

                // 3) Requestor(마스터/관리자) → entityId
                string? requestorEntityId = await ConvertPlayFabIdToEntityId(input.playFabId);
                if (string.IsNullOrEmpty(requestorEntityId))
                {
                    _logger.LogWarning("[GetJoinRequests] Could not get requestorEntityId");
                    return new OkObjectResult(new List<GuildApplicationItem>());
                }

                // 4) Requestor's group (ListMembership)
                var listMemReq = new ListMembershipRequest
                {
                    AuthenticationContext = serverAuth,
                    Entity = new PlayFab.GroupsModels.EntityKey
                    {
                        Id = requestorEntityId,
                        Type = "title_player_account"
                    }
                };
                var listMemRes = await PlayFabGroupsAPI.ListMembershipAsync(listMemReq);
                if (listMemRes.Error != null ||
                    listMemRes.Result.Groups == null ||
                    listMemRes.Result.Groups.Count == 0)
                {
                    // 길드 없음 => 빈 목록
                    _logger.LogInformation("[GetJoinRequests] User not in any guild => no applications");
                    return new OkObjectResult(new List<GuildApplicationItem>());
                }
                var groupId = listMemRes.Result.Groups[0].Group.Id;

                // 5) GetObjects("GuildApplications")
                var getObjReq = new GetObjectsRequest
                {
                    AuthenticationContext = serverAuth,
                    Entity = new PlayFab.DataModels.EntityKey { Id = groupId, Type = "group" }
                };
                var getObjRes = await PlayFabDataAPI.GetObjectsAsync(getObjReq, serverAuth);
                if (getObjRes.Error != null)
                {
                    _logger.LogWarning("[GetJoinRequests] GetObjects error: " + getObjRes.Error.GenerateErrorReport());
                    return new OkObjectResult(new List<GuildApplicationItem>());
                }

                // 6) 파싱
                GuildApplicationsData? currentApps = null;
                if (getObjRes.Result.Objects != null &&
                    getObjRes.Result.Objects.ContainsKey("GuildApplications"))
                {
                    var rawObj = getObjRes.Result.Objects["GuildApplications"].DataObject;
                    if (rawObj != null)
                    {
                        var rawJson = JsonConvert.SerializeObject(rawObj);
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
                    currentApps = new GuildApplicationsData { applications = new List<GuildApplicationItem>() };
                }

                // 7) **배열**만 반환
                // => 클라이언트가 `List<GuildManagerData>`로 역직렬화 시 정상 파싱
                return new OkObjectResult(currentApps.applications);
            }
            catch (Exception ex)
            {
                _logger.LogError("[GetJoinRequests] Exception: " + ex.Message);
                return new BadRequestObjectResult("Internal server error in GetJoinRequests.");
            }
        }

        // 요청 DTO
        public class GetJoinRequestsRequest
        {
            public string? playFabId { get; set; }
        }

        // GuildApplicationsData
        public class GuildApplicationsData
        {
            public List<GuildApplicationItem>? applications;
        }

        public class GuildApplicationItem
        {
            public string? applicantId { get; set; }        // ex) "3BC91AEA1D825090"
            public string? applicantEntityId { get; set; }  // ex) "3665614AD486F0FD"
            public string? applicantName { get; set; }      // ex) "JJungSu51115"
            public int applicantPower { get; set; }        // ex) 88888
            public string? applyTime { get; set; }          // ex) "2025-03-31 12:48:13"
        }

        // PlayFabId -> entityId
        private async Task<string?> ConvertPlayFabIdToEntityId(string playFabId)
        {
            var req = new PlayFab.ServerModels.GetUserAccountInfoRequest { PlayFabId = playFabId };
            var res = await PlayFabServerAPI.GetUserAccountInfoAsync(req);
            if (res.Error != null) return null;
            return res.Result.UserInfo?.TitleInfo?.TitlePlayerAccount?.Id;
        }
    }
}
