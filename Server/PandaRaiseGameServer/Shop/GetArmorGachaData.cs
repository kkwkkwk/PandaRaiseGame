using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;   // GetTitleDataRequest
using CommonLibrary;

namespace Shop
{
    public class GetArmorGachaData
    {
        private readonly ILogger<GetArmorGachaData> _logger;

        public GetArmorGachaData(ILogger<GetArmorGachaData> logger)
        {
            _logger = logger;
        }

        [Function("GetArmorGachaData")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("[GetArmorGachaData] Invocation started at {Time}", DateTime.UtcNow);

            try
            {
                // 1) 환경 변수 로드
                PlayFabConfig.Configure();

                // 2) TitleData 요청
                const string key = "ArmorGachaShop";
                _logger.LogInformation("Requesting TitleData for key '{Key}'", key);

                var tdReq = new GetTitleDataRequest { Keys = new List<string> { key } };
                var tdResult = await PlayFabServerAPI.GetTitleDataAsync(tdReq);

                if (tdResult.Error != null)
                {
                    _logger.LogError("[GetArmorGachaData] PlayFab API error");
                    return new OkObjectResult(new ArmorGachaResponseData
                    {
                        IsSuccess = false,
                        ErrorMessage = "PlayFab TitleData 조회 실패"
                    });
                }

                var data = tdResult.Result.Data;
                _logger.LogInformation("PlayFab returned {Count} TitleData entries", data.Count);

                if (!data.TryGetValue(key, out var json))
                {
                    _logger.LogWarning("[GetArmorGachaData] Key '{Key}' not found. Available keys: {Keys}",
                        key, string.Join(", ", data.Keys));
                    return new OkObjectResult(new ArmorGachaResponseData
                    {
                        IsSuccess = false,
                        ErrorMessage = "상점 데이터가 없습니다"
                    });
                }

                _logger.LogInformation("Raw JSON length: {Length}", json?.Length ?? 0);
                if (!string.IsNullOrEmpty(json))
                {
                    var preview = json.Length > 200 ? json.Substring(0, 200) + "…" : json;
                    _logger.LogDebug("JSON preview: {Preview}", preview);
                }

                // 3) JSON 파싱
                List<ArmorGachaItemData> list;
                try
                {
                    list = JsonConvert.DeserializeObject<List<ArmorGachaItemData>>(json)
                           ?? new List<ArmorGachaItemData>();
                    _logger.LogInformation("Parsed {Count} items from JSON", list.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[GetArmorGachaData] JSON 파싱 중 예외 발생");
                    return new OkObjectResult(new ArmorGachaResponseData
                    {
                        IsSuccess = false,
                        ErrorMessage = "데이터 형식 오류"
                    });
                }

                // 4) 응답 준비
                var resp = new ArmorGachaResponseData
                {
                    IsSuccess = true,
                    ErrorMessage = null,
                    ArmorItemList = list
                };
                _logger.LogInformation("Returning successful response with {Count} items", list.Count);

                return new OkObjectResult(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetArmorGachaData] 처리 중 예기치 않은 예외 발생");
                return new ObjectResult("서버 처리 중 오류가 발생했습니다")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
            finally
            {
                _logger.LogInformation("[GetArmorGachaData] Invocation ended at {Time}", DateTime.UtcNow);
            }
        }
    }
}
