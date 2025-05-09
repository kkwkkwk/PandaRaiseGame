using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using CommonLibrary;

namespace Shop
{
    public class BuyGachaItem
    {
        private readonly ILogger<BuyGachaItem> _logger;
        private readonly Random _rng = new();

        public BuyGachaItem(ILogger<BuyGachaItem> logger) => _logger = logger;

        [Function("BuyGachaItem")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "BuyGachaItem")] HttpRequest req)
        {
            _logger.LogInformation("[BuyGachaItem] 시작");

            // 1) PlayFab 설정
            try { PlayFabConfig.Configure(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BuyGachaItem] PlayFabConfig.Configure 실패");
                return new BadRequestObjectResult("서버 설정 오류");
            }

            // 2) 요청 파싱
            string body = await new StreamReader(req.Body).ReadToEndAsync();
            BuyGachaRequestData data;
            try
            {
                data = JsonConvert.DeserializeObject<BuyGachaRequestData>(body)
                       ?? throw new Exception("RequestData null");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BuyGachaItem] 요청 JSON 파싱 실패");
                return new BadRequestObjectResult("잘못된 요청 형식");
            }

            // 3) 뽑기 횟수 파싱 (ItemName에서 'N개' 패턴)
            string? itemName = data.WeaponItemData?.ItemName
                               ?? data.ArmorItemData?.ItemName
                               ?? data.SkillItemData?.ItemName;
            int count = 1;
            if (!string.IsNullOrEmpty(itemName))
            {
                // 첫 번째 연속된 숫자 시퀀스를 찾아 파싱
                var match = Regex.Match(itemName, @"\d+");
                if (match.Success && int.TryParse(match.Value, out var n))
                    count = n;
            }

            _logger.LogInformation("[BuyGachaItem] ItemName='{0}', Count={1}", itemName, count);

            // 4) 뽑기 가격 파악
            int pricePerDraw = data.WeaponItemData?.Price
                             ?? data.ArmorItemData?.Price
                             ?? data.SkillItemData?.Price
                             ?? 0;

            // 5) 카탈로그 읽어와 등급별 풀 구성
            var catRes = await PlayFabServerAPI.GetCatalogItemsAsync(new GetCatalogItemsRequest
            {
                CatalogVersion = data.ItemType
            });
            if (catRes.Error != null)
            {
                _logger.LogError(catRes.Error.GenerateErrorReport(), "[BuyGachaItem] Catalog 조회 오류");
                return new OkObjectResult(new BuyGachaResponseData { IsSuccess = false });
            }
            var catalog = catRes.Result.Catalog;
            var poolByRank = new Dictionary<string, List<CatalogItem>>();
            foreach (var ci in catalog)
            {
                if (ci.CustomData == null) continue;
                try
                {
                    var cd = JsonConvert.DeserializeObject<Dictionary<string, string>>(ci.CustomData);
                    if (!cd!.TryGetValue("rank", out var rank)) continue;
                    if (!poolByRank.ContainsKey(rank))
                        poolByRank[rank] = new List<CatalogItem>();
                    poolByRank[rank].Add(ci);
                }
                catch { }
            }

            // 6) 뽑기 실행
            var drawn = new List<CatalogItem>();
            int totalWeight = GachaRankWeight.RankWeights.Values.Sum();
            for (int i = 0; i < count; i++)
            {
                int roll = _rng.Next(1, totalWeight + 1);
                int acc = 0;
                string? chosenRank = null;
                foreach (var kv in GachaRankWeight.RankWeights)
                {
                    acc += kv.Value;
                    if (roll <= acc)
                    {
                        chosenRank = kv.Key;
                        break;
                    }
                }
                _logger.LogInformation("[BuyGachaItem] 뽑기#{0} 등급={1}", i + 1, chosenRank);

                if (chosenRank != null && poolByRank.TryGetValue(chosenRank, out var pool) && pool.Count > 0)
                {
                    drawn.Add(pool[_rng.Next(pool.Count)]);
                    _logger.LogInformation("[BuyGachaItem] 뽑기#{0} 아이템={1}", i + 1, drawn.Last().ItemId);
                }
                else
                {
                    _logger.LogWarning("[BuyGachaItem] 등급 풀 비었거나 등급없음: {0}", chosenRank);
                }
            }
            if (drawn.Count == 0)
            {
                _logger.LogError("[BuyGachaItem] 뽑힌 아이템 없음");
                return new OkObjectResult(new BuyGachaResponseData { IsSuccess = false });
            }

            // 7) 재화 차감
            if (!data.CurrencyType!.Equals("FREE", StringComparison.OrdinalIgnoreCase) && pricePerDraw > 0)
            {
                // 7-1) 현재 잔액 조회
                var invRes = await PlayFabServerAPI.GetUserInventoryAsync(new GetUserInventoryRequest
                {
                    PlayFabId = data.PlayFabId
                });
                if (invRes.Error != null)
                {
                    _logger.LogError(invRes.Error.GenerateErrorReport(), "[BuyGachaItem] 잔액 조회 실패");
                    return new OkObjectResult(new BuyGachaResponseData { IsSuccess = false });
                }

                invRes.Result.VirtualCurrency.TryGetValue(data.CurrencyType, out int currentBalance);

                // 7-2) 잔액 부족 체크
                if (currentBalance < pricePerDraw)
                {
                    _logger.LogWarning("[BuyGachaItem] 잔액 부족: 현재 {0}, 필요 {1}", currentBalance, pricePerDraw);
                    return new OkObjectResult(new BuyGachaResponseData { IsSuccess = false });
                }

                // 7-3) 차감 진행
                var subRes = await PlayFabServerAPI.SubtractUserVirtualCurrencyAsync(new SubtractUserVirtualCurrencyRequest
                {
                    PlayFabId = data.PlayFabId,
                    VirtualCurrency = data.CurrencyType,
                    Amount = pricePerDraw
                });
                if (subRes.Error != null)
                {
                    _logger.LogError(subRes.Error.GenerateErrorReport(), "[BuyGachaItem] 재화 차감 실패");
                    return new OkObjectResult(new BuyGachaResponseData { IsSuccess = false });
                }
                _logger.LogInformation("[BuyGachaItem] {0} {1} 차감 성공", data.CurrencyType, pricePerDraw);
            }


            // 8) 아이템 지급
            var invRes2 = await PlayFabServerAPI.GetUserInventoryAsync(new GetUserInventoryRequest
            {
                PlayFabId = data.PlayFabId
            });
            if (invRes2.Error != null)
            {
                _logger.LogError(invRes2.Error.GenerateErrorReport(), "[BuyGachaItem] GetUserInventory 실패");
                return new OkObjectResult(new BuyGachaResponseData { IsSuccess = false });
            }
            var inventory = invRes2.Result.Inventory;

            // drawn 리스트를 ItemId별로 그룹핑
            var groups = drawn
                .GroupBy(ci => ci.ItemId)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var kv in groups)
            {
                string itemId = kv.Key;
                int quantity = kv.Value;

                // --- 기존 인벤토리에 스택 인스턴스가 있는지 확인 ---
                var existing = inventory.FirstOrDefault(i => i.ItemId == itemId);

                if (existing == null)
                {
                    // 8a) 신규 인스턴스 생성 (Grant 한 번)
                    _logger.LogInformation("[BuyGachaItem] Grant 신규 인스턴스: {0}", itemId);
                    var grantRes = await PlayFabServerAPI.GrantItemsToUserAsync(new GrantItemsToUserRequest
                    {
                        CatalogVersion = data.ItemType,
                        PlayFabId = data.PlayFabId,
                        ItemIds = new List<string> { itemId }
                    });
                    if (grantRes.Error != null)
                    {
                        _logger.LogError(grantRes.Error.GenerateErrorReport(),
                            "[BuyGachaItem] GrantItemsToUser 실패: {0}", itemId);
                        return new OkObjectResult(new BuyGachaResponseData { IsSuccess = false });
                    }

                    // 8b) InstanceId 확보: 먼저 Grant 결과에서 시도
                    string? instanceId = grantRes.Result.ItemGrantResults?
                        .Select(r => r.ItemInstanceId)
                        .FirstOrDefault(id => !string.IsNullOrEmpty(id));

                    if (string.IsNullOrEmpty(instanceId))
                    {
                        _logger.LogInformation("[BuyGachaItem] Grant 결과에 InstanceId 누락, 인벤토리 재조회");
                        // 인벤토리 재조회
                        var invAfter = await PlayFabServerAPI.GetUserInventoryAsync(new GetUserInventoryRequest
                        {
                            PlayFabId = data.PlayFabId
                        });
                        if (invAfter.Error != null)
                        {
                            _logger.LogError(invAfter.Error.GenerateErrorReport(),
                                "[BuyGachaItem] Grant 후 인벤토리 조회 실패");
                            return new OkObjectResult(new BuyGachaResponseData { IsSuccess = false });
                        }
                        var found = invAfter.Result.Inventory
                            .FirstOrDefault(i => i.ItemId == itemId);
                        if (found == null || string.IsNullOrEmpty(found.ItemInstanceId))
                        {
                            _logger.LogError("[BuyGachaItem] Grant 후에도 인벤토리 인스턴스를 찾을 수 없음: {0}", itemId);
                            return new OkObjectResult(new BuyGachaResponseData { IsSuccess = false });
                        }
                        instanceId = found.ItemInstanceId;
                        _logger.LogInformation("[BuyGachaItem] 재조회로 확보된 InstanceId: {0}", instanceId);
                    }

                    // 8c) 신규 인스턴스에는 quantity만큼 Uses 추가
                    //    (첫 그랜트로 기본 1개가 들어갔으므로, toAddNew = quantity - 1)
                    int toAddNew = quantity - 1;
                    if (toAddNew > 0)
                    {
                        _logger.LogInformation("[BuyGachaItem] ModifyItemUses 신규 인스턴스 {0}에 +{1} Uses",
                            itemId, toAddNew);
                        var modNew = await PlayFabServerAPI.ModifyItemUsesAsync(new ModifyItemUsesRequest
                        {
                            PlayFabId = data.PlayFabId,
                            ItemInstanceId = instanceId,
                            UsesToAdd = toAddNew
                        });
                        if (modNew.Error != null)
                        {
                            _logger.LogError(modNew.Error.GenerateErrorReport(),
                                "[BuyGachaItem] ModifyItemUses 실패 (신규): {0}", itemId);
                            return new OkObjectResult(new BuyGachaResponseData { IsSuccess = false });
                        }
                        _logger.LogInformation("[BuyGachaItem] 신규 RemainingUses={0}", modNew.Result.RemainingUses);
                    }
                }

                else
                {
                    // 8d) 기존 인스턴스가 있으면 quantity 만큼 Uses 추가
                    _logger.LogInformation("[BuyGachaItem] ModifyItemUses 기존 인스턴스 {0}에 +{1} Uses",
                        itemId, quantity);
                    var modExist = await PlayFabServerAPI.ModifyItemUsesAsync(new ModifyItemUsesRequest
                    {
                        PlayFabId = data.PlayFabId,
                        ItemInstanceId = existing.ItemInstanceId,
                        UsesToAdd = quantity
                    });
                    if (modExist.Error != null)
                    {
                        _logger.LogError(modExist.Error.GenerateErrorReport(),
                            "[BuyGachaItem] ModifyItemUses 실패 (기존): {0}", itemId);
                        return new OkObjectResult(new BuyGachaResponseData { IsSuccess = false });
                    }
                    _logger.LogInformation("[BuyGachaItem] 기존 RemainingUses={0}", modExist.Result.RemainingUses);
                }
            }

            // 9) 응답 구성
            var owned = drawn.Select(ci => new OwnedItemData
            {
                ItemType = data.ItemType,
                ArmorItemData = data.ItemType == "Armor"
                    ? JsonConvert.DeserializeObject<ArmorGachaItemData>(ci.CustomData) : null,
                WeaponItemData = data.ItemType == "Weapon"
                    ? JsonConvert.DeserializeObject<WeaponGachaItemData>(ci.CustomData) : null,
                SkillItemData = data.ItemType == "Skill"
                    ? JsonConvert.DeserializeObject<SkillGachaItemData>(ci.CustomData) : null
            }).ToList();

            return new OkObjectResult(new BuyGachaResponseData
            {
                IsSuccess = true,
                OwnedItemList = owned
            });
        }
    }
}
