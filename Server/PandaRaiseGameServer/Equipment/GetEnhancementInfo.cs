using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using CommonLibrary;          // PlayFabConfig.Configure()

namespace Equipment
{
    public class GetEnhancementInfo
    {
        private readonly ILogger<GetEnhancementInfo> _log;
        public GetEnhancementInfo(ILogger<GetEnhancementInfo> log) => _log = log;

        [Function("GetEnhancementInfo")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(body)!;
            string? playFabId = data?.playFabId;
            string? itemInstanceId = data?.itemInstanceId;

            if (string.IsNullOrEmpty(playFabId) || string.IsNullOrEmpty(itemInstanceId))
                return new BadRequestObjectResult("playFabId 또는 itemInstanceId 누락");

            PlayFabConfig.Configure();

            // ── 인벤토리 조회 ─────────────────────────────────────────────
            var inv = await PlayFabServerAPI.GetUserInventoryAsync(
                new GetUserInventoryRequest { PlayFabId = playFabId });
            if (inv.Error != null)
                return new BadRequestObjectResult(inv.Error.GenerateErrorReport());

            var target = inv.Result.Inventory
                                 .FirstOrDefault(i => i.ItemInstanceId == itemInstanceId);
            if (target == null)
                return new BadRequestObjectResult("해당 ItemInstanceId 없음");

            // ── 인스턴스 CustomData → enhancement, level ────────────────
            int enhancement = 0;
            if (target.CustomData?.ContainsKey("enhancement") == true)
                int.TryParse(target.CustomData["enhancement"], out enhancement);

            // ── 카탈로그에서 rank 가져오기 ───────────────────────────────
            var cat = await PlayFabServerAPI.GetCatalogItemsAsync(
                new GetCatalogItemsRequest { CatalogVersion = target.CatalogVersion });
            if (cat.Error != null)
                return new BadRequestObjectResult(cat.Error.GenerateErrorReport());

            string rank = "common";
            var catItem = cat.Result.Catalog.FirstOrDefault(c => c.ItemId == target.ItemId);
            if (catItem == null)
                return new BadRequestObjectResult("카탈로그에 해당 ItemId 없음");

            if (catItem != null && !string.IsNullOrEmpty(catItem.CustomData))
            {
                try
                {
                    var catData = JsonConvert.DeserializeObject<Dictionary<string, object>>(catItem.CustomData);
                    if (catData != null && catData.ContainsKey("rank"))
                    {
                        // 1) "rank" 키가 있는지, 그 값이 null이 아닌지를 검사.
                        // 2) 값이 있다면 object?.ToString(), 없다면 "common" 사용
                        rank = catData.ContainsKey("rank")
                            ? catData["rank"]?.ToString() ?? "common"
                            : "common";
                    }
                }
                catch { }
            }

            // ── 비용 / 확률 / 필요 아이템 수 계산 ────────────────────────
            int goldCost = EnhancementCostTable.GetEnhancementCost(rank, enhancement);
            float successRate = EnhancementCostTable.GetEnhancementSuccessRate(enhancement);
            int requiredCount = EnhancementCostTable.GetRequiredItemCount(rank, enhancement);

            // ── 보유 골드 & 같은 ItemId 개수 ────────────────────────────
            int userGold = inv.Result.VirtualCurrency?.GetValueOrDefault("GC") ?? 0;
            int sameItemCount = inv.Result.Inventory.Count(i =>
                i.ItemId == target.ItemId && i.ItemInstanceId != target.ItemInstanceId);

            return new OkObjectResult(new
            {
                success = true,
                enhancement,
                rank,
                goldCost,
                successRate,
                requiredItemCount = requiredCount,
                userGold,
                sameItemCount
            });
        }
    }
}
