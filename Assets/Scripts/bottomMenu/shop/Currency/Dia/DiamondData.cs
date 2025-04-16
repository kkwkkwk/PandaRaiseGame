using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class DiamondResponseData
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }

    [JsonProperty("diamondItemList")]
    public List<DiamondItemData> DiamondItemList { get; set; }
}

[Serializable]
public class DiamondItemData
{
    [JsonProperty("itemName")]
    public string ItemName { get; set; }      // 예: "다이아 300개"

    [JsonProperty("price")]
    public int Price { get; set; }            // 실제 결제 금액 (현금결제는 금액 앞에 기호 붙여주세요 ￦ 1200 )

    [JsonProperty("currencyType")]
    public string CurrencyType { get; set; }  // "Won"

    [JsonProperty("header")]
    public string Header { get; set; }          // 헤더 UI
}
