using System;
using Newtonsoft.Json;

[Serializable]
public class BuyCurrencyRequestData
{
    [JsonProperty("playFabId")]
    public string PlayFabId { get; set; }

    [JsonProperty("currencyType")]
    public string CurrencyType { get; set; }   // "WON", "DIAMOND", "GC", "MILEAGE" 등

    [JsonProperty("goodsType")]
    public string GoodsType { get; set; }  // "상품 종류"

    [JsonProperty("itemType")]
    public string ItemType { get; set; }       // "Diamond", "Free", "Japhwa", "Mileage"


    //얘도 가챠처럼 (ItemType 이 "Weapon" 이면 WeaponItemData 만 채우고, 나머지 두 필드는 null 로 두면됨. (서버에서 NullValue 무시) >> 이런식으로)
    [JsonProperty("diamondItemData", NullValueHandling = NullValueHandling.Ignore)]
    public DiamondItemData DiamondItemData { get; set; }

    [JsonProperty("freeItemData", NullValueHandling = NullValueHandling.Ignore)]
    public FreeItemData FreeItemData { get; set; }

    [JsonProperty("japhwaItemData", NullValueHandling = NullValueHandling.Ignore)]
    public JaphwaItemData JaphwaItemData { get; set; }

    [JsonProperty("mileageItemData", NullValueHandling = NullValueHandling.Ignore)]
    public MileageItemData MileageItemData { get; set; }
}
