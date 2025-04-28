using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class MileageResponseData
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }

    [JsonProperty("mileageItemList")]
    public List<MileageItemData> MileageItemList { get; set; }
}

[Serializable]
public class MileageItemData
{
    [JsonProperty("itemName")]
    public string ItemName { get; set; }      // 예: "마일리지 상품 A"

    [JsonProperty("price")]
    public int Price { get; set; }            // 예: 300

    [JsonProperty("currencyType")]
    public string CurrencyType { get; set; }  // 예: "FREE", "MILEAGE", "WON" 등

    [JsonProperty("goodsType")]
    public string GoodsType { get; set; }  // "상품 종류"

    [JsonProperty("header")]
    public string Header { get; set; }       // UI 헤더에 해당
}
