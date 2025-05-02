// UserSeedInfo.cs
using System;
using Newtonsoft.Json;

/// <summary>
/// 플레이어가 보유한 씨앗 정보 DTO
/// - SeedId: 씨앗의 고유 식별자
/// - SeedName: 씨앗의 표시 이름
/// - SpriteName: Resources 폴더에서 로드할 스프라이트 이름
/// - Count: 보유 개수
/// </summary>
public class UserSeedInfo
{
    [JsonProperty("seedId")]
    public string SeedId { get; set; }     // 씨앗 ID

    [JsonProperty("seedName")]
    public string SeedName { get; set; }   // 씨앗 이름

    [JsonProperty("spriteName")]
    public string SpriteName { get; set; } // 스프라이트 파일 이름

    [JsonProperty("count")]
    public int Count { get; set; }         // 보유 개수
}