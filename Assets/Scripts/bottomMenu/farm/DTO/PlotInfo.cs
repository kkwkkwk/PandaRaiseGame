using System;
using Newtonsoft.Json;

/// <summary>
/// 농지 한 칸의 상태를 나타내는 DTO
/// - PlotIndex: 농지의 인덱스 (0~5)
/// - HasSeed: 현재 씨앗이 심어져 있는지 여부
/// - SeedId: 심어진 씨앗의 식별자 (없으면 null 또는 빈 문자열)
/// - PlantedTimeUtc: 씨앗이 심어진 시각(UTC)
/// - GrowthDurationSeconds: 씨앗이 완전히 성장하는 데 필요한 총 시간(초)
/// </summary>
public class PlotInfo
{
    [JsonProperty("plotIndex")]
    public int PlotIndex { get; set; }  // 농지 인덱스

    [JsonProperty("hasSeed")]
    public bool HasSeed { get; set; }   // 씨앗 심김 여부

    [JsonProperty("seedId")]
    public string SeedId { get; set; }  // 씨앗 ID

    [JsonProperty("plantedTimeUtc")]
    public DateTime PlantedTimeUtc { get; set; }  // 심기 시각(UTC)

    [JsonProperty("growthDurationSeconds")]
    public int GrowthDurationSeconds { get; set; }  // 성장 필요 시간(초)
}