using System;
using Newtonsoft.Json;

/// <summary>
/// 씨앗 심기 API 응답 DTO
/// - IsSuccess: 요청 성공 여부
/// - ErrorMessage: 실패 시 에러 메시지
/// - GrowthDurationSeconds: 해당 씨앗이 완전히 자라기까지 필요한 시간(초)
/// </summary>
public class PlantSeedResponse
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }            // 요청 성공 여부

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }       // 실패 시 에러 메시지

    [JsonProperty("growthDurationSeconds")]
    public int GrowthDurationSeconds { get; set; } // 성장 필요 시간(초)
}