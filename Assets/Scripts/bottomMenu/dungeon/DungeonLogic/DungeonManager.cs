using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Newtonsoft.Json;

public class DungeonManager : MonoBehaviour
{
    public static DungeonManager Instance { get; private set; }

    #region UI References
    [Header("티켓 표시 UI")]
    public GameObject ticketPanel;
    [Header("팝업 UI")]
    public GameObject popupPanel;
    [Header("던전 입장 버튼")]
    public Button enterDungeonButton;
    [Header("소탕 버튼")]
    public Button sweepButton;
    [Header("닫기 버튼 (X)")]
    public Button closeDungeonButton;
    [Tooltip("X 버튼 클릭 시 꺼질 던전 팝업 전체 캔버스")]
    public GameObject dungeonPopupCanvas;
    #endregion

    private const string fetchDungeonUrl = "https://pandaraisegame-dungeon.azurewebsites.net/api/GetDungeonData?code=Fwrfo8c9NGk2KKfDt6JMP8ijqTjtQfB4piDmJghkBm3xAzFu2t3q1w==";
    private const string updateFloorUrl = "https://pandaraisegame-dungeon.azurewebsites.net/api/UpdateClearedFloor?code=CQXeXsaz7lKJgKMBt7DyfzViOiCzDYi4TpGnVNOnIM-mAzFu-6M2bw==";

    public int MaxClearedFloor { get; private set; }
    public int RemainingTickets { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        popupPanel?.SetActive(false);

        if (enterDungeonButton != null)
            enterDungeonButton.onClick.AddListener(TryEnterDungeon);

        if (sweepButton != null)
            sweepButton.onClick.AddListener(OnSweep);

        if (closeDungeonButton != null)
            closeDungeonButton.onClick.AddListener(OnCloseDungeon);
    }

    #region Initialization
    public IEnumerator StartSequence()
    {
        yield return FetchDungeonDataCoroutine();
        UpdateTicketUI();
    }

    private IEnumerator FetchDungeonDataCoroutine()
    {
        var requestData = new DungeonRequestData
        {
            PlayFabId = GlobalData.playFabId,
            Floor = 0,
            Cleared = false,
            IsSweep = false
        };
        string jsonBody = JsonConvert.SerializeObject(requestData);

        var req = new UnityWebRequest(fetchDungeonUrl, "POST")
        {
            uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonBody)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[DungeonManager] Fetch 실패: {req.error}");
            yield break;
        }

        DungeonResponseData resp;
        try
        {
            resp = JsonConvert.DeserializeObject<DungeonResponseData>(req.downloadHandler.text);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DungeonManager] JSON 파싱 오류: {ex.Message}");
            yield break;
        }

        if (!resp.IsSuccess)
        {
            Debug.LogWarning("[DungeonManager] 서버 응답 이상");
            yield break;
        }

        MaxClearedFloor = resp.HighestClearedFloor;
        RemainingTickets = resp.TicketCount;
        Debug.Log($"[DungeonManager] Floor={MaxClearedFloor}, Tickets={RemainingTickets}");
    }

    public IEnumerator UpdateClearedFloorCoroutine(int newClearedFloor)
    {
        MaxClearedFloor = newClearedFloor;

        var requestData = new DungeonRequestData
        {
            PlayFabId = GlobalData.playFabId,
            Floor = newClearedFloor,
            Cleared = true,
            IsSweep = false
        };
        string jsonBody = JsonConvert.SerializeObject(requestData);

        var req = new UnityWebRequest(updateFloorUrl, "POST")
        {
            uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonBody)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            Debug.LogError($"[DungeonManager] UpdateFloor 실패: {req.error}");
        else
            Debug.Log("[DungeonManager] 최고 층수 갱신 성공");
    }
    #endregion

    #region Dungeon Entry
    public void TryEnterDungeon()
    {
        if (RemainingTickets <= 0)
        {
            ShowPopup("입장에 필요한 티켓이 부족합니다", 2f);
            return;
        }

        RemainingTickets--;
        UpdateTicketUI();

        // 마지막으로 못 깬 층으로 도전
        DungeonStageManager.Instance.StartDungeon(MaxClearedFloor + 1);
    }

    private void OnSweep()
    {
        if (RemainingTickets <= 0)
        {
            ShowPopup("소탕에 필요한 티켓이 부족합니다", 2f);
            return;
        }

        RemainingTickets--;
        UpdateTicketUI();

        // 소탕 요청: 마지막으로 깬 층, 클리어=true, isSweep=true
        DungeonRewardManager.Instance.RequestAndDisplayRewards(
            MaxClearedFloor, true, true
        );

        dungeonPopupCanvas?.SetActive(false);
    }
    #endregion

    #region UI Helpers
    private void UpdateTicketUI()
    {
        if (ticketPanel == null) return;
        var txt = ticketPanel.GetComponentInChildren<Text>();
        if (txt != null)
            txt.text = $"Ticket: {RemainingTickets}";
    }

    private void ShowPopup(string message, float duration)
    {
        if (popupPanel == null) return;
        var msg = popupPanel.GetComponentInChildren<Text>();
        if (msg != null) msg.text = message;
        popupPanel.SetActive(true);
        StartCoroutine(HidePopupAfter(duration));
    }

    private IEnumerator HidePopupAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        popupPanel.SetActive(false);
    }
    #endregion

    #region Close Dungeon
    private void OnCloseDungeon()
    {
        dungeonPopupCanvas?.SetActive(false);
    }
    #endregion
}
