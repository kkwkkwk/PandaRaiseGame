// GetFarmPopup.cs
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
using CommonLibrary;  // PlayFabConfig

namespace Farm
{
    public class GetFarmPopup
    {
        private readonly ILogger<GetFarmPopup> _logger;
        public GetFarmPopup(ILogger<GetFarmPopup> logger) => _logger = logger;

        [Function("GetFarmPopup")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "GetFarmPopup")] HttpRequest req)
        {
            PlayFabConfig.Configure();

            // 1) 요청 파싱
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<GetFarmPopupRequest>(body)
                       ?? throw new Exception("Invalid request");

            // 2) FarmPlots UserData 조회 (플롯 상태)
            var udRes = await PlayFabServerAPI.GetUserDataAsync(new GetUserDataRequest
            {
                PlayFabId = data.PlayFabId,
                Keys = new List<string> { "FarmPlots" }
            });
            List<PlotInfoServer> plotsServer =
                udRes.Result.Data.TryGetValue("FarmPlots", out var pj) && !string.IsNullOrEmpty(pj.Value)
                ? JsonConvert.DeserializeObject<List<PlotInfoServer>>(pj.Value)!
                : Enumerable.Range(0, 6).Select(i => new PlotInfoServer { PlotIndex = i }).ToList();

            // 3) 씨앗 인벤토리 조회 → VirtualCurrency
            var invRes = await PlayFabServerAPI.GetUserInventoryAsync(new GetUserInventoryRequest
            {
                PlayFabId = data.PlayFabId
            });
            var vc = invRes.Result.VirtualCurrency ?? new Dictionary<string, int>();

            // 4) TitleData에서 씨앗 정의 로드
            var seedDefs = await LoadSeedDefinitions();

            // 5) UserSeedInfo 생성 (virtual currency 기준)
            var inventory = seedDefs
                .Select(s => new UserSeedInfo
                {
                    SeedId = s.Id,
                    SeedName = s.Name,
                    SpriteName = s.SpriteName,
                    Count = vc.TryGetValue(s.Id!, out var c) ? c : 0
                })
                .Where(u => u.Count > 0)
                .ToList();

            // 6) PlotInfo 생성
            var plotsResp = plotsServer
                .Select(p => new PlotInfo
                {
                    PlotIndex = p.PlotIndex,
                    HasSeed = p.HasSeed,
                    SeedId = p.SeedId,
                    PlantedTimeUtc = p.PlantedTimeUtc,
                    GrowthDurationSeconds = p.GrowthDurationSeconds
                })
                .ToList();

            // 7) 응답
            return new OkObjectResult(new FarmPopupResponse
            {
                IsSuccess = true,
                Plots = plotsResp,
                Inventory = inventory
            });
        }

        // TitleData “SeedsList” 파싱용 헬퍼
        public async Task<List<SeedDef>> LoadSeedDefinitions()
        {
            var tdRes = await PlayFabServerAPI.GetTitleDataAsync(new GetTitleDataRequest
            {
                Keys = new List<string> { "SeedsList" }
            });
            var json = tdRes.Result.Data["SeedsList"];
            return JsonConvert.DeserializeObject<List<SeedDef>>(json)!;
        }

        // 내부 모델: 서버에 저장되는 Plot 정보
        public class PlotInfoServer
        {
            public int PlotIndex { get; set; }
            public bool HasSeed { get; set; }
            public string? SeedId { get; set; }
            public DateTime PlantedTimeUtc { get; set; }
            public int GrowthDurationSeconds { get; set; }
        }

        // 내부 모델: TitleData에서 내려오는 씨앗 정의
        public class SeedDef
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public string? SpriteName { get; set; }
            public string? Info { get; set; }
            public int GrowthDurationSeconds { get; set; }
            public string? StatType { get; set; }
            public float StatAmount { get; set; }
        }
    }
}
