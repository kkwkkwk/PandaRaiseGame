// FarmPopupResponse.cs
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// 농장 팝업 API 응답 DTO
/// - IsSuccess: 호출 성공 여부
/// - ErrorMessage: 실패 시 에러 메시지
/// - Plots: 농지 상태 리스트
/// - Inventory: 플레이어 씨앗 인벤토리 리스트
/// </summary>
public class FarmPopupResponse
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }            // 호출 성공 여부

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }       // 에러 메시지

    [JsonProperty("plots")]
    public List<PlotInfo> Plots { get; set; }      // 농지 상태 리스트

    [JsonProperty("inventory")]
    public List<UserSeedInfo> Inventory { get; set; } // 씨앗 인벤토리 리스트
}