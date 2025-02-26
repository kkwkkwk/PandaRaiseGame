using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.AdminModels;
using CommonLibrary;

namespace Profile
{
    public class ChangeDisplayName
    {
        private readonly ILogger<ChangeDisplayName> _logger;

        public ChangeDisplayName(ILogger<ChangeDisplayName> logger)
        {
            _logger = logger;
        }

        [Function("ChangeDisplayName")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("ChangeDisplayName 함수가 호출되었습니다.");

            // 1. 요청 본문에서 JSON 파싱
            //    예: { "playFabId": "xxxx", "newDisplayName": "새닉네임" }
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody)!;

            string playFabId = data?.playFabId!;
            string newDisplayName = data?.newDisplayName!;

            // 유효성 검사
            if (string.IsNullOrEmpty(playFabId) || string.IsNullOrEmpty(newDisplayName))
            {
                _logger.LogWarning("playFabId 또는 newDisplayName이 누락되었습니다.");
                return new BadRequestObjectResult("playFabId와 newDisplayName 모두 입력해 주세요.");
            }

            // 2. PlayFab 설정 적용
            PlayFabConfig.Configure();
            _logger.LogInformation("PlayFab 설정 적용 완료.");

            // 3. UpdateUserTitleDisplayNameRequest로 닉네임 변경
            var updateRequest = new UpdateUserTitleDisplayNameRequest
            {
                PlayFabId = playFabId,
                DisplayName = newDisplayName
            };

            var updateResult = await PlayFabAdminAPI.UpdateUserTitleDisplayNameAsync(updateRequest);
            if (updateResult.Error != null)
            {
                _logger.LogError("닉네임 변경 실패: " + updateResult.Error.GenerateErrorReport());
                return new BadRequestObjectResult("닉네임 변경 실패: " + updateResult.Error.GenerateErrorReport());
            }

            // 4. 변경된 닉네임 확인
            string changedName = updateResult.Result.DisplayName ?? "(NoName)";
            _logger.LogInformation($"플레이어 {playFabId}의 닉네임이 '{changedName}'로 변경되었습니다.");

            // 5. 응답 DTO 생성
            var responseData = new ChangeDisplayNameResponse
            {
                PlayFabId = playFabId,
                NewDisplayName = changedName,
                Message = "닉네임 변경 완료"
            };

            // 6. JSON 결과 반환
            return new OkObjectResult(responseData);
        }

        /// <summary>
        /// 응답용 DTO
        /// </summary>
        private class ChangeDisplayNameResponse
        {
            public string? PlayFabId { get; set; }
            public string? NewDisplayName { get; set; }
            public string? Message { get; set; }
        }
    }
}
