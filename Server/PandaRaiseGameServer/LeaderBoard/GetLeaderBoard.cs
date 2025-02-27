using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PlayFab;
using PlayFab.ServerModels;
using CommonLibrary;

namespace LeaderBoard
{
    public class GetLeaderBoard
    {
        private readonly ILogger<GetLeaderBoard> _logger;

        public GetLeaderBoard(ILogger<GetLeaderBoard> logger)
        {
            _logger = logger;
        }

        [Function("GetLeaderBoard")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("GetLeaderBoard 함수가 호출되었습니다.");

            // 원하는 통계 이름
            string statisticName = "LoginCount";
            // 상위 몇 명까지 불러올지 예시 (직접 요청 본문에서 파싱하거나, 파라미터 처리 가능)
            int startPosition = 0;
            int maxResultsCount = 100;

            // 1. (선택) 요청 본문에서 추가 파라미터 파싱 - 여기서는 생략
            //    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //    dynamic data = JsonConvert.DeserializeObject(requestBody);
            //    if (data != null) { ... }

            // 2. PlayFab 설정 적용 (TitleId, DeveloperSecretKey)
            PlayFabConfig.Configure();

            // 3. GetLeaderboard 요청 생성
            var leaderboardRequest = new GetLeaderboardRequest
            {
                StatisticName = statisticName,
                StartPosition = startPosition,
                MaxResultsCount = maxResultsCount
            };

            // 4. 서버 API로 리더보드 조회
            var leaderboardResult = await PlayFabServerAPI.GetLeaderboardAsync(leaderboardRequest);
            if (leaderboardResult.Error != null)
            {
                _logger.LogError("리더보드 조회 실패: " + leaderboardResult.Error.GenerateErrorReport());
                return new BadRequestObjectResult("리더보드 조회 실패: " + leaderboardResult.Error.GenerateErrorReport());
            }

            _logger.LogInformation("리더보드 조회 성공.");

            // 5. 결과를 DTO(List) 형태로 가공
            var leaderboardList = new List<LeaderBoardEntryData>();
            foreach (var entry in leaderboardResult.Result.Leaderboard)
            {
                leaderboardList.Add(new LeaderBoardEntryData
                {
                    Rank = entry.Position + 1,      // 0-based → 1-based
                    PlayFabId = entry.PlayFabId,
                    DisplayName = entry.DisplayName ?? "",
                    StatValue = entry.StatValue
                });
            }

            // 6. JSON으로 반환
            //    OkObjectResult는 내부적으로 JSON 직렬화하여 클라이언트에 보냅니다.
            return new OkObjectResult(leaderboardList);
        }

        /// <summary>
        /// 리더보드 항목 전송용 DTO
        /// </summary>
        private class LeaderBoardEntryData
        {
            public int Rank { get; set; }
            public string? PlayFabId { get; set; }
            public string? DisplayName { get; set; }
            public int StatValue { get; set; }
        }
    }
}
