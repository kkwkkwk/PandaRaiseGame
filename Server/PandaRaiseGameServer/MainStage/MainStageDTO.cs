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

namespace Offline
{
    /// <summary>
    /// 오프라인 보상 요청 DTO
    /// </summary>
    [Serializable]
    public class OfflineRewardRequestData
    {
        /// <summary>PlayFab 사용자 ID</summary>
        [JsonProperty("playFabId")]
        public string? PlayFabId { get; set; }
    }

    /// <summary>
    /// 오프라인 보상 응답 DTO
    /// - isSuccess: 호출 성공 여부
    /// - offlineSeconds: 마지막 접속 이후 경과된 초
    /// - currentFloor: 현재 메인 스테이지 층수
    /// - rewardAmount: 지급된 골드 양
    /// </summary>
    [Serializable]
    public class OfflineRewardResponseData
    {
        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonProperty("offlineSeconds")]
        public int OfflineSeconds { get; set; }

        [JsonProperty("currentFloor")]
        public int CurrentFloor { get; set; }

        [JsonProperty("rewardAmount")]
        public int RewardAmount { get; set; }
    }
}