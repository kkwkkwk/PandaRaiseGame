// 가챠(뽑기) 결과로 서버에서 내려주는 커스텀데이터(CustomData) 를 그대로 매핑하기 위해 만든 타입
using System;
using Newtonsoft.Json;

[Serializable]
public class WeaponGachaItemData
{
    [JsonProperty("itemId")]
    public string ItemId { get; set; }

    [JsonProperty("itemName")]
    public string ItemName { get; set; }

    [JsonProperty("rank")]
    public string Rank { get; set; }

}
