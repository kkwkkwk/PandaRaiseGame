using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// 골드 강화 탭에서 사용할 서버 응답 DTO
/// 예시 JSON:
/// {
///   "isSuccess": true,
///   "errorMessage": "",
///   "playerGold": 123456,
///   "goldGrowthList": [
///       {
///         "abilityName": "공격력",
///         "baseValue": 0,
///         "incrementValue": 2,
///         "currentLevel": 5,
///         "costGold": 1000
///       },
///       {
///         "abilityName": "체력",
///         "baseValue": 0,
///         "incrementValue": 5,
///         "currentLevel": 3,
///         "costGold": 2000
///       }
///   ]
/// }
/// </summary>
[Serializable]
public class GoldGrowthResponse
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }

    [JsonProperty("playerGold")]
    public int PlayerGold { get; set; }

    [JsonProperty("goldGrowthList")]
    public List<ServerGoldGrowthData> GoldGrowthList { get; set; }
}

[Serializable]
public class ServerGoldGrowthData
{
    [JsonProperty("abilityName")]
    public string AbilityName { get; set; }

    [JsonProperty("baseValue")]
    public int BaseValue { get; set; }

    [JsonProperty("incrementValue")]
    public int IncrementValue { get; set; }

    [JsonProperty("currentLevel")]
    public int CurrentLevel { get; set; }

    [JsonProperty("costGold")]
    public int CostGold { get; set; }
}
