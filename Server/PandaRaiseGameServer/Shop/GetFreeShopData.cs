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
using CommonLibrary;

namespace Shop
{
    public class GetFreeShopData
    {
        private readonly ILogger<GetFreeShopData> _logger;
        public GetFreeShopData(ILogger<GetFreeShopData> logger) => _logger = logger;

        [Function("GetFreeShopData")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest req)
        {
            _logger.LogInformation("[GetFreeShopData] invoked");

            // PlayFab 설정
            PlayFabConfig.Configure();

            // TitleData 조회
            var tdReq = new GetTitleDataRequest { Keys = new List<string> { "FreeShop" } };
            var tdRes = await PlayFabServerAPI.GetTitleDataAsync(tdReq);

            if (tdRes.Error != null)
            {
                _logger.LogError(tdRes.Error.GenerateErrorReport());
                return new OkObjectResult(new FreeResponseData
                {
                    IsSuccess = false,
                    ErrorMessage = "FreeShop 데이터 조회 실패"
                });
            }
            if (!tdRes.Result.Data.TryGetValue("FreeShop", out var json))
            {
                return new OkObjectResult(new FreeResponseData
                {
                    IsSuccess = false,
                    ErrorMessage = "FreeShop 데이터가 없습니다"
                });
            }

            List<FreeItemData>? list;
            try
            {
                list = JsonConvert.DeserializeObject<List<FreeItemData>>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetFreeShopData] JSON 파싱 오류");
                return new OkObjectResult(new FreeResponseData
                {
                    IsSuccess = false,
                    ErrorMessage = "데이터 형식 오류"
                });
            }

            return new OkObjectResult(new FreeResponseData
            {
                IsSuccess = true,
                ErrorMessage = null,
                FreeItemList = list
            });
        }
    }
}
