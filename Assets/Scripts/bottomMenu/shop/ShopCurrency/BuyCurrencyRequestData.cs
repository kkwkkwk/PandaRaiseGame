using System;
using Newtonsoft.Json;

[Serializable]
public class BuyCurrencyRequestData
{
    [JsonProperty("playFabId")]
    public string PlayFabId { get; set; }     

    [JsonProperty("itemName")]
    public string ItemName { get; set; }       // 예: "다이아 1개" 

    [JsonProperty("currencyType")]
    public string CurrencyType { get; set; }   // 예: "WON", "DIAMOND", "GC", "MILEAGE"

    [JsonProperty("itemType")]
    public string ItemType { get; set; }       // 예: "Free,Diamond..."
}
