// UpdateClearedFloor.cs
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
using CommonLibrary;  // PlayFabConfig

namespace Dungeon
{
    public class UpdateClearedFloor
    {
        private readonly ILogger<UpdateClearedFloor> _logger;
        public UpdateClearedFloor(ILogger<UpdateClearedFloor> logger) => _logger = logger;

        [Function("UpdateClearedFloor")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "UpdateClearedFloor")] HttpRequest req)
        {
            _logger.LogInformation("[UpdateClearedFloor] 함수 호출됨");
            PlayFabConfig.Configure();

            // 1) 요청 파싱
            string body = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data;
            try
            {
                data = JsonConvert.DeserializeObject(body)!;
                _logger.LogInformation("[UpdateClearedFloor] playFabId={PlayFabId}, highestClearedFloor={Floor}",
                    (string)data.playFabId!, (int)data.highestClearedFloor!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateClearedFloor] 요청 파싱 오류");
                return new BadRequestObjectResult("잘못된 요청 형식");
            }

            string playFabId = data.playFabId!;
            int newClearedFloor = (int)data.highestClearedFloor!;

            // 2) Player Statistics 업데이트
            _logger.LogInformation("[UpdateClearedFloor] UpdatePlayerStatisticsAsync 호출");
            var updRes = await PlayFabServerAPI.UpdatePlayerStatisticsAsync(new UpdatePlayerStatisticsRequest
            {
                PlayFabId = playFabId,
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate
                    {
                        StatisticName = "DungeonFloor",
                        Value = newClearedFloor
                    }
                }
            });

            if (updRes.Error != null)
            {
                _logger.LogError(updRes.Error.GenerateErrorReport(), "[UpdateClearedFloor] 통계 업데이트 실패");
                return new OkObjectResult(new DungeonResponseData
                {
                    IsSuccess = false,
                    TicketCount = 0,
                    HighestClearedFloor = 0
                });
            }

            _logger.LogInformation("[UpdateClearedFloor] DungeonFloor 업데이트 성공: {Floor}", newClearedFloor);

            // 3) (옵션) 티켓 소비 로직이 필요하면 여기에 추가
            //    예: SubtractUserVirtualCurrencyAsync(new SubtractUserVirtualCurrencyRequest{ ... })

            // 4) 응답 (성공 여부만)
            return new OkObjectResult(new DungeonResponseData
            {
                IsSuccess = true,
                TicketCount = 0,             // 업데이트 후에도 별도로 티켓을 반환할 필요가 없으면 0
                HighestClearedFloor = newClearedFloor
            });
        }
    }
}
