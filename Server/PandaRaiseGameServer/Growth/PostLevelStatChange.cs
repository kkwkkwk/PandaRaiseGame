using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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
    public class PostLevelStatChange
    {
        private const string StatRemain = "PlayerStatPoint";

        [Function("PostLevelStatChange")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            var dto = JsonConvert.DeserializeObject<LevelStatChangeRequest>(
                          await new StreamReader(req.Body).ReadToEndAsync());

            if (dto == null || string.IsNullOrEmpty(dto.PlayFabId) ||
                string.IsNullOrEmpty(dto.AbilityName) ||
                !AbilityStatTable.Meta.ContainsKey(dto.AbilityName) ||
                (dto.Action != "increase" && dto.Action != "reset"))
                return new BadRequestObjectResult("파라미터 오류");

            PlayFabConfig.Configure();

            // 통계 조회
            var statRes = await PlayFabServerAPI.GetPlayerStatisticsAsync(
                new GetPlayerStatisticsRequest { PlayFabId = dto.PlayFabId });
            if (statRes.Error != null)
                return new BadRequestObjectResult(statRes.Error.GenerateErrorReport());

            var dict = statRes.Result.Statistics.ToDictionary(s => s.StatisticName, s => s.Value);
            dict.TryAdd(StatRemain, 0);
            foreach (var ab in AbilityStatTable.Meta.Keys)
                dict.TryAdd(AbilityStatTable.StatKey(ab), 0);

            int remain = dict[StatRemain];
            string key = AbilityStatTable.StatKey(dto.AbilityName);
            int curLv = dict[key];

            if (dto.Action == "increase")
            {
                if (remain <= 0)
                    return new OkObjectResult(new LevelStatResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = "잔여 스탯 부족"
                    });

                remain -= 1;
                curLv += 1;
            }
            else // reset
            {
                remain += curLv;   // 투자분 환급
                curLv = 0;
            }

            // 통계 저장
            await PlayFabServerAPI.UpdatePlayerStatisticsAsync(
                new UpdatePlayerStatisticsRequest
                {
                    PlayFabId = dto.PlayFabId,
                    Statistics = new()
                    {
                        new StatisticUpdate { StatisticName = StatRemain, Value = remain },
                        new StatisticUpdate { StatisticName = key,        Value = curLv  }
                    }
                });

            // 최신 dict 반영
            dict[StatRemain] = remain;
            dict[key] = curLv;

            // 응답 DTO
            var list = new List<ServerAbilityStatData>();
            foreach (var (name, meta) in AbilityStatTable.Meta)
            {
                list.Add(new ServerAbilityStatData
                {
                    AbilityName = name,
                    BaseValue = meta.baseV,
                    IncrementValue = meta.incV,
                    CurrentLevel = dict[AbilityStatTable.StatKey(name)]
                });
            }

            return new OkObjectResult(new LevelStatResponse
            {
                IsSuccess = true,
                ErrorMessage = "",
                PlayerLevel = dict.GetValueOrDefault("PlayerLevel", 1),
                RemainingStatPoints = remain,
                AbilityStatList = list
            });
        }
    }
}
