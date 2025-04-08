using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using PlayFab.GroupsModels;
using PlayFab.AuthenticationModels;
using PlayFab.DataModels; // for GetObjectsRequest, SetObjectsRequest
using CommonLibrary;

namespace Guild
{
    public class JoinGuild
    {
        private readonly ILogger<JoinGuild> _logger;

        public JoinGuild(ILogger<JoinGuild> logger)
        {
            _logger = logger;
        }

        [Function("JoinGuild")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("[JoinGuild] Function invoked.");

                // 1) 요청 바디 파싱
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation("[JoinGuild] RequestBody: " + requestBody);

                var input = JsonConvert.DeserializeObject<JoinGuildRequest>(requestBody);
                if (input == null ||
                    string.IsNullOrEmpty(input.playFabId) ||
                    string.IsNullOrEmpty(input.guildId) ||
                    input.entityToken == null ||
                    string.IsNullOrEmpty(input.entityToken.EntityToken) ||
                    input.entityToken.Entity == null ||
                    string.IsNullOrEmpty(input.entityToken.Entity.Id) ||
                    string.IsNullOrEmpty(input.entityToken.Entity.Type))
                {
                    _logger.LogWarning("[JoinGuild] Missing required parameters.");
                    return new BadRequestObjectResult("Invalid request: playFabId, guildId, entityToken are required.");
                }

                // 2) PlayFab 설정
                PlayFabConfig.Configure();
                _logger.LogInformation("[JoinGuild] PlayFab configured.");

                // 3) 서버 토큰(DeveloperSecretKey) 획득
                var tokenReq = new GetEntityTokenRequest();
                var tokenRes = await PlayFabAuthenticationAPI.GetEntityTokenAsync(tokenReq);
                if (tokenRes.Error != null)
                {
                    _logger.LogError("[JoinGuild] Error fetching server entity token: " + tokenRes.Error.GenerateErrorReport());
                    return new BadRequestObjectResult("Error fetching server entity token");
                }
                var serverAuthContext = new PlayFabAuthenticationContext
                {
                    EntityId = tokenRes.Result.Entity.Id,
                    EntityType = tokenRes.Result.Entity.Type,
                    EntityToken = tokenRes.Result.EntityToken
                };

                // 4) 유저가 이미 이 길드에 속해 있는지 확인
                var listMemReq = new ListMembershipRequest
                {
                    AuthenticationContext = serverAuthContext,
                    Entity = new PlayFab.GroupsModels.EntityKey
                    {
                        Id = input.entityToken.Entity.Id,
                        Type = input.entityToken.Entity.Type
                    }
                };
                var listMemRes = await PlayFabGroupsAPI.ListMembershipAsync(listMemReq);
                if (listMemRes.Error != null)
                {
                    _logger.LogError("[JoinGuild] ListMembership error: " + listMemRes.Error.GenerateErrorReport());
                    return new BadRequestObjectResult("ListMembership error");
                }

                var userGroups = listMemRes.Result.Groups;
                if (userGroups != null)
                {
                    foreach (var ug in userGroups)
                    {
                        if (ug.Group.Id == input.guildId)
                        {
                            // 이미 가입된 상태
                            var respAlready = new JoinGuildResponse
                            {
                                success = false,
                                message = "이미 이 길드에 속해 있습니다."
                            };
                            return new OkObjectResult(respAlready);
                        }
                    }
                }

                // 5) 유저 닉네임 & 전투력 가져오기
                //    5.1) GetUserAccountInfo -> TitleInfo.DisplayName
                string applicantName = "Unknown";
                var accountInfoReq = new GetUserAccountInfoRequest { PlayFabId = input.playFabId };
                var accountInfoRes = await PlayFabServerAPI.GetUserAccountInfoAsync(accountInfoReq);
                if (accountInfoRes.Error != null)
                {
                    _logger.LogWarning("[JoinGuild] GetUserAccountInfo failed: " + accountInfoRes.Error.GenerateErrorReport());
                }
                else
                {
                    applicantName = accountInfoRes.Result.UserInfo?.TitleInfo?.DisplayName ?? "NoName";
                }

                //    5.2) GetUserData["UserPower"]
                int applicantPower = 0;
                var userDataReq = new GetUserDataRequest
                {
                    PlayFabId = input.playFabId,
                    Keys = new List<string> { "UserPower" }
                };
                var userDataRes = await PlayFabServerAPI.GetUserDataAsync(userDataReq);
                if (userDataRes.Error != null)
                {
                    _logger.LogWarning("[JoinGuild] GetUserData failed: " + userDataRes.Error.GenerateErrorReport());
                }
                else if (userDataRes.Result.Data != null && userDataRes.Result.Data.ContainsKey("UserPower"))
                {
                    int.TryParse(userDataRes.Result.Data["UserPower"].Value, out applicantPower);
                }

                // 6) guildId -> 그룹 엔티티 key
                var groupEntityKey = new PlayFab.DataModels.EntityKey
                {
                    Id = input.guildId,
                    Type = "group"
                };

                // 7) GetObjects -> "GuildApplications"
                var getObjReq = new GetObjectsRequest
                {
                    AuthenticationContext = serverAuthContext,
                    Entity = groupEntityKey
                };
                var getObjRes = await PlayFabDataAPI.GetObjectsAsync(getObjReq, serverAuthContext);
                if (getObjRes.Error != null)
                {
                    _logger.LogWarning("[JoinGuild] GetObjects error: " + getObjRes.Error.GenerateErrorReport());
                }

                GuildApplicationsData? currentApps = null;
                if (getObjRes?.Result?.Objects != null && getObjRes.Result.Objects.ContainsKey("GuildApplications"))
                {
                    var appObj = getObjRes.Result.Objects["GuildApplications"];
                    if (appObj.DataObject != null)
                    {
                        var rawJson = JsonConvert.SerializeObject(appObj.DataObject);
                        _logger.LogInformation("[JoinGuild] Existing GuildApplications JSON: " + rawJson);

                        try
                        {
                            currentApps = JsonConvert.DeserializeObject<GuildApplicationsData>(rawJson);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("[JoinGuild] JSON parse error: " + ex.Message);
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

                // 8) 중복 신청 방지
                if (currentApps.applications != null)
                {
                    if (currentApps.applications.Any(a => a.applicantEntityId == input.entityToken.Entity.Id))
                    {
                        var respDup = new JoinGuildResponse
                        {
                            success = false,
                            message = "이미 가입 신청을 하였습니다. 마스터 승인 대기 중."
                        };
                        return new OkObjectResult(respDup);
                    }
                }

                // 9) 새 신청 항목 추가 (ID, 닉네임, 전투력)
                if (currentApps.applications != null)
                {
                    currentApps.applications.Add(new GuildApplicationItem
                    {
                        applicantId = input.playFabId,            // Title Player Account ID (PlayFabId)
                        applicantEntityId = input.entityToken.Entity.Id, // entityId
                        applicantName = applicantName,            // DisplayName
                        applicantPower = applicantPower,          // 전투력
                        applyTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }

                // 10) SetObjects -> "GuildApplications"
                var setReq = new SetObjectsRequest
                {
                    AuthenticationContext = serverAuthContext,
                    Entity = groupEntityKey,
                    Objects = new List<SetObject>
                    {
                        new SetObject
                        {
                            ObjectName = "GuildApplications",
                            DataObject = currentApps
                        }
                    }
                };
                var setRes = await PlayFabDataAPI.SetObjectsAsync(setReq, serverAuthContext);
                if (setRes.Error != null)
                {
                    _logger.LogWarning("[JoinGuild] SetObjects error: " + setRes.Error.GenerateErrorReport());
                    var failResp = new JoinGuildResponse
                    {
                        success = false,
                        message = "가입 신청 저장에 실패했습니다."
                    };
                    return new OkObjectResult(failResp);
                }

                // 11) 성공 응답
                var successResp = new JoinGuildResponse
                {
                    success = true,
                    message = "길드 가입 신청이 완료되었습니다."
                };
                return new OkObjectResult(successResp);
            }
            catch (Exception ex)
            {
                _logger.LogError("[JoinGuild] Exception: " + ex.Message + "\n" + ex.StackTrace);
                return new BadRequestObjectResult("Internal server error in JoinGuild.");
            }
        }
    }

    // -----------------------------------------------------------
    // 요청 DTO
    // -----------------------------------------------------------
    public class JoinGuildRequest
    {
        public string? playFabId { get; set; }   // Title Player ID
        public string? guildId { get; set; }     // 대상 길드 ID
        public EntityTokenData? entityToken { get; set; }
    }

    // -----------------------------------------------------------
    // 응답 DTO
    // -----------------------------------------------------------
    public class JoinGuildResponse
    {
        public bool success { get; set; }
        public string? message { get; set; }
    }

    // -----------------------------------------------------------
    // 가입 신청 목록 구조
    // -----------------------------------------------------------
    public class GuildApplicationsData
    {
        public List<GuildApplicationItem>? applications;
    }

    public class GuildApplicationItem
    {
        public string? applicantId;        // PlayFabId (타이틀 ID)
        public string? applicantEntityId;  // entity ID
        public string? applicantName;      // 닉네임 (DisplayName)
        public int applicantPower;        // 전투력 (UserPower)
        public string? applyTime;          // UTC 시각 문자열
    }
}
