using System;

[System.Serializable]
public class WeaponData
{
    public string weaponName;   // 무기 이름(혹은 ID)
    public int level;           // 장비 레벨
    public int enhancement;     // 강화 수치
    public string rank;         // 장비 등급 (예: "Common", "Rare", "Epic"...)
}
