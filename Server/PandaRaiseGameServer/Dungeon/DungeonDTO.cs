using System;
using Newtonsoft.Json;

namespace Dungeon
{
    /// <summary>
    /// 던전 정보 조회 요청 DTO
    /// </summary>
    [Serializable]
    public class DungeonRequestData
    {
        /// <summary>PlayFab 사용자 ID</summary>
        [JsonProperty("playFabId")]
        public string? PlayFabId { get; set; }
    }

    /// <summary>
    /// 던전 정보 조회 응답 DTO
    /// - IsSuccess: 호출 성공 여부
    /// - TicketCount: 사용 가능한 던전 티켓 수량 (가상화폐 "DT")
    /// - HighestClearedFloor: 서버가 기록하고 있는 최고 클리어 층수
    /// </summary>
    [Serializable]
    public class DungeonResponseData
    {
        /// <summary>호출 성공 여부</summary>
        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        /// <summary>사용 가능한 던전 티켓 수량 (VirtualCurrency "DT")</summary>
        [JsonProperty("ticketCount")]
        public int TicketCount { get; set; }

        /// <summary>서버에 저장된 최고 클리어 층수</summary>
        [JsonProperty("highestClearedFloor")]
        public int HighestClearedFloor { get; set; }
    }
}
