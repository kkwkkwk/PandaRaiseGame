using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DungeonStageManager : MonoBehaviour
{
    public static DungeonStageManager Instance { get; private set; }

    /// <summary>
    /// 현재 플레이 중인 던전 층
    /// </summary>
    public int CurrentFloor { get; private set; }

    [Header("던전 데이터")]
    [Tooltip("1~10층에 해당하는 Stage Data를 순서대로 할당")]
    [SerializeField] private DungeonStageData[] stageDataArray;
    [Tooltip("보스 소환 기준 Transform")]
    [SerializeField] private Transform bossSpawnRoot;

    [Header("UI (Floor 표시)")]
    [Tooltip("Text 컴포넌트가 붙어 있는 GameObject")]
    [SerializeField] private GameObject floorNumberObject;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// 던전 도전 시작. 지정된 층으로 진입
    /// </summary>
    public void StartDungeon(int floor)
    {
        LoadDungeonStage(floor);
    }

    private void LoadDungeonStage(int floor)
    {
        CurrentFloor = Mathf.Clamp(floor, 1, stageDataArray.Length);
        Debug.Log($"[DungeonStageManager] Loading dungeon floor {CurrentFloor}");
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene($"DungeonFloor{CurrentFloor}");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.name.StartsWith("DungeonFloor")) return;

        // 1) 상단 UI에 층수 표시
        if (floorNumberObject != null)
        {
            var txt = floorNumberObject.GetComponent<Text>();
            if (txt != null) txt.text = $"{CurrentFloor}층";
            else Debug.LogWarning("[DungeonStageManager] floorNumberObject에 Text 컴포넌트가 없습니다.");
        }

        // 2) 플레이어 체력 100% 회복 & 사망 이벤트 구독
        var stats = FindObjectOfType<PlayerStats>();
        if (stats != null)
        {
            stats.currentHealth = stats.maxHealth;
            stats.OnDeath += OnPlayerDefeated;
            Debug.Log("[DungeonStageManager] 플레이어 체력 회복 및 사망 이벤트 구독 완료");
        }
        else Debug.LogWarning("[DungeonStageManager] PlayerStats를 찾을 수 없습니다.");

        // 3) 기존 BossSpawner로 기존 보스 제거
        var spawner = FindObjectOfType<BossSpawner>();
        if (spawner != null) spawner.ClearBoss();

        // 4) 던전용 보스 직접 소환 및 이벤트 구독
        var bossData = stageDataArray[CurrentFloor - 1];
        var boss = Instantiate(
            bossData.bossPrefab,
            bossSpawnRoot.position,
            Quaternion.identity,
            spawner != null ? spawner.transform : null
        );
        var bossStats = boss.GetComponent<BossStats>();
        if (bossStats != null) bossStats.OnBossDeath += OnBossCleared;
        else Debug.LogWarning("[DungeonStageManager] BossStats 컴포넌트가 없습니다.");

        // 콜백 중복 등록 방지
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 보스 처치 시 호출: 서버에 클리어 업데이트 후 다음 층 진입
    /// </summary>
    public void OnBossCleared()
    {
        // 서버에 최고 클리어 층 업데이트
        StartCoroutine(DungeonManager.Instance.UpdateClearedFloorCoroutine(CurrentFloor));

        // 다음 층으로 진입
        StartDungeon(CurrentFloor + 1);
    }

    /// <summary>
    /// 플레이어 사망(실패) 시 호출: 서버에서 보상 요청 후 UI 표시
    /// </summary>
    public void OnPlayerDefeated()
    {
        // 실패 보상 요청: cleared=false, isSweep=false
        DungeonRewardManager.Instance.RequestAndDisplayRewards(
            CurrentFloor,
            /* cleared= */ false,
            /* isSweep= */ false
        );
    }
}
