using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class DungeonRequestData
{
    [JsonProperty("playFabId")]
    public string PlayFabId { get; set; }

    [JsonProperty("floor")]
    public int Floor { get; set; }        // 도전 또는 소탕할 층 번호

    [JsonProperty("cleared")]
    public bool Cleared { get; set; }     // true면 보스 클리어 플래그

    [JsonProperty("isSweep")]
    public bool IsSweep { get; set; }     // true면 소탕 요청, false면 일반 도전

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