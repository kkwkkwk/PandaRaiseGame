using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommonLibrary;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using static Shop.BuyCurrencyRequestData;

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
            PlayFabConfig.Configure();

            // 1) 요청 바디 파싱
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation("[BuyCurrencyItem] Request Body: {0}", body);
            var data = JsonConvert.DeserializeObject<BuyCurrencyRequestData>(body)
                       ?? throw new Exception("Invalid request");

            // 2) nested DTO 에서 price, itemName 추출 → 수량 파싱
            int price;
            string itemName;
            switch (data.ItemType)
            {
                case "Diamond":
                    price = data.DiamondItemData!.Price;
                    itemName = data.DiamondItemData.ItemName!;
                    break;
                case "Free":
                    price = data.FreeItemData!.Price;
                    itemName = data.FreeItemData.ItemName!;
                    break;
                case "Japhwa":
                    price = data.JaphwaItemData!.Price;
                    itemName = data.JaphwaItemData.ItemName!;
                    break;
                case "Mileage":
                    price = data.MileageItemData!.Price;
                    itemName = data.MileageItemData.ItemName!;
                    break;
                default:
                    return new BadRequestObjectResult(new BuyCurrencyResponseData
                    {
                        IsSuccess = false,
                        OwnedItemList = null
                    });
            }

            var match = Regex.Match(itemName, @"\d+");
            int quantity = match.Success ? int.Parse(match.Value) : 1;

            // 3) 재화 차감 (FREE, WON 은 차감 스킵)
            if (!data.CurrencyType!.Equals("FREE", StringComparison.OrdinalIgnoreCase)
             && !data.CurrencyType.Equals("WON", StringComparison.OrdinalIgnoreCase))
            {
                var subRes = await PlayFabServerAPI.SubtractUserVirtualCurrencyAsync(
                    new SubtractUserVirtualCurrencyRequest
                    {
                        PlayFabId = data.PlayFabId,
                        VirtualCurrency = data.CurrencyType,
                        Amount = price
                    });
                if (subRes.Error != null)
                    return new OkObjectResult(new BuyCurrencyResponseData
                    {
                        IsSuccess = false,
                        OwnedItemList = null
                    });
            }

            // 4) 지급 재화 추가
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
                    OwnedItemList = null
                });

            // 5) OwnedCurrencyData 생성 → nested DTO 그대로 복사
            var owned = new OwnedCurrencyData
            {
                ItemType = data.ItemType
            };
            switch (data.ItemType)
            {
                case "Diamond":
                    owned.DiamondItemData = data.DiamondItemData;
                    break;
                case "Free":
                    owned.FreeItemData = data.FreeItemData;
                    break;
                case "Japhwa":
                    owned.JaphwaItemData = data.JaphwaItemData;
                    break;
                case "Mileage":
                    owned.MileageItemData = data.MileageItemData;
                    break;
            }

            // 6) 최종 응답
            var response = new BuyCurrencyResponseData
            {
                IsSuccess = true,
                OwnedItemList = new List<OwnedCurrencyData> { owned }
            };
            return new OkObjectResult(response);
        }
    }
}
