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
    public class GetMileageShopData
    {
        private readonly ILogger<GetMileageShopData> _logger;
        public GetMileageShopData(ILogger<GetMileageShopData> logger) => _logger = logger;

        [Function("GetMileageShopData")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest req)
        {
            _logger.LogInformation("[GetMileageShopData] invoked");

            // PlayFab 설정
            PlayFabConfig.Configure();

            // TitleData 조회
            var tdReq = new GetTitleDataRequest { Keys = new List<string> { "MileageShop" } };
            var tdRes = await PlayFabServerAPI.GetTitleDataAsync(tdReq);

            if (tdRes.Error != null)
            {
                _logger.LogError(tdRes.Error.GenerateErrorReport());
                return new OkObjectResult(new MileageResponseData
                {
                    IsSuccess = false,
                    ErrorMessage = "MileageShop 데이터 조회 실패"
                });
            }
            if (!tdRes.Result.Data.TryGetValue("MileageShop", out var json))
            {
                return new OkObjectResult(new MileageResponseData
                {
                    IsSuccess = false,
                    ErrorMessage = "MileageShop 데이터가 없습니다"
                });
            }

            List<MileageItemData>? list;
            try
            {
                list = JsonConvert.DeserializeObject<List<MileageItemData>>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetMileageShopData] JSON 파싱 오류");
                return new OkObjectResult(new MileageResponseData
                {
                    IsSuccess = false,
                    ErrorMessage = "데이터 형식 오류"
                });
            }

            return new OkObjectResult(new MileageResponseData
            {
                IsSuccess = true,
                ErrorMessage = null,
                MileageItemList = list
            });
        }
    }
}
