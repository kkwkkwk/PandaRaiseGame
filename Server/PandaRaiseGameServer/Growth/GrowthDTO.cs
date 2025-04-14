using System;
using System.Collections.Generic;
using Newtonsoft.Json;


namespace Growth
{
    /// <summary>
    /// 골드 성장(강화) 탭 서버 응답 DTO
    /// </summary>
    [Serializable]
    public class GoldGrowthResponse
    {
        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonProperty("errorMessage")]
        public string? ErrorMessage { get; set; }

        /// <summary>강화 후(or 조회 시) 플레이어가 보유한 골드</summary>
        [JsonProperty("playerGold")]
        public int PlayerGold { get; set; }

        /// <summary>능력(공격력·체력)별 현재 레벨·비용 정보</summary>
        [JsonProperty("goldGrowthList")]
        public List<ServerGoldGrowthData>? GoldGrowthList { get; set; }
    }

    /// <summary>
    /// 능력 하나에 대한 서버‑측 데이터
    /// </summary>
    [Serializable]
    public class ServerGoldGrowthData
    {
        /// <summary>능력 이름 – “공격력”, “체력”</summary>
        [JsonProperty("abilityName")]
        public string? AbilityName { get; set; }

        /// <summary>기본값 (레벨 0 기준 스탯)</summary>
        [JsonProperty("baseValue")]
        public int BaseValue { get; set; }

        /// <summary>레벨이 1 오를 때마다 증가하는 값</summary>
        [JsonProperty("incrementValue")]
        public int IncrementValue { get; set; }

        /// <summary>현재 레벨</summary>
        [JsonProperty("currentLevel")]
        public int CurrentLevel { get; set; }

        /// <summary>다음 레벨업에 필요한 골드</summary>
        [JsonProperty("costGold")]
        public int CostGold { get; set; }
    }

    /// <summary>
    /// 골드 성장(강화) 요청 DTO  
    /// POST Body 예) { "playFabId":"…", "abilityName":"공격력" }
    /// </summary>
    [Serializable]
    public class GoldGrowthChangeRequest
    {
        [JsonProperty("playFabId")]
        public string? PlayFabId { get; set; }

        /// <summary>“공격력” 또는 “체력” (한글)</summary>
        [JsonProperty("abilityName")]
        public string? AbilityName { get; set; }
    }

    /// <summary>
    /// 캐릭터 레벨·경험치 조회 / 레벨업 응답 DTO
    /// </summary>
    [Serializable]
    public class PlayerLevelResponse
    {
        /// <summary>true = 성공, false = 실패</summary>
        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        /// <summary>현재 레벨</summary>
        [JsonProperty("level")]
        public int Level { get; set; }

        /// <summary>현재 보유 경험치</summary>
        [JsonProperty("currentExp")]
        public int CurrentExp { get; set; }

        /// <summary>다음 레벨까지 필요 경험치  
        /// ‑ 만렙이면 0 또는 ‑1 반환</summary>
        [JsonProperty("requiredExp")]
        public int RequiredExp { get; set; }

        /// <summary>오류 메시지(실패 시)</summary>
        [JsonProperty("errorMessage")]
        public string? ErrorMessage { get; set; }
    }

    [Serializable]
    public class LevelStatChangeRequest
    {
        [JsonProperty("playFabId")] public string? PlayFabId { get; set; }
        [JsonProperty("action")] public string? Action { get; set; }   // "increase" | "reset"
        [JsonProperty("abilityName")] public string? AbilityName { get; set; }
    }

    [Serializable]
    public class LevelStatResponse
    {
        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonProperty("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonProperty("playerLevel")]
        public int PlayerLevel { get; set; }

        [JsonProperty("remainingStatPoints")]
        public int RemainingStatPoints { get; set; }

        [JsonProperty("abilityStatList")]
        public List<ServerAbilityStatData>? AbilityStatList { get; set; }
    }

    [Serializable]
    public class ServerAbilityStatData
    {
        [JsonProperty("abilityName")]
        public string? AbilityName { get; set; }

        [JsonProperty("baseValue")]
        public int BaseValue { get; set; }

        [JsonProperty("incrementValue")]
        public int IncrementValue { get; set; }

        [JsonProperty("currentLevel")]
        public int CurrentLevel { get; set; }
    }
}
