using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class DungeonRewardsData
{
    [JsonProperty("rewards")]
    public List<RewardItemData> Rewards { get; set; } // 서버로부터 내려받은 보상 목록
}

[Serializable]
public class RewardItemData
{
    [JsonProperty("type")]
    public string RewardType { get; set; } // 보상 타입 ("GOLD", "DM", "EXP" 등)

    [JsonProperty("amount")] // 지급 수량
    public int Amount { get; set; }
}
