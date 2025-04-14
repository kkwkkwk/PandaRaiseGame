using System;
using System.Collections.Generic;

namespace Growth
{
    public static class GrowthCostTable
    {
        public const int MaxLevel = 100;

        // 기본값·증가치
        public static (int baseVal, int incVal) GetStatInfo(string ability) => ability switch
        {
            "공격력" => (0, 2),
            "체력" => (0, 5),
            _ => (0, 0)
        };

        private const int BaseCost = 1_000;
        private const float Coef = 0.15f;

        public static int GetCost(int currentLevel)
            => currentLevel >= MaxLevel
               ? -1
               : (int)Math.Ceiling(BaseCost * (1 + currentLevel * Coef));
    }
}
