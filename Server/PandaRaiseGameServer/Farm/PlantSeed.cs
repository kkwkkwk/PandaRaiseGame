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

            // 2) FarmPlots UserData 조회
            var udRes = await PlayFabServerAPI.GetUserDataAsync(new GetUserDataRequest
            {
                PlayFabId = data.PlayFabId,
                Keys = new List<string> { "FarmPlots" }
            });
            var plots = udRes.Result.Data.TryGetValue("FarmPlots", out var pj) && !string.IsNullOrEmpty(pj.Value)
                ? JsonConvert.DeserializeObject<List<PlotInfoServer>>(pj.Value)!
                : Enumerable.Range(0, 6).Select(i => new PlotInfoServer { PlotIndex = i }).ToList();

            // 3) 씨앗 수 체크 (VirtualCurrency)
            var invRes = await PlayFabServerAPI.GetUserInventoryAsync(new GetUserInventoryRequest
            {
                PlayFabId = data.PlayFabId
            });
            var vc = invRes.Result.VirtualCurrency ?? new Dictionary<string, int>();
            vc.TryGetValue(data.SeedId!, out var have);
            if (have < 1)
                return new OkObjectResult(new PlantSeedResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "씨앗 부족"
                });

            // 4) 씨앗 차감
            var subRes = await PlayFabServerAPI.SubtractUserVirtualCurrencyAsync(new SubtractUserVirtualCurrencyRequest
            {
                PlayFabId = data.PlayFabId,
                VirtualCurrency = data.SeedId,
                Amount = 1
            });
            if (subRes.Error != null)
                return new OkObjectResult(new PlantSeedResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "씨앗 차감 실패"
                });

            // 5) 성장 시간 조회
            var seedDefs = await new GetFarmPopup(null!).LoadSeedDefinitions();
            var def = seedDefs.First(s => s.Id == data.SeedId);

            // 6) FarmPlots 업데이트
            var plot = plots.First(p => p.PlotIndex == data.PlotIndex);
            plot.HasSeed = true;
            plot.SeedId = def.Id;
            plot.PlantedTimeUtc = DateTime.UtcNow;
            plot.GrowthDurationSeconds = def.GrowthDurationSeconds;

            // 7) UserData에 FarmPlots만 저장
            await PlayFabServerAPI.UpdateUserDataAsync(new UpdateUserDataRequest
            {
                PlayFabId = data.PlayFabId,
                Data = new Dictionary<string, string>
                {
                    ["FarmPlots"] = JsonConvert.SerializeObject(plots)
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
