using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class BuyCurrencyResponseData
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 구매된(획득된) 재화 항목 리스트  
    /// 네 가지 중 하나만 채워져 온다고 가정
    /// </summary>
    [JsonProperty("ownedItemList")]
    public List<OwnedCurrencyData> OwnedItemList { get; set; }
}

[Serializable]
public class OwnedCurrencyData

{
    //뭐들어왔는지 빠른 식별용
    [JsonProperty("itemType")]
    public string ItemType { get; set; }   // "Diamond", "Free", "Japhwa", "Mileage"

    [JsonProperty("diamondItemData", NullValueHandling = NullValueHandling.Ignore)]
    public DiamondItemData DiamondItemData { get; set; }

    [JsonProperty("freeItemData", NullValueHandling = NullValueHandling.Ignore)]
    public FreeItemData FreeItemData { get; set; }

    [JsonProperty("japhwaItemData", NullValueHandling = NullValueHandling.Ignore)]
    public JaphwaItemData JaphwaItemData { get; set; }

    [JsonProperty("mileageItemData", NullValueHandling = NullValueHandling.Ignore)]
    public MileageItemData MileageItemData { get; set; }
}
