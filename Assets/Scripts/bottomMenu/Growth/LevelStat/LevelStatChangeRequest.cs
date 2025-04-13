using System;
using Newtonsoft.Json;

/// <summary>
/// 특정 능력치에 대한 변경(증가/초기화) 요청 DTO
/// 실제 서버 구현에 따라 내용은 달라질 수 있습니다.
/// </summary>
[Serializable]
public class LevelStatChangeRequest
{
    [JsonProperty("playFabId")]
    public string PlayFabId { get; set; }

    [JsonProperty("action")]
    public string Action { get; set; }  // "increase" or "reset"

    [JsonProperty("abilityName")]
    public string AbilityName { get; set; }
}
