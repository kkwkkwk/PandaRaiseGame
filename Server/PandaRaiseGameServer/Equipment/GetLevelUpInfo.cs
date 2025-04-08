using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using CommonLibrary;

namespace Equipment
{
    public class GetLevelUpInfo
    {
        private readonly ILogger<GetLevelUpInfo> _log;
        public GetLevelUpInfo(ILogger<GetLevelUpInfo> log) => _log = log;

        private const int MaxLevel = 1000;

        [Function("GetLevelUpInfo")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic d = JsonConvert.DeserializeObject(body)!;
            string? playFabId = d?.playFabId;
            string? itemInstanceId = d?.itemInstanceId;
            if (string.IsNullOrEmpty(playFabId) || string.IsNullOrEmpty(itemInstanceId))
                return new BadRequestObjectResult("playFabId 또는 itemInstanceId 누락");

            PlayFabConfig.Configure();

            // ── 인벤토리 조회 ─────────────────────────────────────────────
            var inv = await PlayFabServerAPI.GetUserInventoryAsync(
                new GetUserInventoryRequest { PlayFabId = playFabId });
            if (inv.Error != null)
                return new BadRequestObjectResult(inv.Error.GenerateErrorReport());

            var target = inv.Result.Inventory.FirstOrDefault(i => i.ItemInstanceId == itemInstanceId);
            if (target == null)
                return new BadRequestObjectResult("해당 ItemInstanceId 없음");


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

            // ── 인벤토리에서 레벨 가져오기 ───────────────────────────────
            int level = 1;
            if (target.CustomData?.ContainsKey("level") == true)
                int.TryParse(target.CustomData["level"], out level);

            if (level >= MaxLevel)
                return new OkObjectResult(new
                {
                    success = false,
                    message = "이미 만렙입니다.",
                    currentLevel = level
                });

            // ── 비용 / 확률 / 필요 아이템 수 계산 ────────────────────────
            int needStone = LevelUpCostTable.GetRequiredStones(rank, level);
            int userStone = inv.Result.VirtualCurrency?.GetValueOrDefault("RS") ?? 0;

            return new OkObjectResult(new
            {
                success = true,
                currentLevel = level,
                requiredStones = needStone,
                userStoneCount = userStone
            });
        }
    }
}
