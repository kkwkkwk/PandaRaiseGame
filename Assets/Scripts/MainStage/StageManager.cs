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

    [Header("Monster Object")] // 몬스터 활성화/비활성화 용
    [Tooltip("몬스터 스폰 관리 오브젝트 (Idle용)")]
    public GameObject mobSpawnerObject;

    [Tooltip("보스 오브젝트(또는 보스 스크립트)")]
    public GameObject bossObject;

    void Start()
    {
        // (1) 처음에 보스전 패널을 비활성화
        if (bossModePanel != null)
        {
            bossModePanel.SetActive(false);
        }

        // 씬 시작 시, Idle 모드로 들어가고 스테이지 정보 갱신
        GoToIdleMode();
        UpdateStageInfo();
    }

    /// <summary>
    /// 방치형 모드로 전환
    /// </summary>
    public void GoToIdleMode()
    {
        Debug.Log("[StageManager] 방치형(Idle) 모드로 전환합니다.");

        // Idle 모드 On, 보스 모드 Off
        if (idleModePanel != null) idleModePanel.SetActive(true);
        if (bossModePanel != null) bossModePanel.SetActive(false);

        // 몬스터 스폰 켜기
        if (mobSpawnerObject != null) mobSpawnerObject.SetActive(true);

        // 보스 오브젝트(또는 AI) 끄기
        if (bossObject != null) bossObject.SetActive(false);
    }

    /// <summary>
    /// 보스 모드로 전환
    /// </summary>
    public void GoToBossMode()
    {
        Debug.Log("[StageManager] 보스 모드로 전환합니다.");

        // 방치형 모드 Off, 보스 모드 On
        if (idleModePanel != null) idleModePanel.SetActive(false);
        if (bossModePanel != null) bossModePanel.SetActive(true);

        // 몬스터 스폰 끄기
        if (mobSpawnerObject != null) mobSpawnerObject.SetActive(false);

        // 보스 오브젝트(또는 AI) 켜기
        if (bossObject != null) bossObject.SetActive(true);
    }

    /// <summary>
    /// 보스전 승리 시 호출. 스테이지(1-x)에서 x+1로 업데이트하고 방치 모드로 복귀
    /// </summary>
    public void OnBossVictory()
    {
        Debug.Log("[StageManager] 보스 승리! 다음 스테이지로 넘어갑니다.");

        // (3) 스테이지를 올려서 다음 보스 도전
        stageIndex++;
        UpdateStageInfo();

        // 일단 Idle로 복귀 (원하면 바로 BossMode 유지도 가능)
        GoToIdleMode();
    }

    /// <summary>
    /// 보스전 패배 시 호출. 스테이지 그대로, Idle 모드로 복귀
    /// </summary>
    public void OnBossDefeat()
    {
        Debug.Log("[StageManager] 보스 패배! 방치형 모드로 복귀해서 골드 파밍 가능.");
        GoToIdleMode();
    }

    /// <summary>
    /// (2) 스테이지 정보 텍스트 업데이트
    /// 예: 'Stage: 1-1', 'Stage: 1-2' 형식으로 표시
    /// </summary>
    private void UpdateStageInfo()
    {
        if (stageInfoText != null)
        {
            stageInfoText.text = $"Stage: {chapter}-{stageIndex}";
        }
    }
}