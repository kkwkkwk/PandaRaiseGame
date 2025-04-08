using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using CommonLibrary;

namespace Armor
{
    public class FetchArmorData
    {
        private readonly ILogger<FetchArmorData> _logger;

        public FetchArmorData(ILogger<FetchArmorData> logger)
        {
            _logger = logger;
        }

        [Function("FetchArmorData")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("FetchArmorData 함수가 호출되었습니다.");

            // 1) 요청 JSON 파싱
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody)!;
            string playFabId = data?.playFabId!;

            if (string.IsNullOrEmpty(playFabId))
            {
                return new BadRequestObjectResult("PlayFabId가 누락되었습니다.");
            }

            // 2) PlayFab 설정
            PlayFabConfig.Configure();

            // 3) Armor 카탈로그 조회 -> rank, displayName 파싱
            var getCatalogReq = new GetCatalogItemsRequest
            {
                CatalogVersion = "Armor"
            };
            var getCatalogRes = await PlayFabServerAPI.GetCatalogItemsAsync(getCatalogReq);
            if (getCatalogRes.Error != null)
            {
                return new BadRequestObjectResult("GetCatalogItems(Armor) 오류: " + getCatalogRes.Error.GenerateErrorReport());
            }

            var catalogItems = getCatalogRes.Result.Catalog;
            var catalogDataDict = new Dictionary<string, (string rank, string displayName)>();

            foreach (var catItem in catalogItems)
            {
                string rank = "common";
                string catDisplayName = catItem.DisplayName ?? catItem.ItemId;

                if (!string.IsNullOrEmpty(catItem.CustomData))
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
                    catch
                    {
                        // ignore
                    }
                }
                catalogDataDict[catItem.ItemId] = (rank, catDisplayName);
            }

            // 4) 유저 인벤토리에서 Armor 카탈로그 아이템만 조회
            var getInvReq = new GetUserInventoryRequest { PlayFabId = playFabId };
            var getInvRes = await PlayFabServerAPI.GetUserInventoryAsync(getInvReq);
            if (getInvRes.Error != null)
            {
                return new BadRequestObjectResult("GetUserInventory 오류: " + getInvRes.Error.GenerateErrorReport());
            }

            var allItems = getInvRes.Result.Inventory;
            var armorItems = allItems.Where(i => i.CatalogVersion == "Armor").ToList();

            // 5) ArmorData 리스트 생성
            var armorDataList = new List<ArmorData>();
            foreach (var item in armorItems)
            {
                if (!catalogDataDict.TryGetValue(item.ItemId, out var catInfo))
                    continue;

                int level = 1;
                int enhancement = 0;

                if (item.CustomData != null)
                {
                    if (item.CustomData.ContainsKey("level"))
                    {
                        int.TryParse(item.CustomData["level"], out level);
                    }
                    if (item.CustomData.ContainsKey("enhancement"))
                    {
                        int.TryParse(item.CustomData["enhancement"], out enhancement);
                    }
                }

                var armorData = new ArmorData
                {
                    armorName = catInfo.displayName,
                    rank = catInfo.rank,
                    level = level,
                    enhancement = enhancement
                };
                armorDataList.Add(armorData);
            }

            return new OkObjectResult(armorDataList);
        }
    }

    /// <summary>
    /// 방어구 아이템 DTO
    /// </summary>
    public class ArmorData
    {
        public string? armorName { get; set; }
        public string? rank { get; set; }
        public int level { get; set; }
        public int enhancement { get; set; }
    }
}
