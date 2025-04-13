using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class LevelStatResponse
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }

    [JsonProperty("playerLevel")]
    public int PlayerLevel { get; set; }

    [JsonProperty("remainingStatPoints")]
    public int RemainingStatPoints { get; set; }

    [JsonProperty("abilityStatList")]
    public List<ServerAbilityStatData> AbilityStatList { get; set; }
}

[Serializable]
public class ServerAbilityStatData
{
    [JsonProperty("abilityName")]
    public string AbilityName { get; set; }

    [JsonProperty("baseValue")]
    public int BaseValue { get; set; }

    [JsonProperty("incrementValue")]
    public int IncrementValue { get; set; }

    [JsonProperty("currentLevel")]
    public int CurrentLevel { get; set; }
}
