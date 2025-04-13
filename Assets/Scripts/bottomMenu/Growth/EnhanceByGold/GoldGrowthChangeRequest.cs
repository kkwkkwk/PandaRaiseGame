using System;
using Newtonsoft.Json;

/// <summary>
/// 서버에 "골드 강화"를 요청하는 DTO. (필요한 필드만 포함)
/// 예: POST /api/EnhanceAbilityWithGold
/// {
///   "playFabId": "...",
///   "abilityName": "공격력"
/// }
/// </summary>
[Serializable]
public class GoldGrowthChangeRequest
{
    [JsonProperty("playFabId")]
    public string PlayFabId { get; set; }

    [JsonProperty("abilityName")]
    public string AbilityName { get; set; }
}
