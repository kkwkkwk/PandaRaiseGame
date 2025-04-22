using System.Collections;
using UnityEngine;
using PlayFab;

/// <summary>
/// 로딩 화면을 관리 + 병렬 로딩을 모두 처리하는 매니저
/// </summary>
public class LoadingScreenManager : MonoBehaviour
{
    public static LoadingScreenManager Instance { get; private set; }

    [Header("로딩 화면")]
    public GameObject loadingCanvas; // 인스펙터에서 로딩 UI(Canvas)를 연결하세요.

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // 씬 전환 시에도 계속 유지하려면
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator Start()
    {
        // 예) 씬 시작 시점에 자동으로 실행
        ShowLoadingScreen();

        // 글로벌 데이터(가상화폐 잔액) 초기화
        if (GlobalDataManager.Instance != null)
        {
            yield return StartCoroutine(GlobalDataManager.Instance.Initialize());
        }
        else
        {
            Debug.LogWarning("[LoadingScreenManager] GlobalDataManager 인스턴스를 찾을 수 없습니다.");
        }

        // 병렬 로딩 코루틴 실행
        yield return StartCoroutine(LoadAllDataConcurrently());

        // 모든 데이터 로드 완료 후 로딩 화면 끄기
        HideLoadingScreen();

        Debug.Log("[LoadingScreenManager] 모든 데이터 로딩 완료");
    }

    /// <summary>
    /// 병렬로 여러 StartSequence 코루틴을 실행하고, 모두 끝날 때까지 대기
    /// </summary>
    private IEnumerator LoadAllDataConcurrently()
    {
        Debug.Log("[LoadingScreenManager] 병렬 로딩 시작");

        // 각각의 완료 여부를 추적할 bool 플래그
        bool questDone = false;
        bool profileDone = false;
        bool leaderboardDone = false;
        bool guildDone = false;
        bool weaponDone = false;
        bool armorDone = false;
        bool skillDone = false;

        // 퀘스트
        if (QuestPopupManager.Instance != null)
        {
            StartCoroutine(ParallelHelper(
                QuestPopupManager.Instance.StartSequence(),
                () => questDone = true
            ));
        }
        else
        {
            // 없으면 그냥 완료 처리
            questDone = true;
        }
        // 프로필
        if (ProfilePopupManager.Instance != null)
        {
            StartCoroutine(ParallelHelper(
                ProfilePopupManager.Instance.StartSequence(),
                () => profileDone = true
            ));
        }
        else
        {
            profileDone = true;
        }
        // 리더보드
        if (LeaderboardPopupManager.Instance != null)
        {
            StartCoroutine(ParallelHelper(
                LeaderboardPopupManager.Instance.StartSequence(),
                () => leaderboardDone = true
            ));
        }
        else
        {
            leaderboardDone = true;
        }
        // 길드
        if (GuildPopupManager.Instance != null)
        {
            StartCoroutine(ParallelHelper(
                GuildPopupManager.Instance.StartSequence(),
                () => guildDone = true
            ));
        }
        else
        {
            guildDone = true;
        }
        // 무기
        if (WeaponPopupManager.Instance != null)
        {
            StartCoroutine(ParallelHelper(
                WeaponPopupManager.Instance.StartSequence(),
                () => weaponDone = true
            ));
        }
        else
        {
            weaponDone = true;
        }
        if (ArmorPopupManager.Instance != null)
        {
            StartCoroutine(ParallelHelper(
                ArmorPopupManager.Instance.StartSequence(),
                () => armorDone = true
            ));
        }
        else
        {
            armorDone = true;
        }
        if (SkillPopupManager.Instance != null)
        {
            StartCoroutine(ParallelHelper(
                SkillPopupManager.Instance.StartSequence(),
                () => skillDone = true
            ));
        }
        else
        {
            skillDone = true;
        }
        // 모든 로드가 끝날 때까지 대기
        while (!questDone || !profileDone || !leaderboardDone || !guildDone || !weaponDone || !armorDone || !skillDone)
        {
            yield return null;
        }
        Debug.Log("[LoadingScreenManager] 병렬 로딩 완료");
    }

    /// <summary>
    /// 실제 로드 코루틴을 수행한 후, 끝나면 onComplete() 콜백을 호출해주는 헬퍼
    /// </summary>
    private IEnumerator ParallelHelper(IEnumerator targetCoroutine, System.Action onComplete)
    {
        yield return StartCoroutine(targetCoroutine);
        onComplete?.Invoke();
    }

    /// <summary>
    /// 로딩 화면 켜기
    /// </summary>
    public void ShowLoadingScreen()
    {
        if (loadingCanvas != null)
        {
            loadingCanvas.SetActive(true);
        }
    }

    /// <summary>
    /// 로딩 화면 끄기
    /// </summary>
    public void HideLoadingScreen()
    {
        if (loadingCanvas != null)
        {
            loadingCanvas.SetActive(false);
        }
    }
}
