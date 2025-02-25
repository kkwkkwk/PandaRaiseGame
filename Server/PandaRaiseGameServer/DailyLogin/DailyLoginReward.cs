using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker; // Azure Functions의 고립(worker) 모델의 어트리뷰트
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using CommonLibrary;

namespace DailyLogin
{
    public class DailyLoginReward
    {
        private readonly ILogger<DailyLoginReward> _logger;

        public DailyLoginReward(ILogger<DailyLoginReward> logger)
        {
            _logger = logger;
        }

        [Function("DailyLoginReward")]
        public async Task<IActionResult> Run(
            // HTTP Trigger를 통해 함수를 호출하고, POST 방식으로만 허용
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("DailyLoginReward 함수가 호출되었습니다.");

            // HTTP 요청 본문에서 JSON 데이터 읽기 (예: { "playFabId": "플레이어_ID" })
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody)!;
            string playFabId = data?.playFabId!;

            // playFabId가 없으면 BadRequest(400) 반환
            if (string.IsNullOrEmpty(playFabId))
            {
                _logger.LogWarning("요청 본문에 playFabId가 포함되어 있지 않습니다.");
                return new BadRequestObjectResult("플레이어의 PlayFabId를 요청 본문에 포함시켜 주세요.");
            }

            // PlayFab 설정 적용
            PlayFabConfig.Configure();

            // 가상 화폐 추가 요청 생성 (여기서는 1000골드 지급)
            var addCurrencyRequest = new AddUserVirtualCurrencyRequest
            {
                PlayFabId = playFabId,  //보상을 받을 플레이어 ID
                VirtualCurrency = "GC",  // 게임 내에서 정의한 가상 화폐 코드
                Amount = 1000
            };

            _logger.LogInformation($"플레이어 {playFabId}에게 1000골드 추가 시도 중...");

            // 비동기 메서드 호출
            var addCurrencyResult = await PlayFabServerAPI.AddUserVirtualCurrencyAsync(addCurrencyRequest);

            // 결과 처리
            if (addCurrencyResult.Error != null)
            {
                _logger.LogError($"골드 추가 실패: {addCurrencyResult.Error.GenerateErrorReport()}");
                return new ObjectResult("골드 추가 중 오류 발생: " + addCurrencyResult.Error.GenerateErrorReport())
                {
                    StatusCode = 500
                };
            }
            else
            {
                _logger.LogInformation($"플레이어 {playFabId}에게 1000골드 추가 성공 (새 잔액: {addCurrencyResult.Result.Balance}).");
                return new OkObjectResult($"플레이어 {playFabId}에게 1000골드 추가 완료. (새 잔액: {addCurrencyResult.Result.Balance})");
            }
        }
    }
}
