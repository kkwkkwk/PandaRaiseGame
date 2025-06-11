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
using CommonLibrary; // PlayFabConfig

namespace Farm
{
    public class GetFarmStat
    {
        private readonly ILogger<GetFarmStat> _logger;
        public GetFarmStat(ILogger<GetFarmStat> logger) => _logger = logger;

        [Function("GetFarmStat")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "GetFarmStat")] HttpRequest req)
        {
            PlayFabConfig.Configure();

            // 1) 요청 파싱
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var reqData = JsonConvert.DeserializeObject<FarmStatRequestData>(body)
                          ?? throw new Exception("Invalid request");
            string? playFabId = reqData.PlayFabId;
            _logger.LogInformation("[GetFarmStat] playFabId={0}", playFabId);

            // 2) 인벤토리 조회 (VirtualCurrency: Bamboo, Carrot, Corn)
            var invRes = await PlayFabServerAPI.GetUserInventoryAsync(
                new GetUserInventoryRequest { PlayFabId = playFabId });
            if (invRes.Error != null)
            {
                _logger.LogError(invRes.Error.GenerateErrorReport(), "[GetFarmStat] 인벤토리 조회 실패");
                return new OkObjectResult(new FarmStatResponse { IsSuccess = false, ErrorMessage = "인벤토리 조회 실패" });
            }
            var vc = invRes.Result.VirtualCurrency ?? new Dictionary<string, int>();
            int remBamboo = vc.TryGetValue("Bamboo", out var b) ? b : 0;
            int remCarrot = vc.TryGetValue("Carrot", out var c) ? c : 0;
            int remCorn = vc.TryGetValue("Corn", out var d) ? d : 0;

            // 3) 플레이어 통계 조회 (ExpRate, GoldRate, ItemRate 레벨)
            var statsRes = await PlayFabServerAPI.GetPlayerStatisticsAsync(
                new GetPlayerStatisticsRequest { PlayFabId = playFabId });
            if (statsRes.Error != null)
            {
                _logger.LogError(statsRes.Error.GenerateErrorReport(), "[GetFarmStat] 통계 조회 실패");
                return new OkObjectResult(new FarmStatResponse { IsSuccess = false, ErrorMessage = "통계 조회 실패" });
            }
            var stats = statsRes.Result.Statistics;
            int lvlExp = stats.FirstOrDefault(s => s.StatisticName == "ExpRate")?.Value ?? 0;
            int lvlGold = stats.FirstOrDefault(s => s.StatisticName == "GoldRate")?.Value ?? 0;
            int lvlItem = stats.FirstOrDefault(s => s.StatisticName == "ItemRate")?.Value ?? 0;

            // 4) TitleData 로드 (FarmLevelUpTable)
            var tdRes = await PlayFabServerAPI.GetTitleDataAsync(
                new GetTitleDataRequest { Keys = new List<string> { "FarmLevelUpTable" } });
            var cfgJson = tdRes.Result.Data["FarmLevelUpTable"];
            var cfg = JsonConvert.DeserializeObject<FarmLevelUpTable>(cfgJson)!;

            // 5) 다음 레벨업 비용 계산
            int costExp = lvlExp < cfg.ExpRate!.maxLevel ? cfg.ExpRate.baseCost + cfg.ExpRate.increment * lvlExp : 0;
            int costGold = lvlGold < cfg.GoldRate!.maxLevel ? cfg.GoldRate.baseCost + cfg.GoldRate.increment * lvlGold : 0;
            int costItem = lvlItem < cfg.ItemRate!.maxLevel ? cfg.ItemRate.baseCost + cfg.ItemRate.increment * lvlItem : 0;

            // 6) AbilityStatData 리스트 생성
            var abilityStats = new List<AbilityStatData>
            {
                new AbilityStatData { Name = "ExpRate",  Level = lvlExp,  CostStat = costExp  },
                new AbilityStatData { Name = "GoldRate", Level = lvlGold, CostStat = costGold },
                new AbilityStatData { Name = "ItemRate", Level = lvlItem, CostStat = costItem }
            };

            // 7) 응답
            return new OkObjectResult(new FarmStatResponse
            {
                IsSuccess = true,
                RemainingBamboo = remBamboo,
                RemainingCarrot = remCarrot,
                RemainingCorn = remCorn,
                AbilityStats = abilityStats
            });
        }

        // TitleData 파싱용 모델
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
    }
}
