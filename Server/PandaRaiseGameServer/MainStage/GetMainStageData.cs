// GetMainStageData.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using CommonLibrary;

namespace MainStage
{
    public class GetMainStageData
    {
        private readonly ILogger<GetMainStageData> _logger;
        public GetMainStageData(ILogger<GetMainStageData> logger) => _logger = logger;

        [Function("GetMainStageData")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "GetMainStageData")] HttpRequest req)
        {
            _logger.LogInformation("[GetMainStageData] 함수 호출됨");
            PlayFabConfig.Configure();

            // 1) 요청 파싱
            string body = await new StreamReader(req.Body).ReadToEndAsync();
            MainStageRequestData requestData;
            try
            {
                requestData = JsonConvert.DeserializeObject<MainStageRequestData>(body)
                              ?? throw new Exception("Invalid request");
                _logger.LogInformation("[GetMainStageData] playFabId={PlayFabId}", requestData.PlayFabId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetMainStageData] 요청 파싱 오류");
                return new BadRequestObjectResult("잘못된 요청 형식");
            }

            string playFabId = requestData.PlayFabId!;

            // 2) 플레이어 통계 조회 (MainStageData)
            _logger.LogInformation("[GetMainStageData] GetPlayerStatisticsAsync 호출");
            var statsRes = await PlayFabServerAPI.GetPlayerStatisticsAsync(new GetPlayerStatisticsRequest
            {
                PlayFabId = playFabId
            });

            int currentFloor = 0;
            if (statsRes.Error != null)
            {
                _logger.LogError(statsRes.Error.GenerateErrorReport(), "[GetMainStageData] GetPlayerStatisticsAsync 오류");
                // 통계 조회 실패 시 floor=0 상태로 응답
            }
            else
            {
                var found = statsRes.Result.Statistics.Find(s => s.StatisticName == "MainStageData");
                if (found != null)
                {
                    currentFloor = found.Value;
                    _logger.LogInformation("[GetMainStageData] 기존 MainStageData={CurrentFloor}", currentFloor);
                }
                else
                {
                    // 없으면 0으로 초기화하여 통계 생성
                    _logger.LogInformation("[GetMainStageData] MainStageData 통계 없음 → 0 초기화");
                    await PlayFabServerAPI.UpdatePlayerStatisticsAsync(new UpdatePlayerStatisticsRequest
                    {
                        PlayFabId = playFabId,
                        Statistics = new List<StatisticUpdate>
                        {
                            new StatisticUpdate { StatisticName = "MainStageData", Value = 0 }
                        }
                    });
                    currentFloor = 1;
                }
            }

            // 3) 응답
            var response = new MainStageResponseData
            {
                IsSuccess = true,
                CurrentFloor = currentFloor
            };
            _logger.LogInformation("[GetMainStageData] 응답: CurrentFloor={CurrentFloor}", currentFloor);
            return new OkObjectResult(response);
        }
    }
}
