using Newtonsoft.Json;

namespace Shop
{
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
}
