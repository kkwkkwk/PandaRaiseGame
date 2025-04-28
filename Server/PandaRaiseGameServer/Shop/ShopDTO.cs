using Newtonsoft.Json;

namespace Shop
{
    #region GachaShop
    public class WeaponGachaResponseData
    {
        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonProperty("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonProperty("weaponItemList")]
        public List<WeaponGachaItemData>? WeaponItemList { get; set; }
    }

    public class WeaponGachaItemData
    {
        [JsonProperty("itemName")]
        public string? ItemName { get; set; }

        [JsonProperty("price")]
        public int Price { get; set; }

        [JsonProperty("currencyType")]
        public string? CurrencyType { get; set; }

        [JsonProperty("header")]
        public string? Header { get; set; }
    }

    public class ArmorGachaResponseData
    {
        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonProperty("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonProperty("armorItemList")]
        public List<ArmorGachaItemData>? ArmorItemList { get; set; }
    }

    public class ArmorGachaItemData
    {
        [JsonProperty("itemName")]
        public string? ItemName { get; set; }

        [JsonProperty("price")]
        public int Price { get; set; }

        [JsonProperty("currencyType")]
        public string? CurrencyType { get; set; }

        [JsonProperty("header")]
        public string? Header { get; set; }
    }

    public class SkillGachaResponseData
    {
        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonProperty("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonProperty("skillItemList")]
        public List<SkillGachaItemData>? SkillItemList { get; set; }
    }

    public class SkillGachaItemData
    {
        [JsonProperty("itemName")]
        public string? ItemName { get; set; }

        [JsonProperty("price")]
        public int Price { get; set; }

        [JsonProperty("currencyType")]
        public string? CurrencyType { get; set; }

        [JsonProperty("header")]
        public string? Header { get; set; }
    }
    #endregion
    #region CurrencyShop
    // ─────────────────────────────────────────────────────────────
    // 프론트와 동일한 DTO 정의 (중복 방지를 위해 공용 라이브러리로 관리해도 OK)
    // ─────────────────────────────────────────────────────────────
    public class FreeResponseData
    {
        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonProperty("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonProperty("freeItemList")]
        public List<FreeItemData>? FreeItemList { get; set; }
    }
    public class FreeItemData
    {
        [JsonProperty("itemName")]
        public string? ItemName { get; set; }

        [JsonProperty("price")]
        public int Price { get; set; }

        [JsonProperty("currencyType")]
        public string? CurrencyType { get; set; }

        [JsonProperty("header")]
        public string? Header { get; set; }
    }

    public class DiamondResponseData
    {
        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonProperty("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonProperty("diamondItemList")]
        public List<DiamondItemData>? DiamondItemList { get; set; }
    }
    public class DiamondItemData
    {
        [JsonProperty("itemName")]
        public string? ItemName { get; set; }

        [JsonProperty("price")]
        public int Price { get; set; }

        [JsonProperty("currencyType")]
        public string? CurrencyType { get; set; }

        [JsonProperty("header")]
        public string? Header { get; set; }
    }

    public class JaphwaResponseData
    {
        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonProperty("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonProperty("japhwaItemList")]
        public List<JaphwaItemData>? JaphwaItemList { get; set; }
    }
    public class JaphwaItemData
    {
        [JsonProperty("itemName")]
        public string? ItemName { get; set; }

        [JsonProperty("price")]
        public int Price { get; set; }

        [JsonProperty("currencyType")]
        public string? CurrencyType { get; set; }

        [JsonProperty("header")]
        public string? Header { get; set; }
    }

    public class MileageResponseData
    {
        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonProperty("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonProperty("mileageItemList")]
        public List<MileageItemData>? MileageItemList { get; set; }
    }

    public class MileageItemData
    {
        [JsonProperty("itemName")]
        public string? ItemName { get; set; }

        [JsonProperty("price")]
        public int Price { get; set; }

        [JsonProperty("currencyType")]
        public string? CurrencyType { get; set; }

