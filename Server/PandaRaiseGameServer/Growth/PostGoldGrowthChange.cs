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
using Growth;          // GrowthCostTable
using CommonLibrary;   // PlayFabConfig.Configure()

namespace Growth
{
    public class PostGoldGrowthChange
    {
        private static readonly Dictionary<string, string> AbilityToStat = new()
        {
            { "공격력", "AtkLevel" },
            { "체력",   "HpLevel" }
        };

        [Function("PostGoldGrowthChange")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            // ── 0. 요청 파싱 ────────────────────────────────────────────
            var dto = JsonConvert.DeserializeObject<GoldGrowthChangeRequest>(
                          await new StreamReader(req.Body).ReadToEndAsync());

            if (dto == null ||
                string.IsNullOrEmpty(dto.PlayFabId) ||
                string.IsNullOrEmpty(dto.AbilityName) ||
                !AbilityToStat.ContainsKey(dto.AbilityName))
                return new BadRequestObjectResult("파라미터 오류");

            string statName = AbilityToStat[dto.AbilityName];
            PlayFabConfig.Configure();

            // ── 1. 통계 & 골드 조회 ────────────────────────────────────
            var statRes = await PlayFabServerAPI.GetPlayerStatisticsAsync(
                new GetPlayerStatisticsRequest { PlayFabId = dto.PlayFabId });
            if (statRes.Error != null)
                return new BadRequestObjectResult(statRes.Error.GenerateErrorReport());

            // 통계를 딕셔너리로 변환
            var statDict = statRes.Result.Statistics
                             .ToDictionary(s => s.StatisticName, s => s.Value);

            int level = statDict.TryGetValue(statName, out var val) ? val : 0;
            if (level >= GrowthCostTable.MaxLevel)
                return new OkObjectResult(new { IsSuccess = false, ErrorMessage = "최대 레벨" });

            int cost = GrowthCostTable.GetCost(level);

            var inv = await PlayFabServerAPI.GetUserInventoryAsync(
                new GetUserInventoryRequest { PlayFabId = dto.PlayFabId });
            if (inv.Error != null)
                return new BadRequestObjectResult(inv.Error.GenerateErrorReport());

            int gold = inv.Result.VirtualCurrency?.GetValueOrDefault("GC") ?? 0;
            if (gold < cost)
                return new OkObjectResult(new { IsSuccess = false, ErrorMessage = "골드 부족" });

            // ── 2. 골드 차감 ───────────────────────────────────────────
            var sub = await PlayFabServerAPI.SubtractUserVirtualCurrencyAsync(
                new SubtractUserVirtualCurrencyRequest
                {
                    PlayFabId = dto.PlayFabId,
                    VirtualCurrency = "GC",
                    Amount = cost
                });
            if (sub.Error != null)
                return new BadRequestObjectResult(sub.Error.GenerateErrorReport());

            // ── 3. 레벨 +1 업데이트 ───────────────────────────────────
            int newLevel = level + 1;
            var updStat = await PlayFabServerAPI.UpdatePlayerStatisticsAsync(
                new UpdatePlayerStatisticsRequest
                {
                    PlayFabId = dto.PlayFabId,
                    Statistics = new List<StatisticUpdate>
                    {
                        new StatisticUpdate { StatisticName = statName, Value = newLevel }
                    }
                });
            if (updStat.Error != null)
                return new BadRequestObjectResult(updStat.Error.GenerateErrorReport());

            // 로컬 딕셔너리도 최신 레벨 반영
            statDict[statName] = newLevel;

            // ── 4. UserData 동기화(선택) ──────────────────────────────
            await PlayFabServerAPI.UpdateUserDataAsync(
                new UpdateUserDataRequest
                {
                    PlayFabId = dto.PlayFabId,
                    Data = new Dictionary<string, string> { { statName, newLevel.ToString() } },
                    Permission = UserDataPermission.Private
                });

            // ── 5. 응답 DTO 구성 ───────────────────────────────────────
            var list = new List<ServerGoldGrowthData>();
            foreach (var (ability, stat) in AbilityToStat)
            {
                int curLv = statDict.TryGetValue(stat, out var v) ? v : 0;
                var (baseV, incV) = GrowthCostTable.GetStatInfo(ability);

                list.Add(new ServerGoldGrowthData
                {
                    AbilityName = ability,
                    BaseValue = baseV,
                    IncrementValue = incV,
                    CurrentLevel = curLv,
                    CostGold = GrowthCostTable.GetCost(curLv)
                });
            }

            return new OkObjectResult(new GoldGrowthResponse
            {
                IsSuccess = true,
                ErrorMessage = "",
                PlayerGold = gold - cost,
                GoldGrowthList = list
            });
        }
    }
}
