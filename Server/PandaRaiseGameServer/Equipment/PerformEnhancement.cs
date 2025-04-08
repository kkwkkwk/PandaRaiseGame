using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using CommonLibrary;
using Equipment;

namespace Enhancement
{
    public class PerformEnhancement
    {
        private readonly ILogger<PerformEnhancement> _logger;

        public PerformEnhancement(ILogger<PerformEnhancement> logger)
        {
            _logger = logger;
        }

        [Function("PerformEnhancement")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("PerformEnhancement 함수가 호출되었습니다.");

            // 1) 요청 바디 파싱 : { "playFabId": "...", "itemInstanceId": "..." }
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody)!;

            string? playFabId = data?.playFabId;
            string? itemInstanceId = data?.itemInstanceId;

            if (string.IsNullOrEmpty(playFabId) || string.IsNullOrEmpty(itemInstanceId))
            {
                return new BadRequestObjectResult("playFabId 또는 itemInstanceId가 누락되었습니다.");
            }

            // 2) PlayFab 설정
            PlayFabConfig.Configure();

            // 3) 유저 인벤토리 조회 -> 강화 대상 아이템 찾기
            var invReq = new GetUserInventoryRequest { PlayFabId = playFabId };
            var invRes = await PlayFabServerAPI.GetUserInventoryAsync(invReq);
            if (invRes.Error != null)
            {
                return new BadRequestObjectResult("GetUserInventory 오류: " + invRes.Error.GenerateErrorReport());
            }

            var inventory = invRes.Result.Inventory;
            var targetItem = inventory.FirstOrDefault(i => i.ItemInstanceId == itemInstanceId);
            if (targetItem == null)
            {
                return new BadRequestObjectResult("해당 ItemInstanceId 아이템을 찾을 수 없습니다.");
            }

            // 4) 현재 enhancement, level, rank 가져오기
            //    rank는 Catalog 조회를 통해 파악 (CustomData)
            int enhancement = 0;
            int level = 1;
            if (targetItem.CustomData != null)
            {
                if (targetItem.CustomData.ContainsKey("enhancement"))
                {
                    int.TryParse(targetItem.CustomData["enhancement"], out enhancement);
                }
                if (targetItem.CustomData.ContainsKey("level"))
                {
                    int.TryParse(targetItem.CustomData["level"], out level);
                }
            }

            // 카탈로그 조회 -> rank 구하기
            var catReq = new GetCatalogItemsRequest
            {
                CatalogVersion = targetItem.CatalogVersion
            };
            var catRes = await PlayFabServerAPI.GetCatalogItemsAsync(catReq);
            if (catRes.Error != null)
            {
                return new BadRequestObjectResult("GetCatalogItems 오류: " + catRes.Error.GenerateErrorReport());
            }

            string rank = "common";
            var catItem = catRes.Result.Catalog.FirstOrDefault(c => c.ItemId == targetItem.ItemId);
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

            // 10강 이상이면 불가
            if (enhancement >= 10)
            {
                return new OkObjectResult(new
                {
                    success = false,
                    message = "이미 최대 강화(10강)입니다.",
                    currentEnhancement = enhancement
                });
            }

            // 5) 강화 비용 및 성공 확률, 재료 개수 계산
            int goldCost = EnhancementCostTable.GetEnhancementCost(rank, enhancement);
            float successRate = EnhancementCostTable.GetEnhancementSuccessRate(enhancement);
            int itemCost = EnhancementCostTable.GetRequiredItemCount(rank, enhancement);
            // 같은 ItemId를 가진 "추가" 아이템 N개 필요

            if (goldCost < 0)
            {
                return new OkObjectResult(new
                {
                    success = false,
                    message = "강화를 진행할 수 없습니다. (비용 계산 오류)",
                    currentEnhancement = enhancement
                });
            }

            // 6) 골드 보유량 확인
            var userVC = invRes.Result.VirtualCurrency;
            int userGold = 0;
            if (userVC != null && userVC.ContainsKey("GC"))
                userGold = userVC["GC"];

            if (userGold < goldCost)
            {
                return new OkObjectResult(new
                {
                    success = false,
                    message = $"골드가 부족합니다. 필요: {goldCost}, 보유: {userGold}",
                    currentEnhancement = enhancement
                });
            }

            // 7) 같은 ItemId 재료 수량 확인  (Stack 방식)
            var sameIdItems = inventory
                .Where(i => i.ItemId == targetItem.ItemId && i.ItemInstanceId != targetItem.ItemInstanceId)
                .ToList();

            int stackUses = targetItem.RemainingUses ?? 1; // 총 수량
            int materialCount = stackUses - 1;   // 장비 1개를 남겨둔 중복 수량
            
            if (materialCount < itemCost)
            {
                return new OkObjectResult(new
                {
                    success = false,
                    message = $"같은 아이템이 부족합니다. 필요: {itemCost}, 보유: {materialCount}",
                    currentEnhancement = enhancement
                });
            }

            // 8) 골드 차감
            var subVCReq = new SubtractUserVirtualCurrencyRequest
            {
                PlayFabId = playFabId,
                VirtualCurrency = "GC",
                Amount = goldCost
            };
            var subVCRes = await PlayFabServerAPI.SubtractUserVirtualCurrencyAsync(subVCReq);
            if (subVCRes.Error != null)
            {
                return new BadRequestObjectResult("골드 차감 실패: " + subVCRes.Error.GenerateErrorReport());
            }

            // 9) 같은 ItemId 아이템 N개 소모(삭제) - (RevokeInventoryItems)
            //    N개(=itemCost)만큼 RevokeInventoryItem 배열을 만들어 보냄
            var modifyReq = new ModifyItemUsesRequest
            {
                PlayFabId = playFabId,
                ItemInstanceId = targetItem.ItemInstanceId,
                UsesToAdd = -itemCost          // 음수 → 차감
            };
            var modifyRes = await PlayFabServerAPI.ModifyItemUsesAsync(modifyReq);
            if (modifyRes.Error != null)
                return new BadRequestObjectResult("재료 차감 실패: " + modifyRes.Error.GenerateErrorReport());

            // 10) 강화 성공 여부 판정
            bool isSuccess = false;
            Random rand = new Random();
            double roll = rand.NextDouble(); // 0.0 ~ 1.0
            if (roll < successRate)
            {
                isSuccess = true;
                enhancement++; // +1
            }

            // 11) 성공 시 Item Instance CustomData 갱신
            if (isSuccess)
            {
                var updateReq = new UpdateUserInventoryItemDataRequest
                {
                    PlayFabId = playFabId,
                    ItemInstanceId = itemInstanceId,
                    Data = new Dictionary<string, string>
                    {
                        { "enhancement", enhancement.ToString() },
                        { "level", level.ToString() } // 레벨도 필요 시 갱신
                    }
                };
                var updateRes = await PlayFabServerAPI.UpdateUserInventoryItemCustomDataAsync(updateReq);
                if (updateRes.Error != null)
                {
                    return new BadRequestObjectResult("강화 후 CustomData 업데이트 실패: " + updateRes.Error.GenerateErrorReport());
                }
            }

            // 12) 결과 반환
            return new OkObjectResult(new
            {
                success = isSuccess,
                newEnhancement = enhancement,
                goldUsed = goldCost,
                itemUsedCount = itemCost,
                successRate = successRate,
                message = isSuccess ? "강화 성공!" : "강화 실패!"
            });
        }
    }
}
