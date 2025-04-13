using System;
using Newtonsoft.Json;

/// <summary>
/// 서버로부터 수신할 플레이어 레벨/경험치 정보(예시)
/// </summary>
[Serializable]
public class PlayerLevelResponse
{
    // 예: 조회 성공 여부
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }

    // 플레이어 레벨
    [JsonProperty("level")]
    public int Level { get; set; }

    // 현재 보유 경험치
    [JsonProperty("currentExp")]
    public int CurrentExp { get; set; }

    // 다음 레벨업에 필요한 총 경험치
    [JsonProperty("requiredExp")]
    public int RequiredExp { get; set; }

    // 서버에서 에러메시지를 내려줄 수도 있음
    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }
}
/*
 example

{
    "isSuccess": true,
    "level": 10,
    "currentExp": 250,
    "requiredExp": 500,
    "errorMessage": ""
}

 */