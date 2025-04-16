using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class ArmorResponseData
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }

    [JsonProperty("armorItemList")]
    public List<ArmorItemData> ArmorItemList { get; set; }
}

[Serializable]
public class ArmorItemData
{
    [JsonProperty("itemName")]
    public string ItemName { get; set; }      // 예: "방어구 아이템 A"

    [JsonProperty("price")]
    public int Price { get; set; }           // 가격(결제수단과 함께 사용)

    [JsonProperty("currencyType")]
    public string CurrencyType { get; set; } // "FREE", "DIAMOND", "GC", "WON" 등

    [JsonProperty("header")]
    public string Header { get; set; }       // 예: "뽑기", "일일 광고" 등, UI 헤더에 해당
}
