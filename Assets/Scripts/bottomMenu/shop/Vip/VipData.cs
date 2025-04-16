using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class VipResponseData
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }

    [JsonProperty("vipItemList")]
    public List<VipItemData> VipItemList { get; set; }
}

[Serializable]
public class VipItemData
{
    [JsonProperty("title")]
    public string Title { get; set; }        // VIP 패키지명 (예: "VIP 전용 패키지")

    [JsonProperty("price")]
    public int Price { get; set; }           // 실제 가격 (예: 1200)

    [JsonProperty("currencyType")]
    public string CurrencyType { get; set; } // 예: "FREE", "WON", etc.
}
