using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using CommonLibrary; // <- 여기서 PlayFabConfig 접근
using PlayFab;
using PlayFab.ServerModels;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Shop
{
    public class PurchaseWithGold
    {
        private readonly ILogger<PurchaseWithGold> _logger;

        public PurchaseWithGold(ILogger<PurchaseWithGold> logger)
        {
            _logger = logger;
        }

        [Function("PurchaseWithGold")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("PurchaseWithGold 함수가 호출되었습니다.");

            // 1. 요청 본문 파싱
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody)!;
            string playFabId = data?.playFabId!;
            string itemId = data?.itemId!;

            // 2. 유효성 검사
            if (string.IsNullOrEmpty(playFabId) || string.IsNullOrEmpty(itemId))
            {
                _logger.LogWarning("올바르지 않은 요청: playFabId 또는 itemId 누락");
                return new BadRequestObjectResult("올바른 playFabId와 itemId를 제공해야 합니다.");
            }

            // 3. PlayFab 설정
            // 공통 설정 적용
            PlayFabConfig.Configure();

            _logger.LogInformation($"플레이어 {playFabId}의 아이템 {itemId} 구매 시도 중...");

            // 4. 카탈로그에서 아이템 가격 조회
            var catalogRequest = new GetCatalogItemsRequest();
            var catalogResult = await PlayFabServerAPI.GetCatalogItemsAsync(catalogRequest);

            if (catalogResult.Error != null)
            {
                _logger.LogError("아이템 가격 조회 실패: " + catalogResult.Error.GenerateErrorReport());
                return new ObjectResult("아이템 가격 조회 중 오류 발생")
                {
                    StatusCode = 500
                };
            }

            var catalogItem = catalogResult.Result.Catalog.Find(i => i.ItemId == "itemtest_001");
            if (catalogItem == null || !catalogItem.VirtualCurrencyPrices.ContainsKey("GC"))
            {
                return new BadRequestObjectResult("잘못된 아이템 ID 또는 가격 정보 없음.");
            }

            int itemCost = (int)catalogItem.VirtualCurrencyPrices["GC"];
            _logger.LogInformation($"구매하려는 아이템 가격: {itemCost}골드");

            // 5. 사용자 현재 골드 조회
            var getUserInventoryRequest = new GetUserInventoryRequest
            {
                PlayFabId = playFabId
            };
            var userInventoryResult = await PlayFabServerAPI.GetUserInventoryAsync(getUserInventoryRequest);

            if (userInventoryResult.Error != null)
            {
                _logger.LogError("인벤토리 조회 실패: " + userInventoryResult.Error.GenerateErrorReport());
                return new ObjectResult("인벤토리 조회 중 오류 발생: " + userInventoryResult.Error.GenerateErrorReport())
                {
                    StatusCode = 500
                };
            }

            // 사용자 골드 가져오기 (GC)
            userInventoryResult.Result.VirtualCurrency.TryGetValue("GC", out int userBalance);

            // 6. 골드 부족 시
            if (userBalance < itemCost)
            {
                _logger.LogWarning($"플레이어 {playFabId}의 잔액 부족. (잔액: {userBalance}, 가격: {itemCost})");
                return new BadRequestObjectResult("골드가 부족합니다. 구매할 수 없습니다.");
            }

            // 7. 골드 차감
            _logger.LogInformation($"플레이어 {playFabId}의 골드 차감 시도 중... (차감할 금액: {itemCost})");
            var subtractCurrencyRequest = new SubtractUserVirtualCurrencyRequest
            {
                PlayFabId = playFabId,
                VirtualCurrency = "GC",
                Amount = itemCost
            };
            var subtractCurrencyResult = await PlayFabServerAPI.SubtractUserVirtualCurrencyAsync(subtractCurrencyRequest);

            if (subtractCurrencyResult.Error != null)
            {
                _logger.LogError($"골드 차감 실패: {subtractCurrencyResult.Error.GenerateErrorReport()}");
                return new ObjectResult("골드 차감 중 오류 발생: " + subtractCurrencyResult.Error.GenerateErrorReport())
                {
                    StatusCode = 500
                };
            }

            _logger.LogInformation($"골드 {itemCost} 차감 완료! (새 잔액: {subtractCurrencyResult.Result.Balance})");

            // 8. 아이템 지급
            _logger.LogInformation("아이템 지급 시작...");
            var grantRequest = new GrantItemsToUserRequest
            {
                PlayFabId = playFabId,
                CatalogVersion = "GoldShop",        // 카탈로그 명
                ItemIds = new List<string> { itemId }
            };
            var grantResult = await PlayFabServerAPI.GrantItemsToUserAsync(grantRequest);

            if (grantResult.Error != null)
            {
                _logger.LogError($"아이템 지급 실패: {grantResult.Error.GenerateErrorReport()}");
                return new ObjectResult("아이템 지급 중 오류 발생: " + grantResult.Error.GenerateErrorReport())
                {
                    StatusCode = 500
                };
            }

            _logger.LogInformation($"플레이어 {playFabId}에게 아이템 {itemId} 지급 완료!");

            // 9. 성공 결과 반환
            return new OkObjectResult(
                $"플레이어 {playFabId} 골드 {itemCost} 차감 및 아이템 {itemId} 지급 완료! (새 잔액: {subtractCurrencyResult.Result.Balance})"
            );
        }
    }
}
