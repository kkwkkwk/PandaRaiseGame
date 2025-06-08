using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DungeonRewardManager : MonoBehaviour
{
    public static DungeonRewardManager Instance { get; private set; }

    [Header("Player Stats")]
    [Tooltip("플레이어 체력 스크립트 (OnDeath 이벤트 필요)")]
    [SerializeField] private PlayerStats playerStats;

    [Header("Failure Reward UI")]
    [Tooltip("실패 시 뜨는 보상 패널 전체")]
    [SerializeField] private GameObject rewardPanel;
    [Tooltip("다시 도전 버튼")]
    [SerializeField] private Button retryButton;
    [Tooltip("닫기(X) 버튼")]
    [SerializeField] private Button closeButton;

    [Header("Reward Spawning")]
    [Tooltip("보상 아이템 프리팹 (DungeonRewardController 포함)")]
    [SerializeField] private GameObject rewardPrefab;
    [Tooltip("ScrollView Content 영역")]
    [SerializeField] private Transform contentContainer;

    [Header("Base Failure Rewards")]
    [Tooltip("한 층도 못 깼을 때 지급할 기본 보상 목록")]
    [SerializeField] private List<DungeonStageData.RewardEntry> failureRewards = new List<DungeonStageData.RewardEntry>();

    [Header("Close Target")]
    [Tooltip("닫기 버튼 클릭 시 비활성화할 패널(GameObject)")]
    [SerializeField] private GameObject canvasToClose;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 한 층도 못 깼을 때
        if (playerStats != null)
            playerStats.OnDeath += ShowFailureRewards;
        else
            Debug.LogWarning("[DungeonRewardManager] playerStats가 할당되지 않았습니다.");

        retryButton?.onClick.AddListener(OnRetry);
        closeButton?.onClick.AddListener(OnCloseToMain);

        rewardPanel?.SetActive(false);
    }

    /// <summary>
    /// 한 층도 못 깼을 때 기본 실패 보상 표시
    /// </summary>
    private void ShowFailureRewards()
    {
        DisplayCustomRewards(failureRewards);
    }

    /// <summary>
    /// 던전 스테이지 매니저에서 호출할 때도 사용합니다.
    /// 누적 보상 또는 페일리어 보상 리스트를 받아서 UI에 뿌립니다.
    /// </summary>
    public void DisplayCustomRewards(List<DungeonStageData.RewardEntry> rewards)
    {
        if (rewardPrefab == null || contentContainer == null)
        {
            Debug.LogWarning("[DungeonRewardManager] rewardPrefab 또는 contentContainer가 설정되지 않았습니다.");
            return;
        }

        // 이전 보상 아이템 제거
        foreach (Transform child in contentContainer)
            Destroy(child.gameObject);

        // 새로운 보상 아이템 생성
        foreach (var entry in rewards)
        {
            var go = Instantiate(rewardPrefab, contentContainer);
            var ctrl = go.GetComponent<DungeonRewardController>();
            if (ctrl != null)
                ctrl.Setup(entry.rewardType, entry.amount);
            else
                Debug.LogWarning("[DungeonRewardManager] DungeonRewardController가 없는 프리팹입니다.");
        }

        rewardPanel.SetActive(true);
    }

    /// <summary>
    /// “다시 도전” 버튼
    /// </summary>
    private void OnRetry()
    {
        if (DungeonManager.Instance != null && DungeonManager.Instance.RemainingTickets > 0)
        {
            rewardPanel.SetActive(false);
            DungeonManager.Instance.TryEnterDungeon();
        }
        else
        {
            Debug.LogWarning("[DungeonRewardManager] 티켓 부족 또는 DungeonManager 없음");
        }
    }

    /// <summary>
    /// “닫기(X)” 버튼
    /// </summary>
    private void OnCloseToMain()
    {
        if (canvasToClose != null)
            canvasToClose.SetActive(false);
        else
            Debug.LogWarning("[DungeonRewardManager] canvasToClose가 할당되지 않았습니다.");
    }
}
