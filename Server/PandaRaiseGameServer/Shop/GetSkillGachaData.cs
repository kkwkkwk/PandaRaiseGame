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
    public class GetSkillGachaData
    {
        private readonly ILogger<GetSkillGachaData> _logger;

        public GetSkillGachaData(ILogger<GetSkillGachaData> logger)
        {
            _logger = logger;
        }

        [Function("GetSkillGachaData")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest req)
        {
            _logger.LogInformation("[GetSkillData] Function invoked");

            // 1. PlayFab 설정 (환경 변수 사용)
            PlayFabConfig.Configure();

            // 2. TitleData (SkillGachaShop) 조회
            var tdReq = new GetTitleDataRequest
            {
                Keys = new List<string> { "SkillGachaShop" }
            };

            var tdResult = await PlayFabServerAPI.GetTitleDataAsync(tdReq);
            if (tdResult.Error != null)
            {
                _logger.LogError("[GetSkillData] PlayFab error: {0}",
                                 tdResult.Error.GenerateErrorReport());
                return new OkObjectResult(new SkillGachaResponseData
                {
                    IsSuccess = false,
                    ErrorMessage = "PlayFab TitleData 조회 실패"
                });
            }

            if (!tdResult.Result.Data.TryGetValue("SkillGachaShop", out var json))
            {
                _logger.LogWarning("[GetSkillData] SkillGachaShop key not found");
                return new OkObjectResult(new SkillGachaResponseData
                {
                    IsSuccess = false,
                    ErrorMessage = "상점 데이터가 없습니다"
                });
            }

            // 3. JSON → DTO 변환
            List<SkillGachaItemData>? list;
            try
            {
                list = JsonConvert.DeserializeObject<List<SkillGachaItemData>>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetSkillData] JSON 파싱 오류");
                return new OkObjectResult(new SkillGachaResponseData
                {
                    IsSuccess = false,
                    ErrorMessage = "데이터 형식 오류"
                });
            }

            // 4. 응답 객체 구성 및 전송
            var resp = new SkillGachaResponseData
            {
                IsSuccess = true,
                ErrorMessage = null,
                SkillItemList = list
            };

            return new OkObjectResult(resp);
        }
    }
}
