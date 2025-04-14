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
    public class GetLevelStatData
    {
        private const string StatRemain = "PlayerStatPoint";

        [Function("GetLevelStatData")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            string? playFabId = JsonConvert.DeserializeObject<dynamic>(await new StreamReader(req.Body).ReadToEndAsync())?.playFabId;
            if (string.IsNullOrEmpty(playFabId))
                return new BadRequestObjectResult("playFabId 누락");

            PlayFabConfig.Configure();

            // ── 통계 조회
            var statRes = await PlayFabServerAPI.GetPlayerStatisticsAsync(
                new GetPlayerStatisticsRequest { PlayFabId = playFabId });
            if (statRes.Error != null)
                return new BadRequestObjectResult(statRes.Error.GenerateErrorReport());

            var dict = statRes.Result.Statistics.ToDictionary(s => s.StatisticName, s => s.Value);

            // 기본값 보정
            bool needInit = false;
            if (!dict.ContainsKey(StatRemain)) { dict[StatRemain] = 0; needInit = true; }

            foreach (var ability in AbilityStatTable.Meta.Keys)
            {
                string key = AbilityStatTable.StatKey(ability);
                if (!dict.ContainsKey(key)) { dict[key] = 0; needInit = true; }
            }

            if (needInit)
            {
                var initList = new List<StatisticUpdate>();
                foreach (var kv in dict)
                    initList.Add(new StatisticUpdate { StatisticName = kv.Key, Value = kv.Value });

                await PlayFabServerAPI.UpdatePlayerStatisticsAsync(
                    new UpdatePlayerStatisticsRequest
                    {
                        PlayFabId = playFabId,
                        Statistics = initList
                    });
            }

            // ── DTO 구성
            var abilityList = new List<ServerAbilityStatData>();
            foreach (var (name, meta) in AbilityStatTable.Meta)
            {
                abilityList.Add(new ServerAbilityStatData
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
                RemainingStatPoints = dict[StatRemain],
                AbilityStatList = abilityList
            });
        }
    }
}
