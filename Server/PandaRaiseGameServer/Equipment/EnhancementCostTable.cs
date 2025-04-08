using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equipment
{
    public static class EnhancementCostTable
    {
        // 등급별 골드 비용 배수
        private static readonly Dictionary<string, float> RankCostMultipliers = new Dictionary<string, float>
    {
        { "common", 1.0f },
        { "uncommon", 1.2f },
        { "rare", 1.5f },
        { "unique", 1.8f },
        { "epic", 2.2f },
        { "legendary", 3.0f }
    };

        // 0강~9강까지의 ‘기본 골드 비용’ (인덱스 = 현재 강화 단계)
        private static readonly int[] BaseCosts =
        {
        100, 200, 400, 600, 900,
        1300, 1800, 2400, 3200, 4200
    };

        // 0강~9강까지의 ‘기본 성공 확률’
        private static readonly float[] SuccessRates =
        {
        1.00f, 0.95f, 0.90f, 0.80f, 0.70f,
        0.60f, 0.50f, 0.40f, 0.30f, 0.20f
    };

        // 0강~9강까지의 ‘기본 필요 아이템 개수’ (인덱스 = 현재 강화 단계)
        // 예: 0~2강은 1개, 3~4강은 2개, 5~6강은 3개, 7강은 4개, 8강은 5개, 9강은 5개
        private static readonly int[] BaseItemNeeds =
        {
        1, 1, 1, 2, 2,
        3, 3, 4, 5, 5
    };

        /// <summary>
        /// (1) 골드 비용 계산
        /// </summary>
        public static int GetEnhancementCost(string rank, int currentEnhancement)
        {
            if (currentEnhancement >= 10)
                return -1; // 이미 최대강화 (10강)

            // 배열 인덱스 범위 보정
            int baseCost = BaseCosts[
                currentEnhancement < BaseCosts.Length ? currentEnhancement : BaseCosts.Length - 1
            ];

            float rankMultiplier = 1.0f;
            if (RankCostMultipliers.ContainsKey(rank.ToLower()))
                rankMultiplier = RankCostMultipliers[rank.ToLower()];

            int finalCost = (int)(baseCost * rankMultiplier);
            return finalCost;
        }

        /// <summary>
        /// (2) 강화 성공 확률
        /// </summary>
        public static float GetEnhancementSuccessRate(int currentEnhancement)
        {
            if (currentEnhancement >= 10)
                return 0.0f; // 이미 최대강화

            float rate = SuccessRates[
                currentEnhancement < SuccessRates.Length ? currentEnhancement : SuccessRates.Length - 1
            ];
            return rate;
        }

        /// <summary>
        /// (3) 강화 재료로 소모할 "동일 아이템" 개수
        /// </summary>
        public static int GetRequiredItemCount(string rank, int currentEnhancement)
        {
            if (currentEnhancement >= 10)
                return 0; // 이미 최대강화 -> 추가로 소비할 필요 없음?

            int baseNeed = BaseItemNeeds[
                currentEnhancement < BaseItemNeeds.Length ? currentEnhancement : BaseItemNeeds.Length - 1
            ];
            return baseNeed;
        }
    }
}
