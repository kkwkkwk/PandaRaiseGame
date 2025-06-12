// GetFarmStat.cs
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
            _logger.LogInformation("[GetFarmStat] Function start");

            // 1) 요청 파싱
            string body = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation("[GetFarmStat] Raw request body: {Body}", body);
            var reqData = JsonConvert.DeserializeObject<FarmStatRequestData>(body);
            if (reqData == null || string.IsNullOrEmpty(reqData.PlayFabId))
            {
                _logger.LogWarning("[GetFarmStat] Invalid request data");
                return new BadRequestObjectResult("Invalid request");
            }
            string playFabId = reqData.PlayFabId;
            _logger.LogInformation("[GetFarmStat] Parsed playFabId: {PlayFabId}", playFabId);

            // 2) 인벤토리 조회 (Consumable items: BA, CA, CO)
            _logger.LogInformation("[GetFarmStat] Retrieving user inventory");
            var invRes = await PlayFabServerAPI.GetUserInventoryAsync(
                new GetUserInventoryRequest { PlayFabId = playFabId });
            if (invRes.Error != null)
            {
                _logger.LogError(invRes.Error.GenerateErrorReport(), "[GetFarmStat] Inventory fetch failed");
                return new OkObjectResult(new FarmStatResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "인벤토리 조회 실패"
                });
            }
            var invList = invRes.Result.Inventory;
            // itemId "BA" = Bamboo, "CA" = Carrot, "CO" = Corn
            int remBamboo = invList
                .FirstOrDefault(i => i.ItemId == "BA")
                ?.RemainingUses
                .GetValueOrDefault() ?? 0;

            int remCarrot = invList
                .FirstOrDefault(i => i.ItemId == "CA")
                ?.RemainingUses
                .GetValueOrDefault() ?? 0;

            int remCorn = invList
                .FirstOrDefault(i => i.ItemId == "CO")
                ?.RemainingUses
                .GetValueOrDefault() ?? 0;

            _logger.LogInformation(
                "[GetFarmStat] Remaining items - BA: {B}, CA: {C}, CO: {D}",
                remBamboo, remCarrot, remCorn);

            // 3) 플레이어 통계 조회 (ExpRate, GoldRate, ItemRate 레벨)
            _logger.LogInformation("[GetFarmStat] Retrieving player statistics");
            var statsRes = await PlayFabServerAPI.GetPlayerStatisticsAsync(
                new GetPlayerStatisticsRequest { PlayFabId = playFabId });
            if (statsRes.Error != null)
            {
                _logger.LogError(statsRes.Error.GenerateErrorReport(), "[GetFarmStat] Statistics fetch failed");
                return new OkObjectResult(new FarmStatResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "통계 조회 실패"
                });
            }
            var stats = statsRes.Result.Statistics;
            int lvlExp = stats.FirstOrDefault(s => s.StatisticName == "ExpRate")?.Value ?? 0;
            int lvlGold = stats.FirstOrDefault(s => s.StatisticName == "GoldRate")?.Value ?? 0;
            int lvlItem = stats.FirstOrDefault(s => s.StatisticName == "ItemRate")?.Value ?? 0;
            _logger.LogInformation("[GetFarmStat] Levels - ExpRate: {E}, GoldRate: {G}, ItemRate: {I}",
                lvlExp, lvlGold, lvlItem);

            // 4) TitleData 로드 (FarmLevelUpTable)
            _logger.LogInformation("[GetFarmStat] Loading TitleData 'FarmLevelUpTable'");
            var tdRes = await PlayFabServerAPI.GetTitleDataAsync(
                new GetTitleDataRequest { Keys = new List<string> { "FarmLevelUpTable" } });
            if (tdRes.Error != null || !tdRes.Result.Data.TryGetValue("FarmLevelUpTable", out var cfgJson))
            {
                _logger.LogError(tdRes.Error?.GenerateErrorReport(), "[GetFarmStat] TitleData fetch failed");
                return new OkObjectResult(new FarmStatResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "레벨업 테이블 조회 실패"
                });
            }
            var cfg = JsonConvert.DeserializeObject<FarmLevelUpTable>(cfgJson)!;
            _logger.LogInformation("[GetFarmStat] TitleData loaded");

            // 5) 다음 레벨업 비용 계산
            int costExp = lvlExp < cfg.ExpRate!.maxLevel ? cfg.ExpRate.baseCost + cfg.ExpRate.increment * lvlExp : 0;
            int costGold = lvlGold < cfg.GoldRate!.maxLevel ? cfg.GoldRate.baseCost + cfg.GoldRate.increment * lvlGold : 0;
            int costItem = lvlItem < cfg.ItemRate!.maxLevel ? cfg.ItemRate.baseCost + cfg.ItemRate.increment * lvlItem : 0;
            _logger.LogInformation("[GetFarmStat] Upgrade costs - ExpRate: {CE}, GoldRate: {CG}, ItemRate: {CI}",
                costExp, costGold, costItem);

            // 6) AbilityStatData 리스트 생성
            var abilityStats = new List<AbilityStatData>
            {
                new AbilityStatData { Name = "ExpRate",  Level = lvlExp,  CostStat = costExp  },
                new AbilityStatData { Name = "GoldRate", Level = lvlGold, CostStat = costGold },
                new AbilityStatData { Name = "ItemRate", Level = lvlItem, CostStat = costItem }
            };

            // 7) 응답
            _logger.LogInformation("[GetFarmStat] Returning response");
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
