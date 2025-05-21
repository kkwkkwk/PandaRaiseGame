// GetSeeds.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using CommonLibrary; // PlayFabConfig
using Farm;          // FarmDTO (SeedData 에 Count 프로퍼티 추가)

namespace Farm
{
    public class GetSeeds
    {
        private readonly ILogger<GetSeeds> _logger;
        public GetSeeds(ILogger<GetSeeds> logger) => _logger = logger;

        [Function("GetSeeds")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "GetSeeds")] HttpRequest req)
        {
            _logger.LogInformation("[GetSeeds] invoked");
            PlayFabConfig.Configure();

            // 1) TitleData 조회 (키: "SeedsList")
            var tdRes = await PlayFabServerAPI.GetTitleDataAsync(
                new GetTitleDataRequest { Keys = new List<string> { "SeedsList" } });
            if (tdRes.Error != null)
            {
                _logger.LogError(tdRes.Error.GenerateErrorReport(), "[GetSeeds] TitleData 조회 실패");
                return new OkObjectResult(new SeedsResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "씨앗 목록 조회 실패"
                });
            }
            if (!tdRes.Result.Data.TryGetValue("SeedsList", out var json) || string.IsNullOrEmpty(json))
            {
                return new OkObjectResult(new SeedsResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "SeedsList 데이터가 없습니다"
                });
            }

            // 2) JSON → List<SeedData> 변환
            List<SeedData>? list;
            try
            {
                list = JsonConvert.DeserializeObject<List<SeedData>>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetSeeds] JSON 파싱 오류");
                return new OkObjectResult(new SeedsResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "데이터 형식 오류"
                });
            }

            // 3) 유저 보유량 조회 (VirtualCurrency)
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<GetFarmPopupRequest>(body)
                       ?? throw new Exception("Invalid request");
            var invRes = await PlayFabServerAPI.GetUserInventoryAsync(
                new GetUserInventoryRequest { PlayFabId = data.PlayFabId });
            var vc = invRes.Error == null
                     ? invRes.Result.VirtualCurrency
                     : new Dictionary<string, int>();

            // 4) SeedData 에 Count 채워주기
            foreach (var seed in list!)
            {
                if (seed.Id != null && vc.TryGetValue(seed.Id, out var count))
                    seed.Count = count;
                else
                    seed.Count = 0;
            }

            // 5) 응답
            return new OkObjectResult(new SeedsResponse
            {
                IsSuccess = true,
                SeedsList = list
            });
        }
    }
}
