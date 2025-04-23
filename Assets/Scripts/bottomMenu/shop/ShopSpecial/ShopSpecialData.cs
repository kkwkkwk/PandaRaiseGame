using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class SpecialResponseData
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }

    // 스페셜 아이템 목록
    [JsonProperty("specialItemList")]
    public List<SpecialItemData> SpecialItemList { get; set; }
}

[Serializable]
public class SpecialItemData
{
    // UI에는 제목만 표시할 예정
    [JsonProperty("title")]
    public string Title { get; set; }

    // UI에는 가격만 표시 (예: 1200원)
    [JsonProperty("price")]
    public int Price { get; set; }

    [JsonProperty("currencyType")]
    public string CurrencyType { get; set; }  // "FREE"

}
