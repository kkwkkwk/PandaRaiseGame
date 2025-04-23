using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class BuyGachaResponseData
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 뽑힌(획득된) 아이템 목록  
    /// - Weapon/Armor/Skill 중 ‘하나’ 필드만 채워져 온다고 가정
    /// </summary>
    [JsonProperty("ownedItemList")]
    public List<OwnedItemData> OwnedItemList { get; set; }
}

[Serializable]
public class OwnedItemData
{
    // 무엇이 들어왔는지 빠르게 식별하기 위한 용도
    [JsonProperty("itemType")]
    public string ItemType { get; set; }   // "Weapon", "Armor", "Skill"

    // 아래 세 필드 중 하나만 채워지면 됨
    [JsonProperty("weaponItemData", NullValueHandling = NullValueHandling.Ignore)]
    public WeaponItemData WeaponItemData { get; set; }

    [JsonProperty("armorItemData", NullValueHandling = NullValueHandling.Ignore)]
    public ArmorItemData ArmorItemData { get; set; }

    [JsonProperty("skillItemData", NullValueHandling = NullValueHandling.Ignore)]
    public SkillItemData SkillItemData { get; set; }
}
