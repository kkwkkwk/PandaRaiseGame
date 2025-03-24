using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;

public class GuildBanPopupManager : MonoBehaviour
{

    public static GuildBanPopupManager Instance { get; private set; }

    public GameObject GuildBanCanvas;                       // 길드 ban canvas
    public TextMeshProUGUI guildMemberNameText;             // 상단에 길드원 이름을 표시할 텍스트
    public Button guildBanAgreeButton;                      // "예" 버튼
    public Button guildBanDisAgreeButton;                   // "아니오" 버튼

    // 서버 방출 API
    private string kickGuildMemberURL = "https://your-azure-function-url/KickGuildMember?code=YOUR_FUNCTION_KEY";

    // 팝업에서 사용될 "현재 선택된" 길드원 정보
    private GuildMemberData currentSelectedMember;

    /// <summary>
    /// 길드 추방 팝업 열기 + 길드원 정보 세팅
    /// </summary>
    public void OpenGuildBanPopup(GuildMemberData memberData)
    {
        currentSelectedMember = memberData; // 현재 팝업에서 방출할 길드원 정보 저장

        if (GuildBanCanvas != null)
        {
            GuildBanCanvas.SetActive(true);
            Debug.Log("길드 추방 창 열기");
        }

        // 상단 텍스트에 길드원 이름 표시
        if (guildMemberNameText != null && memberData != null)
        {
            guildMemberNameText.text = memberData.userName;
        }
    }
    /// <summary>
    /// 길드 추방 팝업 닫기
    /// </summary>
    public void CloseGuildBanPopup()
    {
        if (GuildBanCanvas != null)
        {
            GuildBanCanvas.SetActive(false);
            Debug.Log("길드 추방 창 닫기");
        }

        currentSelectedMember = null; // 선택된 멤버 정보 해제
    }

    private void Awake() // Unity의 Awake() 메서드에서 싱글톤 설정
    {
        // 만약 Instance가 비어 있으면, 이 스크립트를 싱글톤 인스턴스로 설정
        if (Instance == null)
        {
            Instance = this; // 현재 객체를 싱글톤으로 지정
        }
        else
        {
            // 이미 싱글톤 인스턴스가 존재하면, 중복된 객체를 제거
            Destroy(gameObject);
        }
    }
    // Start is called before the first frame update
    void Start()
    { // 처음에 팝업 패널 비활성화 
        if (GuildBanCanvas != null)
        {
            GuildBanCanvas.SetActive(false);
        }
        if (guildBanAgreeButton != null)
            guildBanAgreeButton.onClick.AddListener(OnClickYesBan);

        if (guildBanDisAgreeButton != null)
            guildBanDisAgreeButton.onClick.AddListener(OnClickNoBan);
    }

    /// <summary>
    /// "예" 버튼 클릭 시 -> 서버에 방출 요청
    /// </summary>
    private void OnClickYesBan()
    {
        if (currentSelectedMember == null)
        {
            Debug.LogWarning("방출할 길드원 정보가 없습니다.");
            CloseGuildBanPopup();
            return;
        }

        // 예: 플레이팹 아이디 + 방출 대상 정보(JSON)로 전송
        StartCoroutine(CallKickGuildMemberCoroutine(currentSelectedMember));
    }

    /// <summary>
    /// "아니오" 버튼 클릭 시 -> 팝업 닫기
    /// </summary>
    private void OnClickNoBan()
    {
        CloseGuildBanPopup();
    }

    /// <summary>
    /// 서버에 길드원 방출 요청을 보내는 코루틴
    /// </summary>
    private IEnumerator CallKickGuildMemberCoroutine(GuildMemberData memberData)
    {
        // (예) 서버가 "kickGuildMemberURL"로 POST를 받으며,
        // JSON { "playFabId":"방출요청자", "targetId":"방출대상아이디" } 등을 요구한다고 가정
        // 여기서는 "userName"으로 식별하기보다는 "playFabId"가 있어야 더 정확함
        // 편의상 GuildMemberData에 "targetPlayFabId"가 있다고 가정하거나, userName으로 대체
        // 아래는 예시로 userName만 전송
        string jsonData = $"{{\"requestorId\":\"{GlobalData.playFabId}\",\"targetName\":\"{memberData.userName}\"}}";
        Debug.Log("[CallKickGuildMemberCoroutine] 요청 JSON: " + jsonData);

        UnityWebRequest request = new UnityWebRequest(kickGuildMemberURL, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[CallKickGuildMemberCoroutine] 서버 요청 실패: {request.error}");
            // 실패 안내 등
        }
        else
        {
            string responseJson = request.downloadHandler.text;
            Debug.Log($"[CallKickGuildMemberCoroutine] 방출 요청 성공, 응답: {responseJson}");

            // TODO: 응답 파싱 -> 성공 시
            // ex: KickGuildMemberResponse res = JsonConvert.DeserializeObject<KickGuildMemberResponse>(responseJson);
            // if (res.success) { ... }

            // 팝업 닫기 + 길드원 목록 갱신
            // 방출 성공 시 -> 로컬 리스트에서 제거
            GuildTabPanelController.Instance.RemoveMemberAndRefresh(memberData);
            ToastManager.Instance.ShowToast("해당 길드원은 방출되었습니다.");
            CloseGuildBanPopup();
            // 길드 정보 재조회 (ex: GuildTabPanelController.Instance.RefreshGuildData())
        }
    }

    // (선택) 응답 DTO
    [System.Serializable]
    public class KickGuildMemberResponse
    {
        public bool success;
        public string message;
    }
}

