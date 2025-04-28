using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class JaphwaResponseData
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }

    [JsonProperty("japhwaItemList")]
    public List<JaphwaItemData> JaphaItemList { get; set; }
}

[Serializable]
public class JaphwaItemData
{
    [JsonProperty("itemName")]
    public string ItemName { get; set; }

    [JsonProperty("price")]
    public int Price { get; set; }

    [JsonProperty("currencyType")]
    public string CurrencyType { get; set; }

    [JsonProperty("goodsType")]
    public string GoodsType { get; set; }  // "상품 종류"

    [JsonProperty("header")]
    public string Header { get; set; }
}
