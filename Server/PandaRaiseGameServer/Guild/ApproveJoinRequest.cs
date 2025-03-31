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
    public class ApproveJoinRequest
    {
        private readonly ILogger<ApproveJoinRequest> _logger;

        public ApproveJoinRequest(ILogger<ApproveJoinRequest> logger)
        {
            _logger = logger;
        }

        [Function("ApproveJoinRequest")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("[ApproveJoinRequest] Function invoked.");
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var input = JsonConvert.DeserializeObject<ApproveJoinRequestData>(requestBody);
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

                // 2) 요청자(playFabId) -> Entity -> ListMembership -> group
                //    실제 권한 체크(마스터/부마스터)는 생략
                //    applicantId -> entityId 변환도 필요(아래)
                string? requestorEntityId = await ConvertPlayFabIdToEntityId(input.playFabId);
                if (string.IsNullOrEmpty(requestorEntityId))
                {
                    return new OkObjectResult(new ApproveJoinResponse { success = false, message = "Requestor entity not found" });
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
                    return new OkObjectResult(new ApproveJoinResponse { success = false, message = "Requestor has no guild" });
                }
                var groupId = listRes.Result.Groups[0].Group.Id;

                // 3) applicantId -> applicantEntityId
                string? applicantEntityId = await ConvertPlayFabIdToEntityId(input.applicantId);
                if (string.IsNullOrEmpty(applicantEntityId))
                {
                    return new OkObjectResult(new ApproveJoinResponse { success = false, message = "Applicant entity not found" });
                }

                // 4) Remove from "GuildApplications"
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

                // 찾고 제거
                var foundItem = currentApps.applications.FirstOrDefault(a => a.applicantId == input.applicantId);
                if (foundItem != null)
                {
                    currentApps.applications.Remove(foundItem);
                }

                // 5) AddMembers -> 이 applicant을 해당 groupId에 멤버로 추가
                var addReq = new AddMembersRequest
                {
                    AuthenticationContext = serverAuth,
                    Group = new PlayFab.GroupsModels.EntityKey { Id = groupId },
                    Members = new List<PlayFab.GroupsModels.EntityKey>
                    {
                        new PlayFab.GroupsModels.EntityKey
                        {
                            Id = applicantEntityId,
                            Type = "title_player_account"
                        }
                    },
                    // RoleId = "members" (기본) or "부마스터" 등. 여기선 기본 미지정 시 "members"로 들어가도록
                };
                var addRes = await PlayFabGroupsAPI.AddMembersAsync(addReq);
                if (addRes.Error != null)
                {
                    return new OkObjectResult(new ApproveJoinResponse
                    {
                        success = false,
                        message = "AddMembers error: " + addRes.Error.ErrorMessage
                    });
                }

                // 6) 다시 SetObjects -> 저장
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
                    return new OkObjectResult(new ApproveJoinResponse
                    {
                        success = false,
                        message = "SetObjects error: " + setRes.Error.GenerateErrorReport()
                    });
                }

                // success
                return new OkObjectResult(new ApproveJoinResponse
                {
                    success = true,
                    message = "가입 신청 승인 완료"
                });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult("[ApproveJoinRequest] Exception: " + ex.Message);
            }
        }

        // DTO
        public class ApproveJoinRequestData
        {
            public string? playFabId { get; set; }   // Approver
            public string? applicantId { get; set; } // Target
        }

        public class ApproveJoinResponse
        {
            public bool success;
            public string? message;
        }

        // GuildApplications 구조
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

        // ConvertPlayFabIdToEntityId
        private async Task<string?> ConvertPlayFabIdToEntityId(string playFabId)
        {
            var req = new PlayFab.ServerModels.GetUserAccountInfoRequest { PlayFabId = playFabId };
            var res = await PlayFabServerAPI.GetUserAccountInfoAsync(req);
            if (res.Error != null) return null;
            return res.Result.UserInfo?.TitleInfo?.TitlePlayerAccount?.Id;
        }
    }
}
