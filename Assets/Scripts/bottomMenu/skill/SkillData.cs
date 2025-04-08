using System;

[System.Serializable]
public class SkillData
{
    public string skillName;            // 스킬 이름(혹은 ID)
    public string skillId;              // 스킬 UI/설명/아이콘 로딩 용도
    public string itemInstanceId;       // 실제 인벤토리 내 유일 식별자
    public int level;                   // 장비 레벨
    public int enhancement;             // 강화 수치
    public bool isOwned;                // 보유 여부
    public string equipEffectDesc;      // 장착 효과
    public string passiveEffectDesc;    // 보유 효과
    public string rank;                 // 장비 등급 (예: "Common", "Rare", "Epic"...)
}
