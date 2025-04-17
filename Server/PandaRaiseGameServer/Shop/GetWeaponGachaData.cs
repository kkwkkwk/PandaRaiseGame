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
    public class GetWeaponGachaData
    {
        private readonly ILogger<GetWeaponGachaData> _logger;

        public GetWeaponGachaData(ILogger<GetWeaponGachaData> logger)
        {
            _logger = logger;
        }

        [Function("GetWeaponData")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest req)
        {
            _logger.LogInformation("[GetWeaponData] Function invoked");

            // 1. PlayFab 설정 (환경 변수로 보관)
            PlayFabSettings.staticSettings.TitleId =
                Environment.GetEnvironmentVariable("PLAYFAB_TITLE_ID");
            PlayFabSettings.staticSettings.DeveloperSecretKey =
                Environment.GetEnvironmentVariable("PLAYFAB_SECRET_KEY");

            // 2. TitleData (WeaponGachaShop) 조회
            var tdReq = new GetTitleDataRequest
            {
                Keys = new List<string> { "WeaponGachaShop" }
            };

            var tdResult = await PlayFabServerAPI.GetTitleDataAsync(tdReq);
            if (tdResult.Error != null)
            {
                _logger.LogError("[GetWeaponData] PlayFab error: {0}",
                                 tdResult.Error.GenerateErrorReport());
                return new OkObjectResult(new WeaponGachaResponseData
                {
                    IsSuccess = false,
                    ErrorMessage = "PlayFab TitleData 조회 실패"
                });
            }

            if (!tdResult.Result.Data.TryGetValue("WeaponGachaShop", out var json))
            {
                _logger.LogWarning("[GetWeaponData] WeaponGachaShop key not found");
                return new OkObjectResult(new WeaponGachaResponseData
                {
                    IsSuccess = false,
                    ErrorMessage = "상점 데이터가 없습니다"
                });
            }

            // 3. JSON 데이터를 DTO(List)로 변환
            List<WeaponGachaItemData>? list;
            try
            {
                list = JsonConvert.DeserializeObject<List<WeaponGachaItemData>>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetWeaponData] JSON 파싱 오류");
                return new OkObjectResult(new WeaponGachaResponseData
                {
                    IsSuccess = false,
                    ErrorMessage = "데이터 형식 오류"
                });
            }

            // 4. 응답 전송
            var resp = new WeaponGachaResponseData
            {
                IsSuccess = true,
                ErrorMessage = null,
                WeaponItemList = list
            };

            return new OkObjectResult(resp);
        }
    }
}
