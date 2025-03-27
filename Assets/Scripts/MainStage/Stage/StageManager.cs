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
    private int currentSubStage = 1;         // 서브 스테이지: 1 ~ 10 (1~9: 일반, 10: 보스)
    private int killCount = 0;

    [Header("UI Panels")]
    public GameObject idleModePanel;   // 방치형 스테이지 UI 패널
    public GameObject bossModePanel;   // 보스 전투 UI 패널

    [Header("Optional References")]
    public TextMeshProUGUI stageInfoText;

    [Header("Spawner Objects")]
    [Tooltip("일반 몹 스폰용 오브젝트 (EnemySpawner 스크립트가 붙은 GameObject)")]
    public GameObject mobSpawnerObject;
    [Tooltip("보스 스폰용 오브젝트 (BossSpawner 스크립트가 붙은 GameObject)")]
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
        GoToIdleMode();
    }

    /// <summary>
    /// 일반 몹 처치 시 호출 (서브 스테이지 1~9에서)
    /// </summary>
    public void OnEnemyKilled()
    {
        // 만약 현재가 보스 서브 스테이지(10)이면 일반 몹 처치는 무시
        if (currentSubStage == 10)
            return;

        killCount++;
        Debug.Log($"[StageManager] 적 처치: {killCount}/{currentStageData.killGoal} (Sub Stage {currentSubStage}/10)");

        if (killCount >= currentStageData.killGoal)
        {
            killCount = 0;
            currentSubStage++;

            if (currentSubStage < 10)
            {
                // 일반 서브 스테이지 진행: 계속 Idle 모드
                GoToIdleMode();
            }
            else
            {
                // 10번째 서브 스테이지: 보스 등장
                GoToBossMode();
            }
            UpdateStageInfo();
        }
    }

    /// <summary>
    /// 보스 승리 시 호출: 다음 큰 스테이지로 진행
    /// </summary>
    public void OnBossVictory()
    {
        Debug.Log("[StageManager] 보스 승리! 다음 큰 스테이지로 넘어갑니다.");
        currentSubStage = 1;
        killCount = 0;
        currentBigStageIndex++;
        if (currentBigStageIndex >= stages.Length)
        {
            // 예: 모든 큰 스테이지 완료 → 순환하거나 게임 클리어 처리
            currentBigStageIndex = 0;
        }
        currentStageData = stages[currentBigStageIndex];
        UpdateStageInfo();
        GoToIdleMode();
    }

    /// <summary>
    /// 보스 패배 시 호출: 보스 스테이지에 재진입할 수 있도록 (보스 버튼 활성화 등)
    /// </summary>
    public void OnBossDefeat()
    {
        Debug.Log("[StageManager] 보스 패배! 보스 스테이지에 재진입할 수 있도록 준비합니다.");
        // 보스 모드 상태 유지 또는 UI에서 보스 재진입 버튼 활성화 (별도 UI 스크립트로 처리)
    }

    /// <summary>
    /// Idle 모드: 일반 몹 스포너 활성, 보스 스포너 비활성
    /// </summary>
    public void GoToIdleMode()
    {
        Debug.Log("[StageManager] Idle 모드로 전환 (일반 몹 소환).");
        if (idleModePanel != null) idleModePanel.SetActive(true);
        if (bossModePanel != null) bossModePanel.SetActive(false);

        if (mobSpawnerObject != null) mobSpawnerObject.SetActive(true);
        if (bossSpawnerObject != null) bossSpawnerObject.SetActive(false);
    }

    /// <summary>
    /// Boss 모드: 보스 스포너 활성, 일반 몹 스포너 비활성
    /// </summary>
    public void GoToBossMode()
    {
        Debug.Log("[StageManager] Boss 모드로 전환 (보스 소환).");
        if (idleModePanel != null) idleModePanel.SetActive(false);
        if (bossModePanel != null) bossModePanel.SetActive(true);

        if (mobSpawnerObject != null) mobSpawnerObject.SetActive(false);
        if (bossSpawnerObject != null) bossSpawnerObject.SetActive(true);
    }

    private void UpdateStageInfo()
    {
        if (stageInfoText != null && currentStageData != null)
        {
            stageInfoText.text = $"Big Stage: {currentStageData.chapter} - Sub Stage: {currentSubStage}/10";
        }
    }

    /// <summary>
    /// 스포너들이 현재 스테이지 정보를 얻을 수 있도록 StageData 반환
    /// </summary>
    public StageData GetCurrentStageData()
    {
        return currentStageData;
    }
}