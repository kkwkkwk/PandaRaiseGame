using System;
using Newtonsoft.Json;

/// <summary>
/// 씨앗 수확 API 응답 DTO
/// - IsSuccess: 요청 성공 여부
/// - ErrorMessage: 실패 시 에러 메시지
/// - StatType: 제공할 스탯 유형 ("GoldGain", "ItemDropRate", "ExpGain", "GrowthTimeReduction")
/// - Amount: 제공할 스탯 값 (퍼센트 또는 절대값)
/// </summary> 
public class HarvestSeedResponse
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }            // 요청 성공 여부

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }       // 실패 시 에러 메시지

    [JsonProperty("statType")]
    public string StatType { get; set; }           // 제공할 스탯 유형

    [JsonProperty("amount")]
    public float Amount { get; set; }              // 제공할 스탯 값
}