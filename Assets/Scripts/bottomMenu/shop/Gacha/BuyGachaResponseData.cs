using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class BuyGachaResponseData
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }                     // 구매/뽑기 성공 여부

    [JsonProperty("ownedItemList")]
    public List<OwnedItemData> OwnedItemList { get; set; } // 뽑힌(획득된) 아이템 목록
}

[Serializable]
public class OwnedItemData
{
    [JsonProperty("itemName")]
    public string ItemName { get; set; }    // 예: "강철 검"

    [JsonProperty("itemType")]
    public string ItemType { get; set; }    // 예: "Weapon", "Armor", "Skill"
}
