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
            var tdReq = new GetTitleDataRequest { Keys = new List<string> { "SeedsList" } };
            var tdRes = await PlayFabServerAPI.GetTitleDataAsync(tdReq);

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

            // 3) 응답
            return new OkObjectResult(new SeedsResponse
            {
                IsSuccess = true,
                SeedsList = list
            });
        }
    }
}
