using Newtonsoft.Json;

namespace Shop
{
    public class WeaponResponseData
    {
        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonProperty("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonProperty("weaponItemList")]
        public List<WeaponItemData>? WeaponItemList { get; set; }
    }

    public class WeaponItemData
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

    public class ArmorResponseData
    {
        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonProperty("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonProperty("armorItemList")]
        public List<ArmorItemData>? ArmorItemList { get; set; }
    }

    public class ArmorItemData
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

    public class SkillResponseData
    {
        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonProperty("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonProperty("skillItemList")]
        public List<SkillItemData>? SkillItemList { get; set; }
    }

    public class SkillItemData
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
}
