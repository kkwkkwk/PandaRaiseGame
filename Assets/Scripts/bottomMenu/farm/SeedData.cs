// SeedData.cs
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class SeedData
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    // server 에서는 이 필드만 내려줍니다.
    // 예: "Carrot", "Tomato" 등 Resources/SeedImages/<SpriteName>.png 이름과 매칭
    [JsonProperty("spriteName")]
    public string SpriteName { get; set; }

    [JsonProperty("info")]
    public string Info { get; set; }
}

[Serializable]
public class SeedsResponse
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }

    [JsonProperty("seedsList")]
    public List<SeedData> SeedsList { get; set; }
}
