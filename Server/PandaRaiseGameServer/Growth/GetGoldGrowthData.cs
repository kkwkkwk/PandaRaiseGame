using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using Growth;
using CommonLibrary;

namespace Growth
{
    public class GetGoldGrowthData
    {
        private static readonly (string ability, string stat)[] Map =
        {
            ("공격력", "AtkLevel"),
            ("체력",   "HpLevel")
        };

        [Function("GetGoldGrowthData")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            string? playFabId = JsonConvert.DeserializeObject<dynamic>(body)?.playFabId;
            if (string.IsNullOrEmpty(playFabId))
                return new BadRequestObjectResult("playFabId 누락");

            PlayFabConfig.Configure();

            // ── 통계 조회 ──────────────────────────────
            var statRes = await PlayFabServerAPI.GetPlayerStatisticsAsync(
                new GetPlayerStatisticsRequest { PlayFabId = playFabId });
            if (statRes.Error != null)
                return new BadRequestObjectResult(statRes.Error.GenerateErrorReport());

            var statDict = statRes.Result.Statistics
                             .ToDictionary(s => s.StatisticName, s => s.Value);

            // 없으면 0 으로 만들고, 최초 호출이라면 통계 생성
            var statsToInit = new List<StatisticUpdate>();
            foreach (var (_, stat) in Map)
            {
                if (!statDict.ContainsKey(stat))
                    statsToInit.Add(new StatisticUpdate { StatisticName = stat, Value = 0 });
            }
            if (statsToInit.Count > 0)
                await PlayFabServerAPI.UpdatePlayerStatisticsAsync(
                    new UpdatePlayerStatisticsRequest
                    {
                        PlayFabId = playFabId,
                        Statistics = statsToInit
                    });

            // ── 골드 조회 ───────────────────────────────
            var inv = await PlayFabServerAPI.GetUserInventoryAsync(
                new GetUserInventoryRequest { PlayFabId = playFabId });
            if (inv.Error != null)
                return new BadRequestObjectResult(inv.Error.GenerateErrorReport());
            int playerGold = inv.Result.VirtualCurrency?.GetValueOrDefault("GC") ?? 0;

            // ── DTO 리스트 구성 ────────────────────────
            var list = new List<ServerGoldGrowthData>();
            foreach (var (ability, stat) in Map)
            {
                int lvl = statDict.TryGetValue(stat, out var v) ? v : 0;
                var (baseV, incV) = GrowthCostTable.GetStatInfo(ability);

                list.Add(new ServerGoldGrowthData
                {
                    AbilityName = ability,
                    BaseValue = baseV,
                    IncrementValue = incV,
                    CurrentLevel = lvl,
                    CostGold = GrowthCostTable.GetCost(lvl)
                });
            }

            return new OkObjectResult(new GoldGrowthResponse
            {
                IsSuccess = true,
                ErrorMessage = "",
                PlayerGold = playerGold,
                GoldGrowthList = list
            });
        }
    }
}
