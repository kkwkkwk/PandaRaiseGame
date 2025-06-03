// MainStageDTO.cs
using System;
using Newtonsoft.Json;

namespace MainStage
{
    /// <summary>
    /// 메인 스테이지 조회 요청 DTO
    /// </summary>
    [Serializable]
    public class MainStageRequestData
    {
        [JsonProperty("playFabId")]
        public string? PlayFabId { get; set; }
    }

    /// <summary>
    /// 메인 스테이지 조회 응답 DTO
    /// - isSuccess: 호출 성공 여부
    /// - currentFloor: 현재 층수
    /// </summary>
    [Serializable]
    public class MainStageResponseData
    {
        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonProperty("currentFloor")]
        public int CurrentFloor { get; set; }
    }
}
