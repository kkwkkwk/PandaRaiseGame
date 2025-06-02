// GetDungeonData.cs
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

namespace Dungeon
{
    public class GetDungeonData
    {
        private readonly ILogger<GetDungeonData> _logger;
        public GetDungeonData(ILogger<GetDungeonData> logger) => _logger = logger;

        [Function("GetDungeonData")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "GetDungeonData")] HttpRequest req)
        {
            _logger.LogInformation("[GetDungeonData] 함수 호출됨");
            PlayFabConfig.Configure();

            // 1) 요청 파싱
            string body = await new StreamReader(req.Body).ReadToEndAsync();
            DungeonRequestData requestData;
            try
            {
                requestData = JsonConvert.DeserializeObject<DungeonRequestData>(body)
                              ?? throw new Exception("Invalid request");
                _logger.LogInformation("[GetDungeonData] playFabId={PlayFabId}", requestData.PlayFabId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetDungeonData] 요청 파싱 오류");
                return new BadRequestObjectResult("잘못된 요청 형식");
            }

            string playFabId = requestData.PlayFabId!;

            // 2) 유저 인벤토리 조회 → VirtualCurrency (티켓 “DT”)
            _logger.LogInformation("[GetDungeonData] GetUserInventoryAsync 호출");
            var invRes = await PlayFabServerAPI.GetUserInventoryAsync(new GetUserInventoryRequest
            {
                PlayFabId = playFabId
            });
            if (invRes.Error != null)
            {
                _logger.LogError(invRes.Error.GenerateErrorReport(), "[GetDungeonData] GetUserInventoryAsync 오류");
                return new OkObjectResult(new DungeonResponseData
                {
                    IsSuccess = false,
                    TicketCount = 0,
                    HighestClearedFloor = 0
                });
            }

            var vc = invRes.Result.VirtualCurrency ?? new Dictionary<string, int>();
            int ticketCount = vc.TryGetValue("DT", out var dtCount) ? dtCount : 0;
            _logger.LogInformation("[GetDungeonData] 사용자 DT={TicketCount}", ticketCount);

            // 3) 유저 통계 조회 → DungeonFloor
            _logger.LogInformation("[GetDungeonData] GetPlayerStatisticsAsync 호출");
            var statsRes = await PlayFabServerAPI.GetPlayerStatisticsAsync(new GetPlayerStatisticsRequest
            {
                PlayFabId = playFabId
            });
            int highestFloor = 0;
            if (statsRes.Error != null)
            {
                _logger.LogError(statsRes.Error.GenerateErrorReport(), "[GetDungeonData] GetPlayerStatisticsAsync 오류");
                // 통계 조회 실패 시에도 floor=0으로 응답
            }
            else
            {
                var found = statsRes.Result.Statistics
                             .Find(s => s.StatisticName == "DungeonFloor");
                if (found != null)
                {
                    highestFloor = found.Value;
                    _logger.LogInformation("[GetDungeonData] 기존 DungeonFloor={HighestFloor}", highestFloor);
                }
                else
                {
                    // 4) 통계가 없으면 0으로 초기화해서 저장
                    _logger.LogInformation("[GetDungeonData] DungeonFloor 통계 없음 → 0으로 초기화");
                    await PlayFabServerAPI.UpdatePlayerStatisticsAsync(new UpdatePlayerStatisticsRequest
                    {
                        PlayFabId = playFabId,
                        Statistics = new List<StatisticUpdate>
                        {
                            new StatisticUpdate { StatisticName = "DungeonFloor", Value = 0 }
                        }
                    });
                    highestFloor = 0;
                }
            }

            // 5) 응답
            var response = new DungeonResponseData
            {
                IsSuccess = true,
                TicketCount = ticketCount,
                HighestClearedFloor = highestFloor
            };
            _logger.LogInformation("[GetDungeonData] 응답: Tickets={TicketCount}, Floor={HighestFloor}",
                ticketCount, highestFloor);
            return new OkObjectResult(response);
        }
    }
}
