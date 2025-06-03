// GetOfflineReward.cs
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
using CommonLibrary; // PlayFabConfig

namespace Offline
{
    public class GetOfflineReward
    {
        private readonly ILogger<GetOfflineReward> _logger;
        public GetOfflineReward(ILogger<GetOfflineReward> logger) => _logger = logger;

        [Function("GetOfflineReward")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "GetOfflineReward")] HttpRequest req)
        {
            _logger.LogInformation("[GetOfflineReward] 함수 호출됨");
            PlayFabConfig.Configure();

            // 1) 요청 파싱
            string body = await new StreamReader(req.Body).ReadToEndAsync();
            OfflineRewardRequestData requestData;
            try
            {
                requestData = JsonConvert.DeserializeObject<OfflineRewardRequestData>(body)
                              ?? throw new Exception("Invalid request");
                _logger.LogInformation(
                    "[GetOfflineReward] playFabId={PlayFabId}",
                    requestData.PlayFabId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetOfflineReward] 요청 파싱 오류");
                return new BadRequestObjectResult("잘못된 요청 형식");
            }

            string playFabId = requestData.PlayFabId!;
            DateTime nowUtc = DateTime.UtcNow;

            // 2) UserData에서 lastHeartbeat 읽어오기
            _logger.LogInformation("[GetOfflineReward] GetUserDataAsync 호출 (lastHeartbeat 조회)");
            var udRes = await PlayFabServerAPI.GetUserDataAsync(new GetUserDataRequest
            {
                PlayFabId = playFabId,
                Keys = new List<string> { "lastHeartbeat" }
            });

            if (udRes.Error != null)
            {
                _logger.LogError(
                    udRes.Error.GenerateErrorReport(),
                    "[GetOfflineReward] GetUserDataAsync 오류");
                return new OkObjectResult(new OfflineRewardResponseData
                {
                    IsSuccess = false,
                    OfflineSeconds = 0,
                    CurrentFloor = 0,
                    RewardAmount = 0
                });
            }

            // 3) 이전에 기록된 lastHeartbeat 파싱
            int offlineSeconds = 0;
            if (udRes.Result.Data.TryGetValue("lastHeartbeat", out var hbJson)
                && !string.IsNullOrEmpty(hbJson.Value))
            {
                if (DateTime.TryParse(hbJson.Value, out var lastUtc))
                {
                    offlineSeconds = Convert.ToInt32((nowUtc - lastUtc).TotalSeconds);
                    _logger.LogInformation(
                        "[GetOfflineReward] lastHeartbeat={LastUtc}, now={NowUtc}, offlineSeconds={OfflineSeconds}",
                        lastUtc, nowUtc, offlineSeconds);
                }
                else
                {
                    _logger.LogWarning(
                        "[GetOfflineReward] lastHeartbeat 파싱 실패(값={Value}), offlineSeconds=0 처리",
                        hbJson.Value);
                }
            }
            else
            {
                // 기록이 없으면 최초 호출 → offlineSeconds=0
                _logger.LogInformation("[GetOfflineReward] lastHeartbeat 기록 없음 → offlineSeconds=0");
            }

            // 4) Player Statistics 조회 -> MainStageData (currentFloor)
            _logger.LogInformation("[GetOfflineReward] GetPlayerStatisticsAsync 호출 (MainStageData 조회)");
            var statsRes = await PlayFabServerAPI.GetPlayerStatisticsAsync(new GetPlayerStatisticsRequest
            {
                PlayFabId = playFabId
            });

            int currentFloor = 0;
            if (statsRes.Error != null)
            {
                _logger.LogError(
                    statsRes.Error.GenerateErrorReport(),
                    "[GetOfflineReward] GetPlayerStatisticsAsync 오류");
            }
            else
            {
                var found = statsRes.Result.Statistics.Find(s => s.StatisticName == "MainStageData");
                if (found != null)
                {
                    currentFloor = found.Value;
                    _logger.LogInformation("[GetOfflineReward] MainStageData={CurrentFloor}", currentFloor);
                }
                else
                {
                    // 통계가 없으면 0으로 초기화
                    _logger.LogInformation("[GetOfflineReward] MainStageData 통계 없음 → 0 초기화");
                    await PlayFabServerAPI.UpdatePlayerStatisticsAsync(new UpdatePlayerStatisticsRequest
                    {
                        PlayFabId = playFabId,
                        Statistics = new List<StatisticUpdate>
                        {
                            new StatisticUpdate { StatisticName = "MainStageData", Value = 0 }
                        }
                    });
                }
            }

            // 5) 보상 계산 (예시: offlineSeconds × currentFloor)
            int rewardAmount = offlineSeconds * currentFloor;
            _logger.LogInformation(
                "[GetOfflineReward] 계산된 보상: offlineSeconds={OfflineSeconds} × currentFloor={CurrentFloor} = rewardAmount={RewardAmount}",
                offlineSeconds, currentFloor, rewardAmount);

            // 6) 가상화폐(GC)로 보상 지급
            if (rewardAmount > 0)
            {
                _logger.LogInformation("[GetOfflineReward] AddUserVirtualCurrencyAsync 호출 (GC={RewardAmount})", rewardAmount);
                var addRes = await PlayFabServerAPI.AddUserVirtualCurrencyAsync(new AddUserVirtualCurrencyRequest
                {
                    PlayFabId = playFabId,
                    VirtualCurrency = "GC",     // PlayFab에 미리 등록된 골드 코드
                    Amount = rewardAmount
                });
                if (addRes.Error != null)
                {
                    _logger.LogError(
                        addRes.Error.GenerateErrorReport(),
                        "[GetOfflineReward] AddUserVirtualCurrencyAsync 오류");
                    return new OkObjectResult(new OfflineRewardResponseData
                    {
                        IsSuccess = false,
                        OfflineSeconds = offlineSeconds,
                        CurrentFloor = currentFloor,
                        RewardAmount = 0
                    });
                }
                _logger.LogInformation("[GetOfflineReward] 보상 지급 성공: GD={RewardAmount}", rewardAmount);
            }
            else
            {
                _logger.LogInformation("[GetOfflineReward] 보상 금액이 0이므로 지급 생략");
            }

            // 7) 응답
            var response = new OfflineRewardResponseData
            {
                IsSuccess = true,
                OfflineSeconds = offlineSeconds,
                CurrentFloor = currentFloor,
                RewardAmount = rewardAmount
            };
            _logger.LogInformation(
                "[GetOfflineReward] 응답: offlineSeconds={OfflineSeconds}, currentFloor={CurrentFloor}, rewardAmount={RewardAmount}",
                offlineSeconds, currentFloor, rewardAmount);
            return new OkObjectResult(response);
        }
    }
}
