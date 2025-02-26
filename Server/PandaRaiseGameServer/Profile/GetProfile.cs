using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using CommonLibrary;

namespace Profile
{
    public class GetProfile
    {
        private readonly ILogger<GetProfile> _logger;

        public GetProfile(ILogger<GetProfile> logger)
        {
            _logger = logger;
        }

        [Function("GetProfile")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("GetProfile 함수가 호출되었습니다.");

            // 1. HTTP 요청에서 JSON 바디 파싱 (예: { "playFabId": "..." })
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody)!;
            string playFabId = data?.playFabId!;

            if (string.IsNullOrEmpty(playFabId))
            {
                _logger.LogWarning("요청 본문에 playFabId가 포함되어 있지 않습니다.");
                return new BadRequestObjectResult("PlayFabId가 누락되었습니다.");
            }

            // 2. PlayFab 설정 (TitleId, SecretKey)
            PlayFabConfig.Configure();

            // 3. PlayFab에서 PlayerInfo(계정 정보) 조회
            _logger.LogInformation($"플레이어 {playFabId}의 계정 정보 조회 중...");
            var playerProfileReq = new GetPlayerProfileRequest { PlayFabId = playFabId };
            var playerProfileResult = await PlayFabServerAPI.GetPlayerProfileAsync(playerProfileReq);
            if (playerProfileResult.Error != null)
            {
                _logger.LogError("GetPlayerProfile 실패: " + playerProfileResult.Error.GenerateErrorReport());
                return new BadRequestObjectResult("GetPlayerProfile 오류: " + playerProfileResult.Error.GenerateErrorReport());
            }
            _logger.LogInformation("계정 정보 조회 성공.");

            // 4. User Data (출석 일수 등) 조회
            _logger.LogInformation($"플레이어 {playFabId}의 User Data 조회 중...");
            var userDataReq = new GetUserDataRequest { PlayFabId = playFabId };
            var userDataResult = await PlayFabServerAPI.GetUserDataAsync(userDataReq);
            if (userDataResult.Error != null)
            {
                _logger.LogError("GetUserData 실패: " + userDataResult.Error.GenerateErrorReport());
                return new BadRequestObjectResult("GetUserData 오류: " + userDataResult.Error.GenerateErrorReport());
            }
            _logger.LogInformation("User Data 조회 성공.");

            // 5. 필요한 정보(닉네임, 가입/로그인 날짜, 출석 일수 등) 꺼내기
            var playerProfile = playerProfileResult.Result.PlayerProfile;
            if (playerProfile == null)
            {
                return new OkObjectResult(new { message = "플레이어 프로필 정보가 없습니다." });
            }

            string displayName = playerProfile.DisplayName ?? "(NoName)";
            DateTime? createdUtc = playerProfile.Created;   // UTC
            DateTime? lastLoginUtc = playerProfile.LastLogin; // UTC

            // User Data 중 "LoginCount" 키가 존재하는지 확인
            int loginCount = 0;
            if (userDataResult.Result.Data != null && userDataResult.Result.Data.ContainsKey("LoginCount"))
            {
                int.TryParse(userDataResult.Result.Data["LoginCount"].Value, out loginCount);
            }

            // === KST 변환 ===
            var kst = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");

            DateTime? createdKst = null;
            if (createdUtc.HasValue)
            {
                createdKst = TimeZoneInfo.ConvertTimeFromUtc(createdUtc.Value, kst);
            }

            DateTime? lastLoginKst = null;
            if (lastLoginUtc.HasValue)
            {
                lastLoginKst = TimeZoneInfo.ConvertTimeFromUtc(lastLoginUtc.Value, kst);
            }

            // 6. 응답용 DTO 작성
            var profileData = new ProfileData
            {
                PlayFabId = playFabId,
                DisplayName = displayName,
                CreatedKst = createdKst?.ToString("yyyy-MM-dd HH:mm:ss") ?? "(unknown)",
                LastLogin = lastLoginKst?.ToString("yyyy-MM-dd HH:mm:ss") ?? "(unknown)",
                LoginCount = loginCount
            };

            // 7. JSON 응답
            return new OkObjectResult(profileData);
        }

        /// <summary>
        /// GetProfile 응답용 DTO
        /// </summary>
        private class ProfileData
        {
            public string? PlayFabId { get; set; }
            public string? DisplayName { get; set; }
            public string? CreatedKst { get; set; }
            public string? LastLogin { get; set; }
            public int LoginCount { get; set; }
        }
    }
}
