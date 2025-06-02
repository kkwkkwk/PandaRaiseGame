using System.Collections;
using UnityEngine;
using PlayFab;

/// <summary>
/// 로딩 화면을 관리 + 병렬 로딩을 순차적으로 처리하는 매니저
/// </summary>
public class LoadingScreenManager : MonoBehaviour
{
    public static LoadingScreenManager Instance { get; private set; }

    [Header("로딩 화면")]
    public GameObject loadingCanvas;

    private void Awake()
    {
        Debug.Log($"[LoadingScreenManager] Awake 호출됨 → 오브젝트 이름: {gameObject.name}, 씬 이름: {gameObject.scene.name}");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator Start()
    {
        ShowLoadingScreen();
        Debug.Log("[LoadingScreenManager] 데이터 로딩 시작");
        if (GlobalDataManager.Instance != null)
        {
            yield return StartCoroutine(GlobalDataManager.Instance.Initialize());
        }
        else
        {
            Debug.LogWarning("[LoadingScreenManager] GlobalDataManager 인스턴스를 찾을 수 없습니다.");
        }

        yield return StartCoroutine(LoadAllDataConcurrently());

        HideLoadingScreen();
        Debug.Log("[LoadingScreenManager] 모든 데이터 로딩 완료");
    }

    private IEnumerator LoadAllDataConcurrently()
    {
        Debug.Log("[LoadingScreenManager] 병렬 로딩 시작");

        // === 1차 병렬 로딩 ===
        bool questDone = false;
        bool profileDone = false;
        bool leaderboardDone = false;
        bool guildDone = false;
        bool weaponDone = false;
        bool armorDone = false;
        bool skillDone = false;
        bool farmDone = false;
        bool dungeonDone = false;

        StartIfExists(QuestPopupManager.Instance, QuestPopupManager.Instance?.StartSequence(), () => questDone = true);
        StartIfExists(ProfilePopupManager.Instance, ProfilePopupManager.Instance?.StartSequence(), () => profileDone = true);
        StartIfExists(LeaderboardPopupManager.Instance, LeaderboardPopupManager.Instance?.StartSequence(), () => leaderboardDone = true);
        StartIfExists(GuildPopupManager.Instance, GuildPopupManager.Instance?.StartSequence(), () => guildDone = true);
        StartIfExists(WeaponPopupManager.Instance, WeaponPopupManager.Instance?.StartSequence(), () => weaponDone = true);
        StartIfExists(ArmorPopupManager.Instance, ArmorPopupManager.Instance?.StartSequence(), () => armorDone = true);
        StartIfExists(SkillPopupManager.Instance, SkillPopupManager.Instance?.StartSequence(), () => skillDone = true);
        StartIfExists(FarmPopupManager.Instance, FarmPopupManager.Instance?.StartSequence(), () => farmDone = true);
        StartIfExists(DungeonManager.Instance, DungeonManager.Instance.StartSequence(), () => dungeonDone = true);



        while (!questDone || !profileDone || !leaderboardDone ||
               !guildDone || !weaponDone || !armorDone || !skillDone || !farmDone || !dungeonDone)
        {
            yield return null;
        }

        Debug.Log("[LoadingScreenManager] 1차 병렬 로딩 완료");

        // === 2차 병렬 로딩 (Shop 관련) ===
        bool ShopWeaponDone = false;
        bool ShopArmorDone = false;
        bool ShopSkillDone = false;
        bool ShopFreeDone = false;
        bool ShopDiamondDone = false;
        bool ShopJaphwaDone = false;
        bool ShopMileageDone = false;
        bool ShopSpecialDone = false;
        bool ShopVipDone = false;

        StartIfExists(ShopWeaponManager.Instance, ShopWeaponManager.Instance?.StartSequence(), () => ShopWeaponDone = true);
        StartIfExists(ShopArmorManager.Instance, ShopArmorManager.Instance?.StartSequence(), () => ShopArmorDone = true);
        StartIfExists(ShopSkillManager.Instance, ShopSkillManager.Instance?.StartSequence(), () => ShopSkillDone = true);
        StartIfExists(ShopFreeManager.Instance, ShopFreeManager.Instance?.StartSequence(), () => ShopFreeDone = true);
        StartIfExists(ShopDiamondManager.Instance, ShopDiamondManager.Instance?.StartSequence(), () => ShopDiamondDone = true);
        StartIfExists(ShopJaphwaManager.Instance, ShopJaphwaManager.Instance?.StartSequence(), () => ShopJaphwaDone = true);
        StartIfExists(ShopMileageManager.Instance, ShopMileageManager.Instance?.StartSequence(), () => ShopMileageDone = true);
        StartIfExists(ShopSpecialManager.Instance, ShopSpecialManager.Instance?.StartSequence(), () => ShopSpecialDone = true);
        StartIfExists(ShopVipManager.Instance, ShopVipManager.Instance?.StartSequence(), () => ShopVipDone = true);

        while (!ShopWeaponDone || !ShopArmorDone || !ShopSkillDone ||
               !ShopFreeDone || !ShopDiamondDone || !ShopJaphwaDone ||
               !ShopMileageDone || !ShopSpecialDone || !ShopVipDone)
        {
            yield return null;
        }

        Debug.Log("[LoadingScreenManager] 2차 병렬 로딩 완료");
    }

    private void StartIfExists(object instance, IEnumerator coroutine, System.Action onComplete)
    {
        if (instance != null && coroutine != null)
            StartCoroutine(ParallelHelper(coroutine, onComplete));
        else
            onComplete?.Invoke();
    }

    private IEnumerator ParallelHelper(IEnumerator targetCoroutine, System.Action onComplete)
    {
        yield return StartCoroutine(targetCoroutine);
        onComplete?.Invoke();
    }

    public void ShowLoadingScreen()
    {
        if (loadingCanvas != null)
        {
            loadingCanvas.SetActive(true);
        }
    }

    public void HideLoadingScreen()
    {
        if (loadingCanvas != null)
        {
            loadingCanvas.SetActive(false);
        }
    }
}
