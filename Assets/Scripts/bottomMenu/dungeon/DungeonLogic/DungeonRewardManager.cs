using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Globalization;

public class DungeonRewardManager : MonoBehaviour
{
    public static DungeonRewardManager Instance { get; private set; }

    [Header("Player Stats")]
    [Tooltip("플레이어 체력 스크립트 (OnDeath 이벤트 필요)")]
    [SerializeField] private PlayerStats playerStats;

    [Header("Reward UI")]
    [Tooltip("보상 패널 전체")]
    [SerializeField] private GameObject rewardPanel;
    [Tooltip("보상 아이템 프리팹 (DungeonRewardController 포함)")]
    [SerializeField] private GameObject rewardPrefab;
    [Tooltip("ScrollView Content 영역")]
    [SerializeField] private Transform contentContainer;
    [Tooltip("다시 도전 버튼")]
    [SerializeField] private Button retryButton;
    [Tooltip("닫기(X) 버튼")]
    [SerializeField] private Button closeButton;
    [Tooltip("닫기 시 비활성화할 패널")]
    [SerializeField] private GameObject canvasToClose;

    // ㅎㅎ
    private const string fetchRewardsUrl = "https://your-api.example.com/api/GetDungeonRewards";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 실패 시 보상 요청
        if (playerStats != null)
            playerStats.OnDeath += () =>
                RequestAndDisplayRewards(
                    DungeonStageManager.Instance.CurrentFloor,
                    /*cleared*/ false,
                    /*isSweep*/    false   // ← 이 값 추가
                );
        else
            Debug.LogWarning("[DungeonRewardManager] playerStats 미할당");

        retryButton?.onClick.AddListener(OnRetry);
        closeButton?.onClick.AddListener(OnClosePanel);

        rewardPanel?.SetActive(false);
    }

    /// <summary>
    /// 던전 매니저에서 호출:
    /// floor: 대상 던전 층, hasCleared: 보스 처치 여부
    /// </summary>
    public void RequestAndDisplayRewards(int floor, bool hasCleared, bool isSweep)
    {
        StartCoroutine(FetchRewardsCoroutine(floor, hasCleared, isSweep));
    }

    private IEnumerator FetchRewardsCoroutine(int floor, bool hasCleared, bool isSweep)
    {
        // 1) 요청 페이로드 생성
        var payload = new DungeonRequestData
        {
            PlayFabId = GlobalData.playFabId,
            Floor = floor,
            Cleared = hasCleared,
            IsSweep = isSweep
        };
        string json = JsonUtility.ToJson(payload);

        // 2) HTTP POST 준비
        var req = new UnityWebRequest(fetchRewardsUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        // 3) 요청 전송
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[DungeonRewardManager] 보상 요청 실패: {req.error}");
            yield break;
        }

        // 4) JSON 파싱
        DungeonRewardsData dto;
        try
        {
            dto = JsonConvert.DeserializeObject<DungeonRewardsData>(req.downloadHandler.text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DungeonRewardManager] JSON 파싱 오류: {ex.Message}");
            yield break;
        }

        if (dto.Rewards == null || dto.Rewards.Count == 0)
        {
            Debug.LogWarning("[DungeonRewardManager] 서버 보상 데이터 없음");
            yield break;
        }

        // 5) UI에 표시
        DisplayDtoRewards(dto.Rewards);
    }

    private void DisplayDtoRewards(List<RewardItemData> items)
    {
        // 기존 보상 아이템 제거
        foreach (Transform child in contentContainer)
            Destroy(child.gameObject);

        // DTO 리스트를 직접 UI에 뿌리기
        foreach (var item in items)
        {
            var go = Instantiate(rewardPrefab, contentContainer);
            var ctrl = go.GetComponent<DungeonRewardController>();
            if (ctrl != null)
                ctrl.Setup(item.RewardType, item.Amount);
            else
                Debug.LogWarning("[DungeonRewardManager] DungeonRewardController 누락");
        }

        // 패널 활성화
        rewardPanel.SetActive(true);
    }

    private void OnRetry()
    {
        if (DungeonManager.Instance != null && DungeonManager.Instance.RemainingTickets > 0)
        {
            rewardPanel.SetActive(false);
            DungeonManager.Instance.TryEnterDungeon();
        }
        else
        {
            Debug.LogWarning("[DungeonRewardManager] 티켓 부족 또는 무효 상태");
        }
    }

    private void OnClosePanel()
    {
        if (canvasToClose != null)
            canvasToClose.SetActive(false);
        else
            Debug.LogWarning("[DungeonRewardManager] canvasToClose 미할당");
    }
}
