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
}
