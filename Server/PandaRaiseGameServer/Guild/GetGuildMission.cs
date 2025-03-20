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
using PlayFab.ServerModels; // TitleData 관련
using CommonLibrary;

namespace Guild
{
    public class GetGuildMissionInfo
    {
        private readonly ILogger<GetGuildMissionInfo> _logger;

        public GetGuildMissionInfo(ILogger<GetGuildMissionInfo> logger)
        {
            _logger = logger;
        }

        [Function("GetGuildMissionInfo")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("[GetGuildMissionInfo] Function invoked.");

                // 1) 요청 바디 (현재는 의미 없음, 예시로만 읽고 무시)
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation("[GetGuildMissionInfo] Request body: " + requestBody);

                // 2) PlayFab 초기 설정 (TitleId, SecretKey 등)
                PlayFabConfig.Configure();
                _logger.LogInformation("[GetGuildMissionInfo] PlayFab configured.");

                // 3) TitleData에서 "GuildMissionList" 키의 데이터를 불러오기
                var getTitleDataReq = new GetTitleDataRequest
                {
                    Keys = new List<string> { "GuildMissionList" }
                };
                var getTitleDataRes = await PlayFabServerAPI.GetTitleDataAsync(getTitleDataReq);
                if (getTitleDataRes.Error != null)
                {
                    _logger.LogError("[GetGuildMissionInfo] GetTitleData Error: " + getTitleDataRes.Error.GenerateErrorReport());
                    return new BadRequestObjectResult("Failed to retrieve TitleData");
                }

                string? rawJson = null;
                if (getTitleDataRes.Result.Data != null && getTitleDataRes.Result.Data.ContainsKey("GuildMissionList"))
                {
                    rawJson = getTitleDataRes.Result.Data["GuildMissionList"];
                    _logger.LogInformation("[GetGuildMissionInfo] Retrieved GuildMissionList JSON: " + rawJson);
                }
                else
                {
                    _logger.LogWarning("[GetGuildMissionInfo] TitleData 'GuildMissionList' not found or empty.");
                    // TitleData가 없으면 빈 배열로 반환
                    rawJson = "[]";
                }

                // 4) 서버 응답: GuildMissionData를 문자열로 감싸서 클라이언트에 전달
                var response = new GuildMissionResponse
                {
                    GuildMissionData = rawJson
                };

                // 디버그 로그
                string debugJson = JsonConvert.SerializeObject(response);
                _logger.LogInformation("[GetGuildMissionInfo] Final response: " + debugJson);

                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError("[GetGuildMissionInfo] Exception: " + ex.Message + "\n" + ex.StackTrace);
                return new BadRequestObjectResult("Internal server error in GetGuildMissionInfo.");
            }
        }
    }

    /// <summary>
    /// 서버에서 내려줄 DTO
    /// </summary>
    public class GuildMissionResponse
    {
        // 클라이언트가 받아서 역직렬화할 원본 JSON
        public string? GuildMissionData { get; set; }
    }
}
