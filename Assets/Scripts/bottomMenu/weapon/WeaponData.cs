using System;

[System.Serializable]
public class WeaponData
{
    public string weaponName;               // 무기 이름
    public string weaponId;                 // 무기 UI/설명/아이콘 로딩 용도
    public string itemInstanceId;           // 실제 인벤토리 내 유일 식별자

    public int level;                       // 장비 레벨
    public int enhancement;                 // 강화 수치
    public bool isOwned;                    // 사용자 보유 여부

    public string equipEffectDesc;          // 장착효과 설명
    public string passiveEffectDesc;        // 보유효과 설명
    public string rank;                     // 장비 등급 (예: "Common", "Rare", "Epic"...)

}
