using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class JaphaResponseData
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }

    [JsonProperty("japhwaItemList")]
    public List<JaphaItemData> JaphaItemList { get; set; }
}

[Serializable]
public class JaphaItemData
{
    [JsonProperty("itemName")]
    public string ItemName { get; set; }

    [JsonProperty("price")]
    public int Price { get; set; }

    [JsonProperty("currencyType")]
    public string CurrencyType { get; set; }

    [JsonProperty("header")]
    public string Header { get; set; }
}
