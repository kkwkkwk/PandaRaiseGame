using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;

namespace Shop
{
    public class GetDiamondShopData
    {
        private readonly ILogger<GetDiamondShopData> _logger;

        public GetDiamondShopData(ILogger<GetDiamondShopData> logger)
        {
            _logger = logger;
        }

        [Function("GetDiamondData")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest req)
        {
            _logger.LogInformation("[GetDiamondData] Function invoked");

            // 1) PlayFab 설정 (환경 변수로 보관)
            PlayFabSettings.staticSettings.TitleId =
                Environment.GetEnvironmentVariable("PLAYFAB_TITLE_ID");
            PlayFabSettings.staticSettings.DeveloperSecretKey =
                Environment.GetEnvironmentVariable("PLAYFAB_SECRET_KEY");

            // 2) TitleData 조회: DiamondShop
            var tdReq = new GetTitleDataRequest
            {
                Keys = new List<string> { "DiamondShop" }
            };
            var tdResult = await PlayFabServerAPI.GetTitleDataAsync(tdReq);

            if (tdResult.Error != null)
            {
                _logger.LogError("[GetDiamondData] PlayFab error: {0}",
                                 tdResult.Error.GenerateErrorReport());
                return new OkObjectResult(new DiamondResponseData
                {
                    IsSuccess = false,
                    ErrorMessage = "PlayFab TitleData 조회 실패"
                });
            }

            if (!tdResult.Result.Data.TryGetValue("DiamondShop", out var json))
            {
                _logger.LogWarning("[GetDiamondData] DiamondShop key not found");
                return new OkObjectResult(new DiamondResponseData
                {
                    IsSuccess = false,
                    ErrorMessage = "상품 데이터가 없습니다"
                });
            }

            // 3) JSON → DTO List 변환
            List<DiamondItemData>? list;
            try
            {
                list = JsonConvert.DeserializeObject<List<DiamondItemData>>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetDiamondData] JSON 파싱 오류");
                return new OkObjectResult(new DiamondResponseData
                {
                    IsSuccess = false,
                    ErrorMessage = "데이터 형식 오류"
                });
            }

            // 4) 응답 구성
            var resp = new DiamondResponseData
            {
                IsSuccess = true,
                ErrorMessage = null,
                DiamondItemList = list
            };

            return new OkObjectResult(resp);
        }
    }
}
