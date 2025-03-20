using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StageManager : MonoBehaviour
{
    [Header("Stage Info")]
    [Tooltip("현재 챕터 번호 (예: 1), 현재 스테이지 번호 (예: 1 => 1-1)")]
    public int chapter = 1;          // 1 => '1-x'
    public int stageIndex = 1;       // x => '1-x'

    [Header("UI Panels")]
    [Tooltip("방치형 필드 UI/오브젝트 최상위 (Idle 모드)")]
    public GameObject idleModePanel;

    [Tooltip("보스 전투 UI/오브젝트 최상위 (Boss 모드)")]
    public GameObject bossModePanel;

    [Header("Optional References")]
    [Tooltip("스테이지 표시용 텍스트")]
    public TextMeshProUGUI stageInfoText;

    [Header("Monster Object")]
    [Tooltip("몬스터 스폰 관리 오브젝트 (Idle용) - 기존 mobSpawnerObject (참고용)")]
    public GameObject mobSpawnerObject;

    [Tooltip("보스 오브젝트(또는 보스 스크립트)")]
    public GameObject bossObject;

    [Header("Enemy Spawner Settings")]
    [Tooltip("Enemy Spawner Prefab (캐릭터 프리팹, EnemySpawner 스크립트 포함, 기본 isSpawner=false)")]
    public GameObject enemyPrefab;

    // enemyPrefab을 이용해 생성한 스포너 인스턴스가 이미 생성되었는지 여부
    private bool enemySpawnerInstantiated = false;

    void Start()
    {
        // 보스 모드 패널 비활성화
        if (bossModePanel != null)
        {
            bossModePanel.SetActive(false);
        }

        // 씬 시작 시 Idle 모드로 전환 및 스테이지 정보 갱신
        GoToIdleMode();
        UpdateStageInfo();
    }

    /// <summary>
    /// 방치형 모드로 전환하고, 스테이지 시작 시 enemyPrefab을 소환(스포너 활성화)함.
    /// </summary>
    public void GoToIdleMode()
    {
        Debug.Log("[StageManager] 방치형(Idle) 모드로 전환합니다.");

        // Idle 모드 On, Boss 모드 Off
        if (idleModePanel != null) idleModePanel.SetActive(true);
        if (bossModePanel != null) bossModePanel.SetActive(false);

        // 기존 mobSpawnerObject 활성화 (필요시)
        if (mobSpawnerObject != null) mobSpawnerObject.SetActive(true);

        // 보스 오브젝트 끄기
        if (bossObject != null) bossObject.SetActive(false);
    }


    /// <summary>
    /// 보스 모드로 전환
    /// </summary>
    public void GoToBossMode()
    {
        Debug.Log("[StageManager] 보스 모드로 전환합니다.");
        if (idleModePanel != null) idleModePanel.SetActive(false);
        if (bossModePanel != null) bossModePanel.SetActive(true);
        if (mobSpawnerObject != null) mobSpawnerObject.SetActive(false);
        if (bossObject != null) bossObject.SetActive(true);
    }

    /// <summary>
    /// 보스전 승리 시 호출. 스테이지 정보를 갱신하고 Idle 모드로 전환
    /// </summary>
    public void OnBossVictory()
    {
        Debug.Log("[StageManager] 보스 승리! 다음 스테이지로 넘어갑니다.");
        stageIndex++;
        UpdateStageInfo();
        GoToIdleMode();
    }

    /// <summary>
    /// 보스전 패배 시 호출. 현재 스테이지를 유지하며 Idle 모드로 복귀
    /// </summary>
    public void OnBossDefeat()
    {
        Debug.Log("[StageManager] 보스 패배! 방치형 모드로 복귀합니다.");
        GoToIdleMode();
    }

    /// <summary>
    /// 스테이지 정보 텍스트를 업데이트 (예: 'Stage: 1-1')
    /// </summary>
    private void UpdateStageInfo()
    {
        if (stageInfoText != null)
        {
            stageInfoText.text = $"Stage: {chapter}-{stageIndex}";
        }
    }
}
