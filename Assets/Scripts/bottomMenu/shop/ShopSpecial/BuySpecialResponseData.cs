using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class BuySpecialResponseData
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }    // 실패 시 서버가 돌려주는 오류 메시지
}
