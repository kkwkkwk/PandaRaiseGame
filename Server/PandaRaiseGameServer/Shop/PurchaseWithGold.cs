using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using System.IO;
using System.Threading.Tasks;

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

            // HTTP 요청 본문에서 JSON 데이터 읽기 (예: { "playFabId": "플레이어_ID", "itemId": "아이템_ID" })
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody)!;
            string playFabId = data?.playFabId!;
            string itemId = data?.itemId!;

            // 유효성 검사
            if (string.IsNullOrEmpty(playFabId) || string.IsNullOrEmpty(itemId))
            {
                _logger.LogWarning("올바르지 않은 요청: playFabId 또는 itemId 누락");
                return new BadRequestObjectResult("올바른 playFabId와 itemId를 제공해야 합니다.");
            }

            // PlayFab 설정 적용
            PlayFabSettings.staticSettings.TitleId = "DBDFA";
            PlayFabSettings.staticSettings.DeveloperSecretKey = "USMNHI4U93WRBCXU8NY554IA898DMOSIYYDW96SN79I5IIOX8E";

            _logger.LogInformation($"플레이어 {playFabId}의 아이템 {itemId} 구매 시도 중...");

            // PlayFab에서 아이템 가격 조회
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

            var item = catalogResult.Result.Catalog.Find(i => i.ItemId == "itemtest001"); // 임시 아이템 아이디 itemtest001
            if (item == null || !item.VirtualCurrencyPrices.ContainsKey("GC"))
            {
                return new BadRequestObjectResult("잘못된 아이템 ID 또는 가격 정보 없음.");
            }

            int itemCost = (int)item.VirtualCurrencyPrices["GC"];

            _logger.LogInformation($"플레이어 {playFabId}의 골드 차감 시도 중... (차감할 금액: {itemCost})");

            // 가상 화폐 차감 요청 생성
            var subtractCurrencyRequest = new SubtractUserVirtualCurrencyRequest
            {
                PlayFabId = playFabId,
                VirtualCurrency = "GC", // 게임 내 가상 화폐 코드
                Amount = itemCost
            };

            // PlayFab API 호출
            var subtractCurrencyResult = await PlayFabServerAPI.SubtractUserVirtualCurrencyAsync(subtractCurrencyRequest);

            // 결과 처리
            if (subtractCurrencyResult.Error != null)
            {
                _logger.LogError($"골드 차감 실패: {subtractCurrencyResult.Error.GenerateErrorReport()}");
                return new ObjectResult("골드 차감 중 오류 발생: " + subtractCurrencyResult.Error.GenerateErrorReport())
                {
                    StatusCode = 500
                };
            }
            else
            {
                _logger.LogInformation($"플레이어 {playFabId}의 골드 {itemCost} 차감 완료 (새 잔액: {subtractCurrencyResult.Result.Balance}).");
                return new OkObjectResult($"플레이어 {playFabId}의 골드 {itemCost} 차감 완료. (새 잔액: {subtractCurrencyResult.Result.Balance})");
            }
        }
    }
}