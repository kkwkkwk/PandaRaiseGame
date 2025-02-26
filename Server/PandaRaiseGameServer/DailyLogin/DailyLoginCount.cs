using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using CommonLibrary;

namespace DailyLogin
{
    public class DailyLoginCount
    {
        private readonly ILogger<DailyLoginCount> _logger;
        public DailyLoginCount(ILogger<DailyLoginCount> logger)
        {
            _logger = logger;
        }

        [Function("DailyLoginCount")]
        public async Task<IActionResult> Run(
            // HTTP Trigger로 POST 요청을 허용합니다.
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("DailyLoginCount 함수가 호출되었습니다.");

            // 1. 요청 본문에서 JSON 데이터 읽기 (예: { "playFabId": "플레이어_ID" })
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody)!;
            string playFabId = data?.playFabId!;

            // playFabId가 없으면 BadRequest(400) 반환
            if (string.IsNullOrEmpty(playFabId))
            {
                _logger.LogWarning("요청 본문에 playFabId가 포함되어 있지 않습니다.");
                return new BadRequestObjectResult("플레이어의 PlayFabId를 요청 본문에 포함시켜 주세요.");
            }

            // 2. PlayFab 설정 적용 (타이틀 ID 및 Developer Secret Key)
            PlayFabConfig.Configure();
            _logger.LogInformation("PlayFab 설정이 적용되었습니다.");

            // 3. 플레이어의 User Data 조회 (UserData에는 LoginCount와 LastLoginDate가 저장되어 있음)
            _logger.LogInformation($"플레이어 {playFabId}의 UserData 조회 시작...");
            var getUserDataRequest = new GetUserDataRequest { PlayFabId = playFabId };
            var getUserDataResult = await PlayFabServerAPI.GetUserDataAsync(getUserDataRequest);
            _logger.LogInformation("UserData 조회 성공.");

            // 4. 오늘 날짜(UTC 기준)를 "yyyyMMdd" 형식으로 생성
            var kst = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");
            var nowInKorea = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, kst);
            string todayKst = nowInKorea.ToString("yyyyMMdd");
            _logger.LogInformation($"오늘 날짜 (KST): {todayKst}");

            // 5. UserData에서 "LastLoginDate"를 확인하여 오늘 이미 업데이트되었는지 판단
            bool alreadyUpdated = false;
            if (getUserDataResult.Result.Data != null && getUserDataResult.Result.Data.ContainsKey("LastLoginDate"))
            {
                string lastLoginDate = getUserDataResult.Result.Data["LastLoginDate"].Value;
                _logger.LogInformation($"플레이어의 LastLoginDate: {lastLoginDate}");
                if (lastLoginDate == todayKst)
                {
                    alreadyUpdated = true;
                    _logger.LogInformation("오늘은 이미 로그인 횟수가 업데이트 되었습니다.");
                }
            }

            if (alreadyUpdated)
            {
                return new OkObjectResult($"플레이어 {playFabId}의 로그인 횟수는 이미 업데이트 되었습니다.");
            }

            // 6. 현재 로그인 횟수("LoginCount")를 UserData에서 읽어옴 (없으면 0으로 간주)
            int currentCount = 0;
            if (getUserDataResult.Result.Data != null && getUserDataResult.Result.Data.ContainsKey("LoginCount"))
            {
                int.TryParse(getUserDataResult.Result.Data["LoginCount"].Value, out currentCount);
            }
            _logger.LogInformation($"현재 로그인 횟수: {currentCount}");

            // 7. 로그인 횟수를 1 증가시킴
            int newCount = currentCount + 1;
            _logger.LogInformation($"새로운 로그인 횟수: {newCount}");

            // 8. UserData 업데이트: "LoginCount"와 "LastLoginDate"를 오늘 날짜로 업데이트
            var updateUserDataRequest = new UpdateUserDataRequest
            {
                PlayFabId = playFabId,
                Data = new Dictionary<string, string>
                {
                    { "LoginCount", newCount.ToString() },
                    { "LastLoginDate", todayKst }
                }
            };

            _logger.LogInformation("UserData 업데이트 시도 중...");
            var updateUserDataResult = await PlayFabServerAPI.UpdateUserDataAsync(updateUserDataRequest);
            _logger.LogInformation("UserData 업데이트 완료.");

            // 9. Player Statistics 업데이트: 로그인 횟수를 리더보드에 반영하기 위해 업데이트
            var updateStatsRequest = new UpdatePlayerStatisticsRequest
            {
                PlayFabId = playFabId,
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate { StatisticName = "LoginCount", Value = newCount }
                }
            };

            _logger.LogInformation("Player Statistics 업데이트 시도 중...");
            var updateStatsResult = await PlayFabServerAPI.UpdatePlayerStatisticsAsync(updateStatsRequest);
            _logger.LogInformation("Player Statistics 업데이트 완료.");

            _logger.LogInformation($"플레이어 {playFabId}의 로그인 횟수가 {newCount}로 업데이트되었습니다.");
            return new OkObjectResult($"플레이어 {playFabId}의 로그인 횟수가 {newCount}로 업데이트되었습니다.");
        }
    }
}
