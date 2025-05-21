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
using CommonLibrary;
using static Farm.GetFarmPopup;

namespace Farm
{
    public class HarvestSeed
    {
        private readonly ILogger<HarvestSeed> _logger;
        public HarvestSeed(ILogger<HarvestSeed> logger) => _logger = logger;

        [Function("HarvestSeed")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "HarvestSeed")] HttpRequest req)
        {
            PlayFabConfig.Configure();

            // 1) 요청 파싱
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<HarvestSeedRequest>(body)
                       ?? throw new Exception("Invalid request");

            // 2) FarmPlots 조회
            var udRes = await PlayFabServerAPI.GetUserDataAsync(new GetUserDataRequest
            {
                PlayFabId = data.PlayFabId,
                Keys = new List<string> { "FarmPlots" }
            });
            var plots = JsonConvert.DeserializeObject<List<PlotInfoServer>>(
                udRes.Result.Data["FarmPlots"].Value);
            var plot = plots!.First(p => p.PlotIndex == data.PlotIndex);

            // 3) 성장 완료 체크
            if (!plot.HasSeed ||
                (DateTime.UtcNow - plot.PlantedTimeUtc).TotalSeconds < plot.GrowthDurationSeconds)
            {
                return new OkObjectResult(new HarvestSeedResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "아직 자라지 않았습니다."
                });
            }

            // 4) 보상 정보 조회
            var seedDefs = await new GetFarmPopup(null!).LoadSeedDefinitions();
            var def = seedDefs.First(s => s.Id == plot.SeedId);

            // 5) Player Statistics 업데이트
            await PlayFabServerAPI.UpdatePlayerStatisticsAsync(new UpdatePlayerStatisticsRequest
            {
                PlayFabId = data.PlayFabId,
                Statistics = new List<StatisticUpdate> {
                    new StatisticUpdate { StatisticName = def.StatType, Value = (int)def.StatAmount }
                }
            });

            // 6) 플롯 초기화 & 저장
            plot.HasSeed = false;
            plot.SeedId = null!;
            plot.PlantedTimeUtc = DateTime.MinValue;
            plot.GrowthDurationSeconds = 0;
            await PlayFabServerAPI.UpdateUserDataAsync(new UpdateUserDataRequest
            {
                PlayFabId = data.PlayFabId,
                Data = new Dictionary<string, string>
                {
                    ["FarmPlots"] = JsonConvert.SerializeObject(plots)
                }
            });

            // 7) 응답
            return new OkObjectResult(new HarvestSeedResponse
            {
                IsSuccess = true,
                StatType = def.StatType,
                Amount = def.StatAmount
            });
        }
    }
}
