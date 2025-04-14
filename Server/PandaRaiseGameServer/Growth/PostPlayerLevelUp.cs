using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using CommonLibrary;

namespace Growth
{
    public class PostPlayerLevelUp
    {
        private const string StatLevel = "PlayerLevel";
        private const string StatExp = "PlayerExp";
        private const string StatPoint = "PlayerStatPoint";

        [Function("PostPlayerLevelUp")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            string? playFabId = JsonConvert.DeserializeObject<dynamic>(body)?.playFabId;
            if (string.IsNullOrEmpty(playFabId))
                return new BadRequestObjectResult("playFabId 누락");

            PlayFabConfig.Configure();

            // ── 통계 조회 ─────────────────────────────
            var statRes = await PlayFabServerAPI.GetPlayerStatisticsAsync(
                new GetPlayerStatisticsRequest { PlayFabId = playFabId });
            if (statRes.Error != null)
                return new BadRequestObjectResult(statRes.Error.GenerateErrorReport());

            var dict = statRes.Result.Statistics.ToDictionary(s => s.StatisticName, s => s.Value);
            int level = dict.TryGetValue(StatLevel, out var vL) ? vL : 1;
            int exp = dict.TryGetValue(StatExp, out var vE) ? vE : 0;
            int statP = dict.TryGetValue(StatPoint, out var vP) ? vP : 0;

            int needExp = LevelExpTable.GetRequiredExp(level);
            if (needExp < 0)
                return new OkObjectResult(new PlayerLevelResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "이미 만렙입니다.",
                    Level = level,
                    CurrentExp = exp,
                    RequiredExp = 0
                });

            if (exp < needExp)
                return new OkObjectResult(new PlayerLevelResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "경험치 부족",
                    Level = level,
                    CurrentExp = exp,
                    RequiredExp = needExp
                });

            // ── 레벨업 처리 ────────────────────────────
            level += 1;
            exp -= needExp;
            statP += 5;

            await PlayFabServerAPI.UpdatePlayerStatisticsAsync(
                new UpdatePlayerStatisticsRequest
                {
                    PlayFabId = playFabId,
                    Statistics = new()
                    {
                        new StatisticUpdate { StatisticName = StatLevel, Value = level },
                        new StatisticUpdate { StatisticName = StatExp,   Value = exp   },
                        new StatisticUpdate { StatisticName = StatPoint, Value = statP }
                    }
                });

            int nextNeed = LevelExpTable.GetRequiredExp(level);

            var resp = new PlayerLevelResponse
            {
                IsSuccess = true,
                ErrorMessage = "",
                Level = level,
                CurrentExp = exp,
                RequiredExp = nextNeed
            };
            return new OkObjectResult(resp);
        }
    }
}
