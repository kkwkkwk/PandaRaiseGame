using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// 농장 스탯 팝업에서 서버가 내려주는 응답 전체를 담는 DTO
/// </summary>
[Serializable]
public class FarmStatResponse
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }            // 요청 성공 여부

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }       // 에러 메시지 (실패 시)

    // 상단에 표시할 남은 스탯량 (대나무·당근·옥수수)
    [JsonProperty("remainingBambooStat")]
    public int RemainingBamboo { get; set; }

    [JsonProperty("remainingCarrotStat")]
    public int RemainingCarrot { get; set; }

    [JsonProperty("remainingCornStat")]
    public int RemainingCorn { get; set; }

    // 하단에 뿌려줄 능력치 리스트
    [JsonProperty("abilityStats")]
    public List<AbilityStatData> AbilityStats { get; set; }
}