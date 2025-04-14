using System;

namespace Growth
{
    /// <summary>레벨 → 다음 레벨까지 필요 EXP 계산용 테이블</summary>
    public static class LevelExpTable
    {
        public const int MaxLevel = 1000;

        /// <summary>
        /// 현재 레벨에서 다음 레벨까지 필요 EXP  
        /// * 예시 공식 : 100 × (레벨^1.15) 를 올림
        /// </summary>
        public static int GetRequiredExp(int level)
        {
            if (level >= MaxLevel) return -1;           // 만렙
            double need = 100.0 * Math.Pow(level, 1.15);
            return (int)Math.Ceiling(need);
        }
    }
}
