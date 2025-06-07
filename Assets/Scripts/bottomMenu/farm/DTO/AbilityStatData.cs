using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// 하단에서 1줄씩 표시할 “능력치” 정보를 담는 DTO
/// </summary>
[Serializable]
public class AbilityStatData
{
    [JsonProperty("name")]
    public string Name { get; set; }               // 능력치 이름 (예: "아이템 드랍률")

    [JsonProperty("currentValue")]
    public int CurrentValue { get; set; }          // 현재 능력치 (예: 10 → "10%")

    [JsonProperty("costStat")]
    public int CostStat { get; set; }              // 스탯 1 올리는데 필요한 스탯량
}