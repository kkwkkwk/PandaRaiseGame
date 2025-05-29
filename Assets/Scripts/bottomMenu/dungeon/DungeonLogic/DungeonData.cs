using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
    public class DungeonRequestData
    {
        [JsonProperty("playFabId")]
        public string PlayFabId { get; set; }
    }

[Serializable]
public class DungeonResponseData
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonProperty("ticketCount")]   // 사용 가능한 던전 티켓 수량 (DT)
    public int TicketCount { get; set; }

    [JsonProperty("highestClearedFloor")] // 서버가 기록하고 있는 최고 클리어 층수
    public int HighestClearedFloor { get; set; }
}