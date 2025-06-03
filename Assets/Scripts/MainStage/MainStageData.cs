using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class MainStageRequestData
{
    [JsonProperty("playFabId")]
    public string PlayFabId { get; set; }

}

[Serializable]
public class MainStageResponseData
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonProperty("currentFloor")] // 현재 층수
    public int CurrentFloor { get; set; }

}