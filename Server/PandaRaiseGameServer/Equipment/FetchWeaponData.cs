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

namespace Weapons
{
    public class FetchWeaponData
    {
        private readonly ILogger<FetchWeaponData> _logger;

        public FetchWeaponData(ILogger<FetchWeaponData> logger)
        {
            _logger = logger;
        }

        [Function("FetchWeaponData")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("FetchWeaponData 함수가 호출되었습니다.");

            // 1) 요청 JSON 파싱 ( { "playFabId": "..." } )
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation($"요청 바디: {requestBody}");

            dynamic data = JsonConvert.DeserializeObject(requestBody)!;
            string playFabId = data?.playFabId!;

            if (string.IsNullOrEmpty(playFabId))
            {
                return new BadRequestObjectResult("PlayFabId가 누락되었습니다.");
            }

            // 2) PlayFab 설정
            PlayFabConfig.Configure();

            // 3) 카탈로그(Weapon) 조회 -> rank(등급) 정보
            var getCatalogReq = new GetCatalogItemsRequest
            {
                CatalogVersion = "Weapon"
            };
            var getCatalogRes = await PlayFabServerAPI.GetCatalogItemsAsync(getCatalogReq);
            if (getCatalogRes.Error != null)
            {
                return new BadRequestObjectResult("GetCatalogItems 오류: " + getCatalogRes.Error.GenerateErrorReport());
            }

            // CatalogItem 목록
            var catalogItems = getCatalogRes.Result.Catalog;
            // itemId => (rank, displayName) 저장 (레벨은 이제 카탈로그에서 안 가져옴)
            var catalogDataDict = new Dictionary<string, (string rank, string displayName)>();

            foreach (var catItem in catalogItems)
            {
                // rank를 Catalog CustomData에서 가져온다고 가정
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
                        // 파싱 오류는 무시하거나 로깅
                    }
                }
                catalogDataDict[catItem.ItemId] = (rank, catDisplayName);
            }

            // 4) 유저 인벤토리 조회 (CatalogVersion == "Weapon")
            var getInvReq = new GetUserInventoryRequest
            {
                PlayFabId = playFabId
            };
            var getInvRes = await PlayFabServerAPI.GetUserInventoryAsync(getInvReq);
            if (getInvRes.Error != null)
            {
                return new BadRequestObjectResult("GetUserInventory 오류: " + getInvRes.Error.GenerateErrorReport());
            }

            var allItems = getInvRes.Result.Inventory;
            var weaponItems = allItems.Where(i => i.CatalogVersion == "Weapon").ToList();

            // 5) 무기 데이터 리스트 구성
            var weaponDataList = new List<WeaponData>();
            foreach (var item in weaponItems)
            {
                // 카탈로그에서 rank, displayName 가져오기
                if (!catalogDataDict.TryGetValue(item.ItemId, out var catInfo))
                    continue; // 존재하지 않으면 스킵

                // 인스턴스 CustomData에서 level, enhancement 추출
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

                var weaponData = new WeaponData
                {
                    weaponName = catInfo.displayName, // 카탈로그 DisplayName
                    rank = catInfo.rank,              // 카탈로그 등급
                    level = level,                    // 아이템 인스턴스의 레벨
                    enhancement = enhancement         // 아이템 인스턴스의 강화 수치
                };
                weaponDataList.Add(weaponData);
            }

            return new OkObjectResult(weaponDataList);
        }
    }

    /// <summary>
    /// 무기 아이템 DTO
    /// </summary>
    public class WeaponData
    {
        public string? weaponName { get; set; }
        public string? rank { get; set; }
        public int level { get; set; }
        public int enhancement { get; set; }
    }
}
