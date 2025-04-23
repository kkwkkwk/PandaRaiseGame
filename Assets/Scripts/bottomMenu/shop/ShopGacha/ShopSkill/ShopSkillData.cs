using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class SkillResponseData
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }

    [JsonProperty("skillItemList")]
    public List<SkillItemData> SkillItemList { get; set; }
}

[Serializable]
public class SkillItemData
{
    [JsonProperty("itemName")]
    public string ItemName { get; set; }      // 예: "스킬 카드 A"

    [JsonProperty("price")]
    public int Price { get; set; }           // 예: 500

    [JsonProperty("currencyType")]
    public string CurrencyType { get; set; } // "FREE", "DIAMOND", "GC", "WON" 등

    [JsonProperty("header")]
    public string Header { get; set; }       // 예: "초급 스킬", "고급 스킬" 등
}
