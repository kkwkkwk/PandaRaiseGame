using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
    public class BuyCurrencyItem
    {
        private readonly ILogger<BuyCurrencyItem> _logger;
        public BuyCurrencyItem(ILogger<BuyCurrencyItem> logger) => _logger = logger;

        [Function("BuyCurrencyItem")]
        public async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "BuyCurrencyItem")] HttpRequest req)
        {
            // 1) PlayFab 세팅
            PlayFabConfig.Configure();

            // 2) 요청 파싱
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation("[BuyCurrencyItem] Request Body: {0}", body);
            var data = JsonConvert.DeserializeObject<BuyCurrencyRequestData>(body)
                       ?? throw new Exception("Invalid request");

            // 3) 문자열에서 수량 파싱 (e.g. "던전 티켓 10개" → 10)
            var match = Regex.Match(data.ItemName!, @"\d+");
            int quantity = match.Success ? int.Parse(match.Value) : 1;

            // 4) 재화 차감 (FREE 면 차감 스킵)
            if (!data.CurrencyType!.Equals("FREE", StringComparison.OrdinalIgnoreCase) && !data.CurrencyType.Equals("WON", StringComparison.OrdinalIgnoreCase))
            {
                var subRes = await PlayFabServerAPI.SubtractUserVirtualCurrencyAsync(
                    new SubtractUserVirtualCurrencyRequest
                    {
                        PlayFabId = data.PlayFabId,
                        VirtualCurrency = data.CurrencyType,
                        Amount = data.Price
                    });
                if (subRes.Error != null)
                    return new OkObjectResult(new BuyCurrencyResponseData
                    {
                        IsSuccess = false,
                        ErrorMessage = "재화 차감 실패"
                    });
            }

            // 5) 지급 재화 추가
            var addRes = await PlayFabServerAPI.AddUserVirtualCurrencyAsync(
                new AddUserVirtualCurrencyRequest
                {
                    PlayFabId = data.PlayFabId,
                    VirtualCurrency = data.GoodsType,
                    Amount = quantity
                });
            if (addRes.Error != null)
                return new OkObjectResult(new BuyCurrencyResponseData
                {
                    IsSuccess = false,
                    ErrorMessage = "재화 지급 실패"
                });

            // 6) 최종 응답
            return new OkObjectResult(new BuyCurrencyResponseData { IsSuccess = true });
        }
    }
}
