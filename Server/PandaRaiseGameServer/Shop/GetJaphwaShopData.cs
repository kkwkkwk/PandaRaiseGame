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
    public class GetJaphwaShopData
    {
        private readonly ILogger<GetJaphwaShopData> _logger;
        public GetJaphwaShopData(ILogger<GetJaphwaShopData> logger) => _logger = logger;

        [Function("GetJaphwaShopData")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest req)
        {
            _logger.LogInformation("[GetJaphwaShopData] invoked");

            // 1) PlayFab 설정
            PlayFabSettings.staticSettings.TitleId =
                Environment.GetEnvironmentVariable("PLAYFAB_TITLE_ID");
            PlayFabSettings.staticSettings.DeveloperSecretKey =
                Environment.GetEnvironmentVariable("PLAYFAB_SECRET_KEY");

            // 2) TitleData 조회 (JaphwaShop)
            var tdReq = new GetTitleDataRequest { Keys = new List<string> { "JaphwaShop" } };
            var tdRes = await PlayFabServerAPI.GetTitleDataAsync(tdReq);

            if (tdRes.Error != null)
            {
                _logger.LogError(tdRes.Error.GenerateErrorReport());
                return new OkObjectResult(new JaphwaResponseData
                {
                    IsSuccess = false,
                    ErrorMessage = "JaphwaShop 데이터 조회 실패"
                });
            }
            if (!tdRes.Result.Data.TryGetValue("JaphwaShop", out var json))
            {
                return new OkObjectResult(new JaphwaResponseData
                {
                    IsSuccess = false,
                    ErrorMessage = "상품 데이터가 없습니다"
                });
            }

            // 3) JSON → DTO 변환
            List<JaphwaItemData>? list;
            try
            {
                list = JsonConvert.DeserializeObject<List<JaphwaItemData>>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetJaphwaShopData] JSON 파싱 오류");
                return new OkObjectResult(new JaphwaResponseData
                {
                    IsSuccess = false,
                    ErrorMessage = "데이터 형식 오류"
                });
            }

            // 4) 응답 구성
            var resp = new JaphwaResponseData
            {
                IsSuccess = true,
                ErrorMessage = null,
                JaphwaItemList = list
            };
            return new OkObjectResult(resp);
        }
    }
}
