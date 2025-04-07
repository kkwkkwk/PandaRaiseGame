using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using PlayFab;
using PlayFab.AuthenticationModels;
using PlayFab.DataModels; // For GetObjectsRequest/SetObjectsRequest
using PlayFab.GroupsModels;
using CommonLibrary;

namespace Guild
{
    public class UpdateGuildNotice
    {
        private readonly ILogger<UpdateGuildNotice> _logger;

        public UpdateGuildNotice(ILogger<UpdateGuildNotice> logger)
        {
            _logger = logger;
        }

        [Function("UpdateGuildNotice")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("[UpdateGuildNotice] Function invoked.");

                // 1) 요청 바디
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var input = JsonConvert.DeserializeObject<UpdateGuildNoticeRequest>(requestBody);
                if (input == null || input.EntityToken == null)
                {
                    _logger.LogWarning("[UpdateGuildNotice] Missing entityToken/newNotice.");
                    return new BadRequestObjectResult("Invalid request: entityToken, newNotice required.");
                }

                // 2) 서버 토큰 획득
                PlayFabConfig.Configure();
                var getTokenRes = await PlayFabAuthenticationAPI.GetEntityTokenAsync(new GetEntityTokenRequest());
                if (getTokenRes.Error != null)
                {
                    _logger.LogError("[UpdateGuildNotice] GetEntityToken error: " + getTokenRes.Error.GenerateErrorReport());
                    return new BadRequestObjectResult("Error fetching server token");
                }
                var serverAuth = new PlayFabAuthenticationContext
                {
                    EntityId = getTokenRes.Result.Entity.Id,
                    EntityType = getTokenRes.Result.Entity.Type,
                    EntityToken = getTokenRes.Result.EntityToken
                };

                // 3) 클라이언트 엔티티 -> ListMembership → 첫 번째 길드를 찾거나, 필요 시 특정 길드 로직
                var listMemReq = new ListMembershipRequest
                {
                    AuthenticationContext = serverAuth,
                    Entity = new PlayFab.GroupsModels.EntityKey
                    {
                        Id = input.EntityToken.Entity?.Id,
                        Type = input.EntityToken.Entity?.Type
                    }
                };
                var listMemRes = await PlayFabGroupsAPI.ListMembershipAsync(listMemReq);
                if (listMemRes.Error != null || listMemRes.Result.Groups == null || listMemRes.Result.Groups.Count == 0)
                {
                    _logger.LogWarning("[UpdateGuildNotice] User not in any group");
                    return new OkObjectResult(new UpdateGuildNoticeResponse
                    {
                        success = false,
                        message = "길드에 속해있지 않습니다."
                    });
                }
                var group = listMemRes.Result.Groups[0].Group;
                string groupId = group.Id;

                // 4) GetObjects("GuildInfo") → guildIntro 수정
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
                    _logger.LogWarning("[UpdateGuildNotice] GetObjects error: " + getObjRes.Error.GenerateErrorReport());
                    return new OkObjectResult(new UpdateGuildNoticeResponse
                    {
                        success = false,
                        message = "길드 정보 조회 실패"
                    });
                }

                GuildInfo? guildInfo = new GuildInfo();
                if (getObjRes.Result.Objects != null && getObjRes.Result.Objects.ContainsKey("GuildInfo"))
                {
                    var raw = getObjRes.Result.Objects["GuildInfo"].DataObject;
                    if (raw != null)
                    {
                        var rawJson = JsonConvert.SerializeObject(raw);
                        guildInfo = JsonConvert.DeserializeObject<GuildInfo>(rawJson);
                    }
                }

                // guildIntro(또는 notice) 업데이트
                #pragma warning disable CS8602
                guildInfo.guildIntro = input.newNotice;
                #pragma warning restore CS8602

                // 5) SetObjects
                var setReq = new SetObjectsRequest
                {
                    AuthenticationContext = serverAuth,
                    Entity = new PlayFab.DataModels.EntityKey
                    {
                        Id = groupId,
                        Type = "group"
                    },
                    Objects = new List<SetObject>
                    {
                        new SetObject
                        {
                            ObjectName = "GuildInfo",
                            DataObject = guildInfo
                        }
                    }
                };
                var setRes = await PlayFabDataAPI.SetObjectsAsync(setReq, serverAuth);
                if (setRes.Error != null)
                {
                    _logger.LogWarning("[UpdateGuildNotice] SetObjects error: " + setRes.Error.GenerateErrorReport());
                    return new OkObjectResult(new UpdateGuildNoticeResponse
                    {
                        success = false,
                        message = "길드 공지 저장 실패"
                    });
                }

                // 성공
                return new OkObjectResult(new UpdateGuildNoticeResponse
                {
                    success = true,
                    message = "길드 공지 수정 성공"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("[UpdateGuildNotice] Exception: " + ex.Message);
                return new BadRequestObjectResult("Internal server error in UpdateGuildNotice.");
            }
        }
    }

    // DTO
    public class UpdateGuildNoticeRequest
    {
        public EntityTokenData? EntityToken { get; set; }
        public string? newNotice { get; set; }
    }

    public class UpdateGuildNoticeResponse
    {
        public bool success { get; set; }
        public string? message { get; set; }
    }

    public class GuildInfo
    {
        public int guildLevel { get; set; } = 1;
        public int guildExp { get; set; } = 0;
        public string? guildIntro { get; set; } = "길드를 소개해주세요";
    }
}
