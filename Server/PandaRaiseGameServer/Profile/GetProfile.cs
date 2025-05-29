using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using CommonLibrary; // PlayFabConfig

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

            // 1. 요청 바디 파싱
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation("요청 바디: {Body}", requestBody);
            dynamic data = JsonConvert.DeserializeObject(requestBody)!;
            string playFabId = data?.playFabId!;
            _logger.LogInformation("파싱된 playFabId: {PlayFabId}", playFabId);
            if (string.IsNullOrEmpty(playFabId))
            {
                _logger.LogWarning("요청 본문에 playFabId가 포함되어 있지 않습니다.");
                return new BadRequestObjectResult("PlayFabId가 누락되었습니다.");
            }

            // 2. PlayFab 설정
            PlayFabConfig.Configure();

            // 3. 계정 정보 조회
            _logger.LogInformation("플레이어 {PlayFabId}의 계정 정보 조회 중...", playFabId);
            var profileReq = new GetPlayerProfileRequest
            {
                PlayFabId = playFabId,
                ProfileConstraints = new PlayerProfileViewConstraints
                {
                    ShowCreated = true,
                    ShowLastLogin = true,
                    ShowDisplayName = true
                }
            };
            var profileRes = await PlayFabServerAPI.GetPlayerProfileAsync(profileReq);
            if (profileRes.Error != null)
            {
                _logger.LogError("GetPlayerProfile 실패: {Error}", profileRes.Error.GenerateErrorReport());
                return new BadRequestObjectResult("GetPlayerProfile 오류: " + profileRes.Error.GenerateErrorReport());
            }
            var playerProfile = profileRes.Result.PlayerProfile;
            if (playerProfile == null)
            {
                return new OkObjectResult(new { message = "플레이어 프로필 정보가 없습니다." });
            }
            _logger.LogInformation("계정 정보 조회 성공: DisplayName={DisplayName}", playerProfile.DisplayName);

            // 4. User Data 조회 (출석 일수 등)
            _logger.LogInformation("플레이어 {PlayFabId}의 User Data 조회 중...", playFabId);
            var userDataRes = await PlayFabServerAPI.GetUserDataAsync(
                new GetUserDataRequest { PlayFabId = playFabId });
            if (userDataRes.Error != null)
            {
                _logger.LogError("GetUserData 실패: {Error}", userDataRes.Error.GenerateErrorReport());
                return new BadRequestObjectResult("GetUserData 오류: " + userDataRes.Error.GenerateErrorReport());
            }
            _logger.LogInformation("User Data 조회 성공.");

            // 5. 통계 조회 (PlayerLevel, PlayerExp, UserPower)
            _logger.LogInformation("플레이어 {PlayFabId}의 통계(PlayerLevel, PlayerExp, UserPower) 조회 중...", playFabId);
            var statsReq = new GetPlayerStatisticsRequest
            {
                PlayFabId = playFabId,
                // 만약 모든 통계를 가져온 뒤 필터링해도 됩니다.
                //StatisticNames = new List<string> { "PlayerLevel", "PlayerExp", "UserPower" }
            };
            var statsRes = await PlayFabServerAPI.GetPlayerStatisticsAsync(statsReq);
            if (statsRes.Error != null)
            {
                _logger.LogError("GetPlayerStatistics 실패: {Error}", statsRes.Error.GenerateErrorReport());
                // 통계 조회 실패여도 기본값으로 처리
            }
            var stats = statsRes.Result.Statistics;
            int playerLevel = stats.FirstOrDefault(s => s.StatisticName == "PlayerLevel")?.Value ?? 0;
            int playerExp = stats.FirstOrDefault(s => s.StatisticName == "PlayerExp")?.Value ?? 0;
            int userPower = stats.FirstOrDefault(s => s.StatisticName == "UserPower")?.Value ?? 0;
            _logger.LogInformation("통계 조회 완료: Level={Level}, Exp={Exp}, Power={Power}",
                playerLevel, playerExp, userPower);

            // 6. 필요한 데이터 가공
            string displayName = playerProfile.DisplayName ?? "(NoName)";
            DateTime? createdUtc = playerProfile.Created;
            DateTime? lastLoginUtc = playerProfile.LastLogin;
            var kst = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");
            string createdKst = createdUtc.HasValue
                ? TimeZoneInfo.ConvertTimeFromUtc(createdUtc.Value, kst).ToString("yyyy-MM-dd HH:mm:ss")
                : "(unknown)";
            string lastLoginKst = lastLoginUtc.HasValue
                ? TimeZoneInfo.ConvertTimeFromUtc(lastLoginUtc.Value, kst).ToString("yyyy-MM-dd HH:mm:ss")
                : "(unknown)";

            int loginCount = 0;
            if (userDataRes.Result.Data.TryGetValue("LoginCount", out var loginJson))
                int.TryParse(loginJson.Value, out loginCount);

            // 7. 응답 DTO 생성
            var profileData = new ProfileData
            {
                PlayFabId = playFabId,
                DisplayName = displayName,
                CreatedKst = createdKst,
                LastLogin = lastLoginKst,
                LoginCount = loginCount,
                PlayerLevel = playerLevel,
                PlayerExp = playerExp,
                UserPower = userPower
            };

            // 8. 응답 반환
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
            public int PlayerLevel { get; set; }
            public int PlayerExp { get; set; }
            public int UserPower { get; set; }
        }
    }
}
