using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class JaphaResponseData
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }

    [JsonProperty("japhaItemList")]
    public List<JaphaItemData> JaphaItemList { get; set; }
}

[Serializable]
public class JaphaItemData
{
    [JsonProperty("itemName")]
    public string ItemName { get; set; }        // 예: "던전 티켓 A", "농장 티켓 B"

    [JsonProperty("price")]
    public int Price { get; set; }              // 예: 300, 1200 등

    [JsonProperty("currencyType")]
    public string CurrencyType { get; set; }     // "FREE", "DIAMOND", "GC", "WON" 등

    [JsonProperty("header")]
    public string Header { get; set; }          // "던전티켓", "농장티켓" 등 (헤더 구분)

}
