[System.Serializable]
public class AbilityStat
{
    public string abilityName;       // 예: "공격력", "체력" 등
    public int currentLevel = 0;     // 투자한 포인트(능력치 레벨)
    public int baseValue = 0;        // 기본값 (예: 0%)
    public int upgradeIncrement = 5; // 1포인트당 상승하는 값 (예: 5%)

    public int GetUpgradedValue()
    {
        return baseValue + currentLevel * upgradeIncrement;
    }
}
