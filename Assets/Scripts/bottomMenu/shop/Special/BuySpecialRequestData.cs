using System;
using Newtonsoft.Json;

[Serializable]
public class BuySpecialRequestData
{
    [JsonProperty("playFabId")]
    public string PlayFabId { get; set; }

    [JsonProperty("itemTitle")]
    public string ItemTitle { get; set; }  // 예: "VIP 패키지"
}
