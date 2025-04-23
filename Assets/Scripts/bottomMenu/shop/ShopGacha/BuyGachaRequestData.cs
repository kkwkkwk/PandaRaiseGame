using System;
using Newtonsoft.Json;

/// <summary>
/// 가챠(무기‧방어구‧스킬) 구매 요청 DTO  
/// ▸ 세 가지 ItemData 중 “딱 하나”만 채워서 전송하세요.
/// </summary>
[Serializable]
public class BuyGachaRequestData
{
    [JsonProperty("playFabId")]
    public string PlayFabId { get; set; }

    [JsonProperty("currencyType")]
    public string CurrencyType { get; set; }   // "WON", "DIAMOND", "GC" …

    [JsonProperty("itemType")]
    public string ItemType { get; set; }      // "Weapon", "Armor", "Skill"

    // ItemType 이 "Weapon" 이면 WeaponItemData 만 채우고, 나머지 두 필드는 null 로 두면됨. (서버에서 NullValue 무시)
    [JsonProperty("weaponItemData", NullValueHandling = NullValueHandling.Ignore)]
    public WeaponItemData WeaponItemData { get; set; }

    [JsonProperty("armorItemData", NullValueHandling = NullValueHandling.Ignore)]
    public ArmorItemData ArmorItemData { get; set; }

    [JsonProperty("skillItemData", NullValueHandling = NullValueHandling.Ignore)]
    public SkillItemData SkillItemData { get; set; }
}
