using System;
using System.Collections.Generic;
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
using CommonLibrary;

namespace Farm
{
    public class PostFarmStatChange
    {
        private readonly ILogger<PostFarmStatChange> _logger;
        public PostFarmStatChange(ILogger<PostFarmStatChange> logger) => _logger = logger;

        [Function("PostFarmStatChange")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "PostFarmStatChange")] HttpRequest req)
        {
            PlayFabConfig.Configure();

            // 1) 요청 파싱
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var reqData = JsonConvert.DeserializeObject<FarmStatChangeRequestData>(body)
                          ?? throw new Exception("Invalid request");
            string? playFabId = reqData.PlayFabId;
            string? ability = reqData.AbilityName; // "ExpRate"|"GoldRate"|"ItemRate"
            _logger.LogInformation("[PostFarmStatChange] playFabId={0}, ability={1}", playFabId, ability);

            // 2) 현재 레벨 조회
            var statsRes = await PlayFabServerAPI.GetPlayerStatisticsAsync(
                new GetPlayerStatisticsRequest { PlayFabId = playFabId });
            if (statsRes.Error != null)
            {
                _logger.LogError(statsRes.Error.GenerateErrorReport(), "[PostFarmStatChange] 통계 조회 실패");
                return new OkObjectResult(new FarmStatResponse { IsSuccess = false, ErrorMessage = "통계 조회 실패" });
            }
            var statsList = statsRes.Result.Statistics;
            int currentLevel = statsList.FirstOrDefault(s => s.StatisticName == ability)?.Value ?? 0;

            // 3) TitleData 로드
            var tdRes = await PlayFabServerAPI.GetTitleDataAsync(
                new GetTitleDataRequest { Keys = new List<string> { "FarmLevelUpTable" } });
            var cfg = JsonConvert.DeserializeObject<FarmLevelUpTable>(tdRes.Result.Data["FarmLevelUpTable"])!;

            // 4) 다음 레벨업 비용 계산
            UpgradeSetting setting = ability switch
            {
                "ExpRate" => cfg.ExpRate!,
                "GoldRate" => cfg.GoldRate!,
                "ItemRate" => cfg.ItemRate!,
                _ => throw new Exception("Unknown ability")
            };
            if (currentLevel >= setting.maxLevel)
            {
                return new OkObjectResult(new FarmStatResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "최대 레벨 도달"
                });
            }
            int cost = setting.baseCost + setting.increment * currentLevel;

            // 5) 인벤토리 조회 후 비용 차감
            var invRes = await PlayFabServerAPI.GetUserInventoryAsync(
                new GetUserInventoryRequest { PlayFabId = playFabId });
            var vc = invRes.Result.VirtualCurrency ?? new Dictionary<string, int>();
            string currencyKey = ability switch
            {
                "ExpRate" => "Bamboo",
                "GoldRate" => "Carrot",
                "ItemRate" => "Corn",
                _ => throw new Exception()
            };
            vc.TryGetValue(currencyKey, out var have);
            if (have < cost)
            {
                return new OkObjectResult(new FarmStatResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "잔여 자원 부족"
                });
            }

            var subRes = await PlayFabServerAPI.SubtractUserVirtualCurrencyAsync(
                new SubtractUserVirtualCurrencyRequest
                {
                    PlayFabId = playFabId,
                    VirtualCurrency = currencyKey,
                    Amount = cost
                });
            if (subRes.Error != null)
            {
                _logger.LogError(subRes.Error.GenerateErrorReport(), "[PostFarmStatChange] 비용 차감 실패");
                return new OkObjectResult(new FarmStatResponse { IsSuccess = false, ErrorMessage = "차감 실패" });
            }

            // 6) 레벨업 (통계 업데이트)
            var updRes = await PlayFabServerAPI.UpdatePlayerStatisticsAsync(
                new UpdatePlayerStatisticsRequest
                {
                    PlayFabId = playFabId,
                    Statistics = new List<StatisticUpdate>
                    {
                        new StatisticUpdate { StatisticName = ability, Value = currentLevel + 1 }
                    }
                });
            if (updRes.Error != null)
            {
                _logger.LogError(updRes.Error.GenerateErrorReport(), "[PostFarmStatChange] 레벨업 실패");
                return new OkObjectResult(new FarmStatResponse { IsSuccess = false, ErrorMessage = "레벨업 실패" });
            }

            // 7) 갱신된 스탯·인벤토리 상태를 다시 조회하고 응답
            //    (단순히 getFarmStat 호출 결과 형식을 재현)
            var getStatResult = await RunStatQuery(playFabId);
            return new OkObjectResult(getStatResult);
        }

        private class FarmLevelUpTable
        {
            public UpgradeSetting? ExpRate { get; set; }
            public UpgradeSetting? GoldRate { get; set; }
            public UpgradeSetting? ItemRate { get; set; }
        }
        private class UpgradeSetting
        {
            public int baseCost { get; set; }
            public int increment { get; set; }
            public int maxLevel { get; set; }
        }
        // getFarmStat 로직 재사용
        private async Task<FarmStatResponse> RunStatQuery(string? playFabId)
        {
            // 위 GetFarmStat Run 내부와 동일하게 구현
            // ...
            throw new NotImplementedException();
        }
    }
}
