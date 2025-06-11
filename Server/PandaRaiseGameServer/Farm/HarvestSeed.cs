// HarvestSeed.cs
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
            if (udRes.Error != null)
                return new OkObjectResult(new HarvestSeedResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "FarmPlots 조회 실패"
                });

            var plots = JsonConvert.DeserializeObject<List<PlotInfoServer>>(
                udRes.Result.Data["FarmPlots"].Value)!;
            var plot = plots.FirstOrDefault(p => p.PlotIndex == data.PlotIndex);

            // 3) 성장 완료 검사
            if (plot == null || !plot.HasSeed ||
                (DateTime.UtcNow - plot.PlantedTimeUtc).TotalSeconds < plot.GrowthDurationSeconds)
            {
                return new OkObjectResult(new HarvestSeedResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "아직 자라지 않았습니다."
                });
            }

            string seedId = plot.SeedId!;

            // 4) 인벤토리 조회
            var invRes = await PlayFabServerAPI.GetUserInventoryAsync(new GetUserInventoryRequest
            {
                PlayFabId = data.PlayFabId
            });
            if (invRes.Error != null)
            {
                _logger.LogError(invRes.Error.GenerateErrorReport(), "[HarvestSeed] 인벤토리 조회 실패");
                return new OkObjectResult(new HarvestSeedResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "인벤토리 조회 실패"
                });
            }
            var inventory = invRes.Result.Inventory;

            // 5) 기존 인스턴스 확인
            var existing = inventory.FirstOrDefault(i => i.ItemId == seedId);
            if (existing == null)
            {
                // 5a) 신규 인스턴스 생성 (Grant → Uses=1)
                _logger.LogInformation("[HarvestSeed] GrantItemsToUser: {0}", seedId);
                var grantRes = await PlayFabServerAPI.GrantItemsToUserAsync(new GrantItemsToUserRequest
                {
                    PlayFabId = data.PlayFabId,
                    CatalogVersion = "Farm",
                    ItemIds = new List<string> { seedId }
                });
                if (grantRes.Error != null)
                {
                    _logger.LogError(grantRes.Error.GenerateErrorReport(),
                        "[HarvestSeed] GrantItemsToUser 실패: {0}", seedId);
                    return new OkObjectResult(new HarvestSeedResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = "아이템 그랜트 실패"
                    });
                }
            }
            else
            {
                // 5b) 기존 Uses에 +1 추가
                _logger.LogInformation("[HarvestSeed] ModifyItemUses existing {0} +1", seedId);
                var modRes = await PlayFabServerAPI.ModifyItemUsesAsync(new ModifyItemUsesRequest
                {
                    PlayFabId = data.PlayFabId,
                    ItemInstanceId = existing.ItemInstanceId,
                    UsesToAdd = 1
                });
                if (modRes.Error != null)
                {
                    _logger.LogError(modRes.Error.GenerateErrorReport(),
                        "[HarvestSeed] ModifyItemUses 실패: {0}", seedId);
                    return new OkObjectResult(new HarvestSeedResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = "아이템 Uses 추가 실패"
                    });
                }
            }

            // 6) 플롯 초기화 및 저장
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
                StatType = seedId,  // 프론트에서 itemType 용도로 사용
                Amount = 1
            });
        }
    }
}
