namespace Growth
{
    public static class AbilityStatTable
    {
        // 능력 이름  → (baseValue, incrementValue)
        public static readonly Dictionary<string, (int baseV, int incV)> Meta = new()
        {
            { "Atk", (0, 2) },
            { "Hp",   (0, 5) }
        };

        // 통계 키 생성 규칙
        public static string StatKey(string ability) => $"Stat_{ability}";
    }
}
