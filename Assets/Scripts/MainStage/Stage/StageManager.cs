using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;  // 전역 접근용 싱글톤

    [Header("Stage Data")]
    [Tooltip("각 큰 스테이지의 StageData 에셋 배열 (예: 10개)")]
    public StageData[] stages;
    private int currentBigStageIndex = 0;  // 0 ~ (stages.Length - 1)
    private StageData currentStageData;

    // 서브 스테이지: 1 ~ 100
    private int currentSubStage = 1;
    private int killCount = 0;

    private int maxStage = 50;

    [Header("UI Panels")]
    public GameObject idleModePanel;   // 방치형(Idle) 모드 패널
    public GameObject bossModePanel;   // 보스 모드 패널

    [Header("Optional References")]
    public TextMeshProUGUI stageInfoText;

    [Header("Spawner Objects")]
    [Tooltip("일반 몹 스폰용 오브젝트 (EnemySpawner)")]
    public GameObject mobSpawnerObject;
    [Tooltip("보스 스폰용 오브젝트 (BossSpawner)")]
    public GameObject bossSpawnerObject;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (stages == null || stages.Length == 0)
        {
            Debug.LogError("[StageManager] StageData 에셋이 할당되지 않았습니다!");
            return;
        }

        currentBigStageIndex = 0;
        currentSubStage = 1;
        killCount = 0;

        currentStageData = stages[currentBigStageIndex];
        UpdateStageInfo();

        // 초기 서브 스테이지 모드 결정 (1 => 잡몹)
        GoToCorrectMode();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //                          일반 몹 처치 시
    // ─────────────────────────────────────────────────────────────────────────
    public void OnEnemyKilled()
    {
        Debug.Log("[StageManager] OnEnemyKilled() 호출됨");

        // 현재 스테이지가 보스 스테이지면 (ex: 10,20,30..100) 일반 몹 처치는 무시
        if (IsBossStage(currentSubStage))
        {
            Debug.Log($"[StageManager] 서브 스테이지 {currentSubStage}는 보스 스테이지이므로 몹 처치는 무시합니다.");
            return;
        }

        killCount++;
        Debug.Log($"[StageManager] 적 처치: {killCount}/{currentStageData.killGoal} (Sub Stage {currentSubStage}/100)");

        // 처치 목표 달성 → 다음 서브 스테이지
        if (killCount >= currentStageData.killGoal)
        {
            killCount = 0;
            currentSubStage++;

            if (currentSubStage > maxStage)
            {
                // 1 ~ maxStage 전부 끝
                Debug.Log("[StageManager] 모든 스테이지 클리어! (subStage=101)");
                // ───────── [추가된 부분] ─────────
                // 다음 큰 스테이지(챕터)로 이동
                currentBigStageIndex++;
                if (currentBigStageIndex >= stages.Length)
                {
                    // 모든 챕터 끝 → 다시 0으로 돌리거나 게임 클리어 처리
                    currentBigStageIndex = 0;
                }
                currentStageData = stages[currentBigStageIndex];
                currentSubStage = 1; // 새 챕터 첫 스테이지로
                killCount = 0;
                UpdateStageInfo();
                GoToCorrectMode();
                // ─────────────────────────────
                return;
            }

            UpdateStageInfo();
            GoToCorrectMode();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //                          보스 승리 시
    // ─────────────────────────────────────────────────────────────────────────
    public void OnBossVictory()
    {
        Debug.Log($"[StageManager] 보스 승리! 서브 스테이지 {currentSubStage} 클리어");

        // 다음 서브 스테이지로
        currentSubStage++;
        killCount = 0;

        if (currentSubStage > maxStage)
        {
            Debug.Log("[StageManager] 모든 스테이지 클리어! (subStage=101)");
            // ───────── [추가된 부분] ─────────
            // 다음 큰 스테이지(챕터)로 이동
            currentBigStageIndex++;
            if (currentBigStageIndex >= stages.Length)
            {
                // 모든 챕터 끝 → 다시 0으로 돌리거나 게임 클리어 처리
                currentBigStageIndex = 0;
            }
            currentStageData = stages[currentBigStageIndex];
            currentSubStage = 1; // 새 챕터 첫 스테이지로
            killCount = 0;
            UpdateStageInfo();
            GoToCorrectMode();
            // ─────────────────────────────
            return;
        }

        UpdateStageInfo();
        GoToCorrectMode();
    }

    public void OnBossDefeat()
    {
        Debug.Log($"[StageManager] 보스 패배! 서브 스테이지 {currentSubStage} 재도전.");
        // TODO: 재도전 로직 (보스 스테이지 유지)
    }

    // ─────────────────────────────────────────────────────────────────────────
    //                          모드 전환 로직
    // ─────────────────────────────────────────────────────────────────────────
    private bool IsBossStage(int stg)
    {
        // 서브 스테이지가 10,20,30,...,100 인지 확인
        return (stg % 10 == 0);
    }

    private void GoToCorrectMode()
    {
        // subStage가 10의 배수이면 보스 모드, 아니면 Idle 모드
        if (IsBossStage(currentSubStage))
        {
            GoToBossMode();
        }
        else
        {
            GoToIdleMode();
        }
    }

    public void GoToIdleMode()
    {
        Debug.Log($"[StageManager] Idle 모드 (subStage={currentSubStage}).");
        if (idleModePanel != null) idleModePanel.SetActive(true);
        if (bossModePanel != null) bossModePanel.SetActive(false);

        if (mobSpawnerObject != null) mobSpawnerObject.SetActive(true);
        if (bossSpawnerObject != null) bossSpawnerObject.SetActive(false);
    }

    public void GoToBossMode()
    {
        Debug.Log($"[StageManager] Boss 모드 (subStage={currentSubStage}).");
        if (idleModePanel != null) idleModePanel.SetActive(false);
        if (bossModePanel != null) bossModePanel.SetActive(true);

        if (mobSpawnerObject != null) mobSpawnerObject.SetActive(false);
        if (bossSpawnerObject != null) bossSpawnerObject.SetActive(true);
    }
                        
    // 현재 챕터 표시
    private void UpdateStageInfo()
    {
        if (stageInfoText != null && currentStageData != null)
        {
            // 예: "Stage: 1-3/100" 식으로 표시
            stageInfoText.text = $"{currentStageData.chapter} - {currentSubStage}";
        }
    }

    public StageData GetCurrentStageData()
    {
        return currentStageData;
    }
}
