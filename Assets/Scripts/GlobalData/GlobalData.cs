using PlayFab.ClientModels;
using System.Collections.Generic;

public static class GlobalData
{
    public static string playFabId;
    public static EntityTokenResponse entityToken;

    // PlayFab GetUserInventory 로 받은 가상화폐 잔액 딕셔너리
    // Key: 통화 코드 (e.g. "DM","GC","ML"…)
    // Value: 잔액
    public static Dictionary<string, int> VirtualCurrencyBalances { get; set; }
        = new Dictionary<string, int>();

    // 프로필
    public static ProfilePopupManager.ProfileData ProfileData { get; set; }

    // 퀘스트
    public static string QuestDataJson { get; set; } = "";

    // 무기 목록
    public static List<WeaponData> WeaponDataList { get; set; } = new List<WeaponData>();
    // 방어구 목록
    public static List<ArmorData> ArmorDataList { get; set; } = new List<ArmorData>();
    // 스킬 목록
    public static List<SkillData> SkillDataList { get; set; } = new List<SkillData>();

}