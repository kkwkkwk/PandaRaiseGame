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

namespace Farm
{
    public class PlantSeed
    {
        private readonly ILogger<PlantSeed> _logger;
        public PlantSeed(ILogger<PlantSeed> logger) => _logger = logger;

        [Function("PlantSeed")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "PlantSeed")] HttpRequest req)
        {
            PlayFabConfig.Configure();

            // 1) 요청 파싱
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<PlantSeedRequest>(body)
                       ?? throw new Exception("Invalid request");

            // 2) User Data 조회
            var udRes = await PlayFabServerAPI.GetUserDataAsync(new GetUserDataRequest
            {
                PlayFabId = data.PlayFabId,
                Keys = new List<string> { "FarmPlots", "SeedInventory" }
            });
            if (udRes.Error != null)
            {
                _logger.LogError(udRes.Error.GenerateErrorReport(), "[PlantSeed]");
                return new OkObjectResult(new PlantSeedResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "데이터 조회 실패"
                });
            }

            // 3) 파싱: 내부 모델
            var plots = udRes.Result.Data.TryGetValue("FarmPlots", out var pJson)
                ? JsonConvert.DeserializeObject<List<GetFarmPopup.PlotInfoServer>>(pJson.Value)!
                : Enumerable.Range(0, 6).Select(i => new GetFarmPopup.PlotInfoServer { PlotIndex = i }).ToList();
            var invDict = udRes.Result.Data.TryGetValue("SeedInventory", out var iJson)
                ? JsonConvert.DeserializeObject<Dictionary<string, int>>(iJson.Value)!
                : new Dictionary<string, int>();

            // 4) 재고 체크
            if (!invDict.TryGetValue(data.SeedId!, out var have) || have < 1)
                return new OkObjectResult(new PlantSeedResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "씨앗 부족"
                });

            // 5) TitleData에서 성장 시간 조회
            var seedDefs = await new GetFarmPopup(null!).LoadSeedDefinitions();
            var def = seedDefs.First(s => s.Id == data.SeedId);

            // 6) 재고 차감 & plots 업데이트
            invDict[data.SeedId!] = have - 1;
            var plot = plots.First(p => p.PlotIndex == data.PlotIndex);
            plot.HasSeed = true;
            plot.SeedId = def.Id;
            plot.PlantedTimeUtc = DateTime.UtcNow;
            plot.GrowthDurationSeconds = def.GrowthDurationSeconds;

            // 7) User Data 저장
            await PlayFabServerAPI.UpdateUserDataAsync(new UpdateUserDataRequest
            {
                PlayFabId = data.PlayFabId,
                Data = new Dictionary<string, string>
                {
                    ["FarmPlots"] = JsonConvert.SerializeObject(plots),
                    ["SeedInventory"] = JsonConvert.SerializeObject(invDict)
                }
            });

            // 8) 응답
            return new OkObjectResult(new PlantSeedResponse
            {
                IsSuccess = true,
                GrowthDurationSeconds = def.GrowthDurationSeconds
            });
        }
    }
}
