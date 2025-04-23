using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class WeaponResponseData
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }

    [JsonProperty("weaponItemList")]
    public List<WeaponItemData> WeaponItemList { get; set; }
}

[Serializable]
public class WeaponItemData
{
    [JsonProperty("itemName")]
    public string ItemName { get; set; }     // 예: "무기 아이템 A"

    [JsonProperty("price")]
    public int Price { get; set; }           // 예: 1000, 1200원 등

    [JsonProperty("currencyType")]
    public string CurrencyType { get; set; } // "FREE", "DIAMOND", "GC", "WON" 등

    [JsonProperty("header")]
    public string Header { get; set; }       // 예: "일일 광고", "뽑기", etc. (UI 상의 타이틀/헤더 구분)
}
