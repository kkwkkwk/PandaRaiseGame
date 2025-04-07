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
    public class DeclineJoinRequest
    {
        private readonly ILogger<DeclineJoinRequest> _logger;

        public DeclineJoinRequest(ILogger<DeclineJoinRequest> logger)
        {
            _logger = logger;
        }

        [Function("DeclineJoinRequest")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("[DeclineJoinRequest] Function invoked.");
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var input = JsonConvert.DeserializeObject<DeclineJoinRequestData>(requestBody);
                if (input == null || string.IsNullOrEmpty(input.playFabId) || string.IsNullOrEmpty(input.applicantId))
                {
                    return new BadRequestObjectResult("Invalid request: playFabId, applicantId required.");
                }

                // 1) 서버 토큰
                PlayFabConfig.Configure();
                var tokenRes = await PlayFabAuthenticationAPI.GetEntityTokenAsync(new GetEntityTokenRequest());
                if (tokenRes.Error != null)
                {
                    return new BadRequestObjectResult("Error fetching server token");
                }
                var serverAuth = new PlayFabAuthenticationContext
                {
                    EntityId = tokenRes.Result.Entity.Id,
                    EntityType = tokenRes.Result.Entity.Type,
                    EntityToken = tokenRes.Result.EntityToken
                };

                // 2) requestor -> groupId
                string? requestorEntityId = await ConvertPlayFabIdToEntityId(input.playFabId);
                if (string.IsNullOrEmpty(requestorEntityId))
                {
                    return new OkObjectResult(new DeclineJoinResponse { success = false, message = "Requestor entity not found" });
                }
                var listReq = new ListMembershipRequest
                {
                    AuthenticationContext = serverAuth,
                    Entity = new PlayFab.GroupsModels.EntityKey
                    {
                        Id = requestorEntityId,
                        Type = "title_player_account"
                    }
                };
                var listRes = await PlayFabGroupsAPI.ListMembershipAsync(listReq);
                if (listRes.Error != null || listRes.Result.Groups == null || listRes.Result.Groups.Count == 0)
                {
                    return new OkObjectResult(new DeclineJoinResponse { success = false, message = "Requestor has no guild" });
                }
                var groupId = listRes.Result.Groups[0].Group.Id;

                // 3) GetObjects -> Remove applicant from "GuildApplications"
                var getObjReq = new GetObjectsRequest
                {
                    AuthenticationContext = serverAuth,
                    Entity = new PlayFab.DataModels.EntityKey { Id = groupId, Type = "group" }
                };
                var getObjRes = await PlayFabDataAPI.GetObjectsAsync(getObjReq, serverAuth);
                GuildApplicationsData? currentApps = new GuildApplicationsData { applications = new List<GuildApplicationItem>() };
                if (getObjRes?.Result?.Objects != null && getObjRes.Result.Objects.ContainsKey("GuildApplications"))
                {
                    var raw = getObjRes.Result.Objects["GuildApplications"].DataObject;
                    if (raw != null)
                    {
                        try
                        {
                            var rawJson = JsonConvert.SerializeObject(raw);
                            currentApps = JsonConvert.DeserializeObject<GuildApplicationsData>(rawJson);
                        }
                        catch { }
                    }
                }

                var found = currentApps?.applications?.FirstOrDefault(a => a.applicantId == input.applicantId);
                if (found != null)
                {
                    currentApps?.applications?.Remove(found);
                }

                // 4) SetObjects
                var setReq = new SetObjectsRequest
                {
                    AuthenticationContext = serverAuth,
                    Entity = new PlayFab.DataModels.EntityKey { Id = groupId, Type = "group" },
                    Objects = new List<SetObject>
                    {
                        new SetObject
                        {
                            ObjectName = "GuildApplications",
                            DataObject = currentApps
                        }
                    }
                };
                var setRes = await PlayFabDataAPI.SetObjectsAsync(setReq, serverAuth);
                if (setRes.Error != null)
                {
                    return new OkObjectResult(new DeclineJoinResponse
                    {
                        success = false,
                        message = "SetObjects error: " + setRes.Error.GenerateErrorReport()
                    });
                }

                return new OkObjectResult(new DeclineJoinResponse
                {
                    success = true,
                    message = "가입 신청이 거절되었습니다."
                });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult("[DeclineJoinRequest] Exception: " + ex.Message);
            }
        }

        // DTO
        public class DeclineJoinRequestData
        {
            public string? playFabId { get; set; }   // Approver
            public string? applicantId { get; set; } // Target
        }

        public class DeclineJoinResponse
        {
            public bool success;
            public string? message;
        }

        public class GuildApplicationsData
        {
            public List<GuildApplicationItem>? applications;
        }

        public class GuildApplicationItem
        {
            public string? applicantId { get; set; }
            public string? applicantEntityId { get; set; }
            public string? applicantName { get; set; }
            public int applicantPower { get; set; }
            public string? applyTime { get; set; }
        }

        // Convert
        private async Task<string?> ConvertPlayFabIdToEntityId(string playFabId)
        {
            var req = new PlayFab.ServerModels.GetUserAccountInfoRequest { PlayFabId = playFabId };
            var res = await PlayFabServerAPI.GetUserAccountInfoAsync(req);
            if (res.Error != null) return null;
            return res.Result.UserInfo?.TitleInfo?.TitlePlayerAccount?.Id;
        }
    }
}
