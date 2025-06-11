using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Farm
{
    /// <summary>
    /// 농장 팝업 조회 요청 DTO
    /// </summary>
    public class GetFarmPopupRequest
    {
        /// <summary>PlayFab 사용자 ID</summary>
        [JsonProperty("playFabId")]
        public string? PlayFabId { get; set; }
    }

    /// <summary>
    /// 씨앗 심기 요청 DTO
    /// </summary>
    public class PlantSeedRequest
    {
        /// <summary>PlayFab 사용자 ID</summary>
        [JsonProperty("playFabId")]
        public string? PlayFabId { get; set; }

        /// <summary>심을 plot 인덱스 (0~5)</summary>
        [JsonProperty("plotIndex")]
        public int PlotIndex { get; set; }

        /// <summary>심을 씨앗 ID</summary>
        [JsonProperty("seedId")]
        public string? SeedId { get; set; }
    }

    /// <summary>
    /// 씨앗 수확 요청 DTO
    /// </summary>
    public class HarvestSeedRequest
    {
        /// <summary>PlayFab 사용자 ID</summary>
        [JsonProperty("playFabId")]
        public string? PlayFabId { get; set; }

        /// <summary>수확할 plot 인덱스 (0~5)</summary>
        [JsonProperty("plotIndex")]
        public int PlotIndex { get; set; }
    }

    /// <summary>
    /// 농장 팝업 API 응답 DTO
    /// </summary>
    public class FarmPopupResponse
    {
        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonProperty("errorMessage", NullValueHandling = NullValueHandling.Ignore)]
        public string? ErrorMessage { get; set; }

        [JsonProperty("plots", NullValueHandling = NullValueHandling.Ignore)]
        public List<PlotInfo>? Plots { get; set; }

        [JsonProperty("inventory", NullValueHandling = NullValueHandling.Ignore)]
        public List<UserSeedInfo>? Inventory { get; set; }
    }

    /// <summary>
    /// 농지 한 칸의 상태를 나타내는 DTO
    /// </summary>
    public class PlotInfo
    {
        [JsonProperty("plotIndex")]
        public int PlotIndex { get; set; }

        [JsonProperty("hasSeed")]
        public bool HasSeed { get; set; }

        [JsonProperty("seedId", NullValueHandling = NullValueHandling.Ignore)]
        public string? SeedId { get; set; }

        [JsonProperty("plantedTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime PlantedTimeUtc { get; set; }

        [JsonProperty("growthDurationSeconds")]
        public int GrowthDurationSeconds { get; set; }
    }

    /// <summary>
    /// 플레이어 보유 씨앗 정보 DTO
    /// </summary>
    public class UserSeedInfo
    {
        [JsonProperty("seedId")]
        public string? SeedId { get; set; }

        [JsonProperty("seedName")]
        public string? SeedName { get; set; }

        [JsonProperty("spriteName")]
        public string? SpriteName { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }
    }

    /// <summary>
    /// 씨앗 심기 API 응답 DTO
    /// </summary>
    public class PlantSeedResponse
    {
        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonProperty("errorMessage", NullValueHandling = NullValueHandling.Ignore)]
        public string? ErrorMessage { get; set; }

        [JsonProperty("growthDurationSeconds")]
        public int GrowthDurationSeconds { get; set; }
    }

    /// <summary>
    /// 씨앗 수확 API 응답 DTO
    /// </summary>
    public class HarvestSeedResponse
    {
        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonProperty("errorMessage", NullValueHandling = NullValueHandling.Ignore)]
        public string? ErrorMessage { get; set; }

        [JsonProperty("statType", NullValueHandling = NullValueHandling.Ignore)]
        public string? StatType { get; set; }

        [JsonProperty("amount", NullValueHandling = NullValueHandling.Ignore)]
        public float Amount { get; set; }
    }

    /// <summary>
    /// 서버에서 전달할 씨앗 데이터 DTO
    /// </summary>
    public class SeedData
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("spriteName")]
        public string? SpriteName { get; set; }

        [JsonProperty("info")]
        public string? Info { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }
    }

    /// <summary>
    /// 씨앗 목록 조회 API 응답 DTO
    /// </summary>
    public class SeedsResponse
    {
        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonProperty("errorMessage", NullValueHandling = NullValueHandling.Ignore)]
        public string? ErrorMessage { get; set; }

        [JsonProperty("seedsList", NullValueHandling = NullValueHandling.Ignore)]
        public List<SeedData>? SeedsList { get; set; }
    }

    // 요청 DTO
    public class FarmStatRequestData
    {
        [JsonProperty("playFabId")]
        public string? PlayFabId { get; set; }
    }

    // 응답 DTO
    public class FarmStatResponse
    {
        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonProperty("errorMessage", NullValueHandling = NullValueHandling.Ignore)]
        public string? ErrorMessage { get; set; }

        [JsonProperty("remainingBamboo")]
        public int RemainingBamboo { get; set; }

        [JsonProperty("remainingCarrot")]
        public int RemainingCarrot { get; set; }

        [JsonProperty("remainingCorn")]
        public int RemainingCorn { get; set; }

        [JsonProperty("abilityStats")]
        public List<AbilityStatData>? AbilityStats { get; set; }
    }

    public class AbilityStatData
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("costStat")]
        public int CostStat { get; set; }
    }


    public class FarmStatChangeRequestData
    {
        [JsonProperty("playFabId")]
        public string? PlayFabId { get; set; }

        [JsonProperty("abilityName")]
        public string? AbilityName { get; set; }
    }
}
