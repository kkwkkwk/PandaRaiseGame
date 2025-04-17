using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;   // GetTitleDataRequest

namespace Shop
{
    public class GetArmorData
    {
        private readonly ILogger<GetArmorData> _logger;

        public GetArmorData(ILogger<GetArmorData> logger)
        {
            _logger = logger;
        }

        [Function("GetArmorData")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest req)
        {
            _logger.LogInformation("[GetArmorData] Function invoked");

            // ─────────────────────────────────────────────────────────────
            // 1. PlayFab 설정 (환경 변수로 보관해 두는 것이 안전)
            // ─────────────────────────────────────────────────────────────
            PlayFabSettings.staticSettings.TitleId =
                Environment.GetEnvironmentVariable("PLAYFAB_TITLE_ID");
            PlayFabSettings.staticSettings.DeveloperSecretKey =
                Environment.GetEnvironmentVariable("PLAYFAB_SECRET_KEY");

            // ─────────────────────────────────────────────────────────────
            // 2. TitleData → ArmorGachaShop JSON 가져오기
            // ─────────────────────────────────────────────────────────────
            var tdReq = new GetTitleDataRequest
            {
                Keys = new List<string> { "ArmorGachaShop" }
            };

            var tdResult = await PlayFabServerAPI.GetTitleDataAsync(tdReq);
            if (tdResult.Error != null)
            {
                _logger.LogError("[GetArmorData] PlayFab error: {0}",
                                 tdResult.Error.GenerateErrorReport());
                return new OkObjectResult(new ArmorGachaResponseData
                {
                    IsSuccess = false,
                    ErrorMessage = "PlayFab TitleData 조회 실패"
                });
            }

            if (!tdResult.Result.Data.TryGetValue("ArmorGachaShop", out var json))
            {
                _logger.LogWarning("[GetArmorData] ArmorGachaShop key not found");
                return new OkObjectResult(new ArmorGachaResponseData
                {
                    IsSuccess = false,
                    ErrorMessage = "상점 데이터가 없습니다"
                });
            }

            // ─────────────────────────────────────────────────────────────
            // 3. JSON → DTO 변환
            // ─────────────────────────────────────────────────────────────
            List<ArmorGachaItemData>? list;
            try
            {
                list = JsonConvert.DeserializeObject<List<ArmorGachaItemData>>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetArmorData] JSON 파싱 오류");
                return new OkObjectResult(new ArmorGachaResponseData
                {
                    IsSuccess = false,
                    ErrorMessage = "데이터 형식 오류"
                });
            }

            // ─────────────────────────────────────────────────────────────
            // 4. 프론트 형식으로 응답
            // ─────────────────────────────────────────────────────────────
            var resp = new ArmorGachaResponseData
            {
                IsSuccess = true,
                ErrorMessage = null,
                ArmorItemList = list
            };

            return new OkObjectResult(resp);
        }
    }
}
