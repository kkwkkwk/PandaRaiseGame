using System;
using Newtonsoft.Json;

[Serializable]
public class BuyGachaRequestData
{
    [JsonProperty("playFabId")]
    public string PlayFabId { get; set; }      

    [JsonProperty("itemName")]
    public string ItemName { get; set; }       // 뽑기 종류를 식별하도록

    [JsonProperty("currencyType")]
    public string CurrencyType { get; set; }   // 예: "WON", "DIAMOND", "GC", "MILEAGE", "FREE"

    [JsonProperty("itemType")]
    public string ItemType { get; set; }       // 예: "Weapon", "Armor", "Skill"
}
