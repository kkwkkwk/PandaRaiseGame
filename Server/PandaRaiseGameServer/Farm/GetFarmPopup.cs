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
using Farm;           // FarmDTO

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
            _logger.LogInformation("[GetFarmPopup] Invoked");
            PlayFabConfig.Configure();

            // 1) 요청 파싱
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<GetFarmPopupRequest>(body)
                       ?? throw new Exception("Invalid request");
            _logger.LogInformation("[GetFarmPopup] playFabId={PlayFabId}", data.PlayFabId);

            // 2) User Data 조회
            var udReq = new GetUserDataRequest
            {
                PlayFabId = data.PlayFabId,
                Keys = new List<string> { "FarmPlots", "SeedInventory" }
            };
            var udRes = await PlayFabServerAPI.GetUserDataAsync(udReq);
            if (udRes.Error != null)
            {
                _logger.LogError(udRes.Error.GenerateErrorReport(), "[GetFarmPopup] GetUserDataAsync failed");
                return new OkObjectResult(new FarmPopupResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "데이터 조회 실패"
                });
            }

            // 3) FarmPlots 파싱 or 초기화
            bool needInitPlots = false;
            List<PlotInfoServer> plotsServer;
            if (udRes.Result.Data.TryGetValue("FarmPlots", out var pJson)
                && !string.IsNullOrEmpty(pJson.Value))
            {
                plotsServer = JsonConvert.DeserializeObject<List<PlotInfoServer>>(pJson.Value)!;
                _logger.LogInformation("[GetFarmPopup] Loaded {Count} plots from user data", plotsServer.Count);
            }
            else
            {
                plotsServer = Enumerable.Range(0, 6)
                    .Select(i => new PlotInfoServer { PlotIndex = i })
                    .ToList();
                needInitPlots = true;
                _logger.LogInformation("[GetFarmPopup] No FarmPlots found, initializing {Count} empty plots", plotsServer.Count);
            }

            // 4) SeedInventory 파싱 or 초기화
            bool needInitInv = false;
            Dictionary<string, int> invDict;
            if (udRes.Result.Data.TryGetValue("SeedInventory", out var iJson)
                && !string.IsNullOrEmpty(iJson.Value))
            {
                invDict = JsonConvert.DeserializeObject<Dictionary<string, int>>(iJson.Value)!;
                _logger.LogInformation("[GetFarmPopup] Loaded SeedInventory with {Count} entries", invDict.Count);
            }
            else
            {
                invDict = new Dictionary<string, int>();
                needInitInv = true;
                _logger.LogInformation("[GetFarmPopup] No SeedInventory found, initializing empty inventory");
            }

            // 5) (옵션) User Data 초기화
            if (needInitPlots || needInitInv)
            {
                var updateData = new Dictionary<string, string>();
                if (needInitPlots)
                    updateData["FarmPlots"] = JsonConvert.SerializeObject(plotsServer);
                if (needInitInv)
                    updateData["SeedInventory"] = JsonConvert.SerializeObject(invDict);

                var updRes = await PlayFabServerAPI.UpdateUserDataAsync(new UpdateUserDataRequest
                {
                    PlayFabId = data.PlayFabId,
                    Data = updateData
                });
                if (updRes.Error != null)
                    _logger.LogError(updRes.Error.GenerateErrorReport(), "[GetFarmPopup] Failed to initialize user data");
                else
                    _logger.LogInformation("[GetFarmPopup] Initialized user data keys: {Keys}",
                        string.Join(", ", updateData.Keys));
            }

            // 6) TitleData에서 씨앗 정의 로드
            var seedDefs = await LoadSeedDefinitions();

            // 7) 응답용 DTO 생성
            var inventory = seedDefs
                .Where(s => s.Id != null && invDict.TryGetValue(s.Id!, out var c) && c > 0)
                .Select(s => new UserSeedInfo
                {
                    SeedId = s.Id,
                    SeedName = s.Name,
                    SpriteName = s.SpriteName,
                    Count = invDict[s.Id!]
                })
                .ToList();

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

            _logger.LogInformation("[GetFarmPopup] Returning {PlotCount} plots and {InvCount} inventory items",
                plotsResp.Count, inventory.Count);

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