        [JsonProperty("header")]
        public string? Header { get; set; }
    }
    #endregion
    #region
    [Serializable]
    public class BuyCurrencyRequestData
    {
        [JsonProperty("playFabId")]
        public string? PlayFabId { get; set; }

        [JsonProperty("itemName")]
        public string? ItemName { get; set; }

        [JsonProperty("currencyType")]
        public string? CurrencyType { get; set; }   // "소모 재화"

        [JsonProperty("price")]
        public int Price { get; set; }

        [JsonProperty("goodsType")]
        public string? GoodsType { get; set; }  // "상품 종류"
    }

    [Serializable]
    public class BuyCurrencyResponseData
    {
        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonProperty("errorMessage")]
        public string? ErrorMessage { get; set; }
    }
    #endregion
    #region Gacha
    /// <summary>
    /// 가챠 구매 요청 데이터
    /// </summary>
    public class BuyGachaRequestData
    {
        [JsonProperty("playFabId")]
        public string? PlayFabId { get; set; }

        [JsonProperty("currencyType")]
        public string? CurrencyType { get; set; }   // "WON", "DIAMOND", "GC" …

        [JsonProperty("itemType")]
        public string? ItemType { get; set; }      // "Weapon", "Armor", "Skill"

        // ItemType 이 "Weapon" 이면 WeaponItemData 만 채우고, 나머지 두 필드는 null 로 두면됨. (서버에서 NullValue 무시)
        [JsonProperty("weaponItemData", NullValueHandling = NullValueHandling.Ignore)]
        public WeaponItemData? WeaponItemData { get; set; }

        [JsonProperty("armorItemData", NullValueHandling = NullValueHandling.Ignore)]
        public ArmorItemData? ArmorItemData { get; set; }

        [JsonProperty("skillItemData", NullValueHandling = NullValueHandling.Ignore)]
        public SkillItemData? SkillItemData { get; set; }
    }

    /// <summary>
    /// 가챠 구매 응답 데이터
    /// </summary>
    public class BuyGachaResponseData
    {
        [JsonProperty("isSuccess")] public bool IsSuccess { get; set; }
        [JsonProperty("ownedItemList")] public List<OwnedItemData>? OwnedItemList { get; set; }
    }

    /// <summary>
    /// 획득된 아이템 정보
    /// </summary>
    public class OwnedItemData
    {
        [JsonProperty("itemType")] public string? ItemType { get; set; }

        [JsonProperty("armorItemData", NullValueHandling = NullValueHandling.Ignore)]
        public ArmorItemData? ArmorItemData { get; set; }

        [JsonProperty("weaponItemData", NullValueHandling = NullValueHandling.Ignore)]
        public WeaponItemData? WeaponItemData { get; set; }

        [JsonProperty("skillItemData", NullValueHandling = NullValueHandling.Ignore)]
        public SkillItemData? SkillItemData { get; set; }
    }

    /// <summary>
    /// 방어구 아이템 DTO
    /// </summary>
    public class ArmorItemData
    {
        [JsonProperty("itemName")] public string? ItemName { get; set; }
        [JsonProperty("price")] public int Price { get; set; }
        [JsonProperty("currencyType")] public string? CurrencyType { get; set; }
        [JsonProperty("header")] public string? Header { get; set; }
    }

    /// <summary>
    /// 무기 아이템 DTO
    /// </summary>
    public class WeaponItemData
    {
        [JsonProperty("itemName")] public string? ItemName { get; set; }
        [JsonProperty("price")] public int Price { get; set; }
        [JsonProperty("currencyType")] public string? CurrencyType { get; set; }
        [JsonProperty("header")] public string? Header { get; set; }
    }

    /// <summary>
    /// 스킬 아이템 DTO
    /// </summary>
    public class SkillItemData
    {
        [JsonProperty("itemName")] public string? ItemName { get; set; }
        [JsonProperty("price")] public int Price { get; set; }
        [JsonProperty("currencyType")] public string? CurrencyType { get; set; }
        [JsonProperty("header")] public string? Header { get; set; }
    }
    #endregion
}
