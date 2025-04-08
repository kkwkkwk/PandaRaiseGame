using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equipment
{
    public static class LevelUpCostTable
    {
        private const int MaxLevel = 1000;

        // 등급별 배수 ― EnhancementCostTable와 동일한 값 사용
        private static readonly Dictionary<string, float> RankMultiplier = new()
        {
            { "common",     1.0f },
            { "uncommon",   1.2f },
            { "rare",       1.5f },
            { "unique",     1.8f },
            { "epic",       2.2f },
            { "legendary",  3.0f }
        };

        // 기본(공통) 강화석 필요량 : 0‑9 → 1, 10‑19 → 2 … 90‑99 → 10
        private static int BaseStones(int currentLevel)
        {
            int tier = currentLevel / 10;     // 0‑9
            return tier + 1;                   // 1‑10
        }

        /// <summary>
        /// 등급 + 레벨로 최종 필요 강화석 계산  
        /// (MaxLevel 이상이면 ‑1)
        /// </summary>
        public static int GetRequiredStones(string rank, int currentLevel)
        {
            if (currentLevel >= MaxLevel) return -1;

            int baseNeed = BaseStones(currentLevel);

            float mult = RankMultiplier.TryGetValue(rank.ToLower(), out var m) ? m : 1.0f;
            return (int)(baseNeed * mult);
        }
    }
}
