using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class FreeResponseData
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }

    [JsonProperty("freeItemList")]
    public List<FreeItemData> FreeItemList { get; set; }
}

[Serializable]
public class FreeItemData
{
    [JsonProperty("itemName")]
    public string ItemName { get; set; }   // 예: 무료 어쩌구...?

    [JsonProperty("price")]
    public int Price { get; set; }         // 가격 실제로는 0원일 수도 있지만 서버 구조에 맞춰 필드만 유지

    [JsonProperty("currencyType")]
    public string CurrencyType { get; set; }  // "FREE"

    [JsonProperty("header")]
    public string Header { get; set; }       // UI 헤더에 해당
}
