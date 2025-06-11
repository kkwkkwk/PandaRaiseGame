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
    [Tooltip("티켓 수량을 표시할 패널(GameObject, 내부에 Text 컴포넌트 포함)")]
    public GameObject ticketPanel;

    [Header("팝업 UI")]
    [Tooltip("메시지를 띄울 팝업 패널 (내부에 Text 컴포넌트 포함)")]
    public GameObject popupPanel;

    [Header("던전 입장 버튼")]
    [Tooltip("인스펙터에서 연결하세요")]
    public Button enterDungeonButton;

    [Header("닫기 버튼 (X)")]
    [Tooltip("던전 입장 패널을 닫기 위한 X 버튼")]
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

        if (closeDungeonButton != null)
            closeDungeonButton.onClick.AddListener(OnCloseDungeon);
    }

    #region Initialization
    /// <summary>
    /// 게임 시작 시 한 번만 호출해서 던전 정보를 서버에서 가져옵니다.
    /// </summary>
    public IEnumerator StartSequence()
    {
        yield return FetchDungeonDataCoroutine();
        UpdateTicketUI();
    }

    private IEnumerator FetchDungeonDataCoroutine()
    {
        // DTO를 사용한 요청 바디 생성
        var requestData = new DungeonRequestData
        {
            PlayFabId = GlobalData.playFabId
        };
        string jsonBody = JsonConvert.SerializeObject(requestData);

        var req = new UnityWebRequest(fetchDungeonUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
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

        if (resp == null || !resp.IsSuccess)
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
            PlayFabId = GlobalData.playFabId
            // 서버 API는 highestClearedFloor 파라미터를 기존 URL 쿼리로 받아 처리합니다.
        };
        string jsonBody = JsonConvert.SerializeObject(requestData);

        var req = new UnityWebRequest(updateFloorUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            Debug.LogError($"[DungeonManager] UpdateFloor 실패: {req.error}");
        else
            Debug.Log("[DungeonManager] 최고 층수 갱신 성공");
    }
    #endregion

    #region Dungeon Entry
    /// <summary>
    /// 던전 입장 버튼에서 호출
    /// </summary>
    public void TryEnterDungeon()
    {
        if (RemainingTickets <= 0)
        {
            ShowPopup("입장에 필요한 티켓이 부족합니다", 2f);
            return;
        }

        RemainingTickets--;
        UpdateTicketUI();

        DungeonStageManager.Instance.StartDungeon(MaxClearedFloor + 1);
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
        if (dungeonPopupCanvas != null)
            dungeonPopupCanvas.SetActive(false);
        else
            Debug.LogWarning("[DungeonManager] dungeonPopupCanvas가 할당되지 않았습니다.");
    }
    #endregion
}
