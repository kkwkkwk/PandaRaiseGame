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
using PlayFab.DataModels; // 그룹 Object 업데이트용
using CommonLibrary;

namespace Guild
{
    public class GuildAttendance
    {
        private readonly ILogger<GuildAttendance> _logger;

        public GuildAttendance(ILogger<GuildAttendance> logger)
        {
            _logger = logger;
        }

        [Function("GuildAttendance")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("[GuildAttendance] Function invoked.");

                // 1) 요청 바디 파싱
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation("[GuildAttendance] Request body: " + requestBody);

                var input = JsonConvert.DeserializeObject<GuildAttendanceRequest>(requestBody);
                if (input == null ||
                    string.IsNullOrEmpty(input.playFabId) ||
                    input.EntityToken == null ||
                    string.IsNullOrEmpty(input.EntityToken.EntityToken) ||
                    input.EntityToken.Entity == null ||
                    string.IsNullOrEmpty(input.EntityToken.Entity.Id) ||
                    string.IsNullOrEmpty(input.EntityToken.Entity.Type))
                {
                    _logger.LogWarning("[GuildAttendance] Request is missing required fields (playFabId, entityToken).");
                    return new BadRequestObjectResult("Invalid request: playFabId & entityToken data required.");
                }

                // 2) PlayFab 초기 설정
                PlayFabConfig.Configure();
                _logger.LogInformation("[GuildAttendance] PlayFab configured.");

                // 3) 서버 토큰(DeveloperSecretKey) 획득
                var tokenRequest = new GetEntityTokenRequest();
                var tokenResult = await PlayFabAuthenticationAPI.GetEntityTokenAsync(tokenRequest);
                if (tokenResult.Error != null)
                {
                    _logger.LogError("[GuildAttendance] Error fetching server entity token: " + tokenResult.Error.GenerateErrorReport());
                    return new BadRequestObjectResult("Error fetching server entity token");
                }
                var serverAuthContext = new PlayFabAuthenticationContext
                {
                    EntityId = tokenResult.Result.Entity.Id,
                    EntityType = tokenResult.Result.Entity.Type,
                    EntityToken = tokenResult.Result.EntityToken
                };

                // 4) 출석 여부 확인 (UserData["GuildAttendance"])
                DateTime nowKST = DateTime.UtcNow.AddHours(9);
                string todayString = nowKST.ToString("yyyy-MM-dd");

                var getUserDataReq = new GetUserDataRequest
                {
                    PlayFabId = input.playFabId,
                    Keys = new System.Collections.Generic.List<string> { "GuildAttendance" }
                };
                var getUserDataRes = await PlayFabServerAPI.GetUserDataAsync(getUserDataReq);
                if (getUserDataRes.Error != null)
                {
                    _logger.LogWarning("[GuildAttendance] GetUserData failed: " + getUserDataRes.Error.GenerateErrorReport());
                    return new BadRequestObjectResult("Failed to retrieve user data.");
                }

                string? guildAttendanceStr = null;
                if (getUserDataRes.Result.Data != null && getUserDataRes.Result.Data.ContainsKey("GuildAttendance"))
                {
                    guildAttendanceStr = getUserDataRes.Result.Data["GuildAttendance"].Value;
                }

                // 이미 오늘 출석했는지?
                if (guildAttendanceStr == todayString)
                {
                    var respAlready = new GuildAttendanceResponse
                    {
                        success = false,
                        message = "이미 오늘 길드 출석을 완료하셨습니다."
                    };
                    return new OkObjectResult(respAlready);
                }

                // 5) 아직 출석 안 했다면 -> 출석 기록
                var updateUserDataReq = new UpdateUserDataRequest
                {
                    PlayFabId = input.playFabId,
                    Data = new System.Collections.Generic.Dictionary<string, string>()
                    {
                        { "GuildAttendance", todayString }
                    }
                };
                var updateUserDataRes = await PlayFabServerAPI.UpdateUserDataAsync(updateUserDataReq);
                if (updateUserDataRes.Error != null)
                {
                    _logger.LogError("[GuildAttendance] UpdateUserData error: " + updateUserDataRes.Error.GenerateErrorReport());
                    return new BadRequestObjectResult("Failed to update GuildAttendance.");
                }
                _logger.LogInformation("[GuildAttendance] 출석 기록 완료.");

                // 6) 유저가 속한 길드 찾기 (ListMembership) – "title_player_account" 엔터티
                //    이때, 클라이언트에서 받은 entityToken은 "사용자" 권한
                //    여기서는 '서버 토큰'을 써서 API 호출 (AuthenticationContext = serverAuthContext)
                var listMembershipReq = new ListMembershipRequest
                {
                    AuthenticationContext = serverAuthContext,
                    Entity = new PlayFab.GroupsModels.EntityKey
                    {
                        Id = input.EntityToken.Entity.Id,   // (유저의 엔터티 ID)
                        Type = input.EntityToken.Entity.Type
                    }
                };
                var listMembershipRes = await PlayFabGroupsAPI.ListMembershipAsync(listMembershipReq);
                if (listMembershipRes.Error != null || listMembershipRes.Result.Groups == null || listMembershipRes.Result.Groups.Count == 0)
                {
                    _logger.LogInformation("[GuildAttendance] User not in any guild. Skipping guildExp update.");
                    var respNoGuild = new GuildAttendanceResponse
                    {
                        success = true,
                        message = "오늘 출석이 완료되었습니다! (길드 없음)"
                    };
                    return new OkObjectResult(respNoGuild);
                }

                var group = listMembershipRes.Result.Groups[0].Group;
                string groupId = group.Id;
                _logger.LogInformation($"[GuildAttendance] Found user's guild: {groupId}");

                // 7) 길드 Exp +100
                //    그룹 Object ("GuildInfo") 가져오기 -> guildExp += 100 -> SetObjects
                var getObjReq = new GetObjectsRequest
                {
                    AuthenticationContext = serverAuthContext,
                    Entity = new PlayFab.DataModels.EntityKey
                    {
                        Id = groupId,
                        Type = "group"
                    }
                };
                var getObjRes = await PlayFabDataAPI.GetObjectsAsync(getObjReq, serverAuthContext);
                if (getObjRes.Error != null || getObjRes.Result == null)
                {
                    _logger.LogWarning("[GuildAttendance] GetObjects failed: " + getObjRes.Error?.GenerateErrorReport());
                    // 길드 exp 갱신 실패 -> 출석 자체는 성공
                    var respFailObj = new GuildAttendanceResponse
                    {
                        success = true,
                        message = "오늘 출석이 완료되었습니다! (길드 Info 가져오기 실패)"
                    };
                    return new OkObjectResult(respFailObj);
                }

                int newExp = 0;
                if (getObjRes.Result.Objects != null && getObjRes.Result.Objects.ContainsKey("GuildInfo"))
                {
                    var infoObj = getObjRes.Result.Objects["GuildInfo"];
                    if (infoObj.DataObject != null)
                    {
                        var rawJson = JsonConvert.SerializeObject(infoObj.DataObject);
                        _logger.LogInformation("[GuildAttendance] GuildInfo raw: " + rawJson);

                        var guildInfo = JsonConvert.DeserializeObject<GuildInfoObject>(rawJson)!;
                        newExp = guildInfo.guildExp + 100;
                        guildInfo.guildExp = newExp;

                        // SetObjects -> guildExp 업데이트
                        var setReq = new SetObjectsRequest
                        {
                            AuthenticationContext = serverAuthContext,
                            Entity = new PlayFab.DataModels.EntityKey
                            {
                                Id = groupId,
                                Type = "group"
                            },
                            Objects = new System.Collections.Generic.List<SetObject>
                            {
                                new SetObject
                                {
                                    ObjectName = "GuildInfo",
                                    DataObject = guildInfo
                                }
                            }
                        };
                        var setRes = await PlayFabDataAPI.SetObjectsAsync(setReq, serverAuthContext);
                        if (setRes.Error != null)
                        {
                            _logger.LogWarning("[GuildAttendance] SetObjects(GuildInfo) error: " + setRes.Error.GenerateErrorReport());
                            // exp 갱신 실패 -> 출석 자체는 성공
                        }
                        else
                        {
                            _logger.LogInformation("[GuildAttendance] GuildExp updated to " + newExp);
                        }
                    }
                }

                // 8) 최종 응답
                var finalResp = new GuildAttendanceResponse
                {
                    success = true,
                    message = "오늘 길드 출석이 완료되었습니다!"
                };
                return new OkObjectResult(finalResp);
            }
            catch (Exception ex)
            {
                _logger.LogError("[GuildAttendance] Exception: " + ex.Message + "\n" + ex.StackTrace);
                return new BadRequestObjectResult("Internal server error in GuildAttendance.");
            }
        }
    }

    // 요청 DTO
    public class GuildAttendanceRequest
    {
        public string? playFabId { get; set; }
        public EntityTokenData? EntityToken { get; set; } // 클라이언트에서 entityToken 전체를 넘기는 구조
    }


    // 응답 DTO
    public class GuildAttendanceResponse
    {
        public bool success { get; set; }
        public string? message { get; set; }
    }
}
