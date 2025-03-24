using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;

public class GuildInfoPanelController : MonoBehaviour
{
    [Header("Guild Basic Info")]
    public TextMeshProUGUI guildNameText;       // 길드 이름 출력
    public TextMeshProUGUI guildLevelText;      // 길드 레벨 출력
    public TextMeshProUGUI guildDescText;       // 길드 소개 출력

    [Header("Buttons")]
    public Button attendanceButton;     // 길드 출석 버튼
    public Button manageButton;         // 길드 관리 버튼
    public Button leaveButton;          // 길드 탈퇴 버튼

    [Header("Server Info")]
    // 길드 출석 API
    public string guildAttendanceURL = "https://your-azure-function-url/GuildAttendance?code=YOUR_KEY";

    // 간단하게, “오늘 출석했는지” 여부를 저장 (실제로는 서버 로직으로 체크하는 게 안전)
    private bool hasAttendedToday = false;

    // 길드마스터/부마스터인지 여부 (GuildTabPanelController 등에서 받아오거나, 로그인 시점에 저장)
    // 실제로는 userClass(“길드마스터” / “부마스터” / “길드원”) 등을 받아서 판단할 수도 있음.
    public bool isMasterOrSub = false;

    private void Start()
    {
        // 버튼 클릭 이벤트 연결
        if (attendanceButton != null)
            attendanceButton.onClick.AddListener(OnClickAttendance);

        if (manageButton != null)
            manageButton.onClick.AddListener(OnClickManage);

        if (leaveButton != null)
            leaveButton.onClick.AddListener(OnClickLeave);
    }

    /// <summary>
    /// 길드 이름, 레벨, 소개 등을 UI에 반영
    /// </summary>
    public void SetGuildInfo(string guildName, int guildLevel, string guildDesc)
    {
        if (guildNameText != null)
            guildNameText.text = guildName;

        if (guildLevelText != null)
            guildLevelText.text = "레벨 : " + guildLevel;

        if (guildDescText != null)
            guildDescText.text = guildDesc;
    }

    /// <summary>
    /// 길드 출석 버튼 클릭
    /// </summary>
    private void OnClickAttendance()
    {
        // 이미 출석했다면 토스트만 띄우고 막기
        if (hasAttendedToday)
        {
            ToastManager.Instance.ShowToast("오늘은 이미 출석하셨습니다!");
            return;
        }

        // 아직 출석하지 않았다면 서버에 출석 요청
        StartCoroutine(CallGuildAttendanceCoroutine());
    }

    /// <summary>
    /// 길드 출석 API 호출 코루틴
    /// </summary>
    private IEnumerator CallGuildAttendanceCoroutine()
    {
        // 서버에 보낼 JSON
        // 실제로는 playFabId, guildId 등 필요한 정보를 함께 전송
        string jsonData = $"{{\"playFabId\":\"{GlobalData.playFabId}\"}}";
        Debug.Log("[GuildInfoPanel] 출석 요청: " + jsonData);

        UnityWebRequest request = new UnityWebRequest(guildAttendanceURL, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[GuildInfoPanel] 출석 요청 실패: {request.error}");
            ToastManager.Instance.ShowToast("출석 요청에 실패했습니다. 잠시 후 다시 시도해주세요.");
        }
        else
        {
            string responseJson = request.downloadHandler.text;
            Debug.Log("[GuildInfoPanel] 출석 요청 성공, 응답: " + responseJson);

            // (선택) 서버 응답 파싱해서 success/fail 판단
            // GuildAttendanceResponse res = JsonConvert.DeserializeObject<GuildAttendanceResponse>(responseJson);
            // if (res.success) { ... }

            // 일단은 성공이라고 가정
            hasAttendedToday = true;
            ToastManager.Instance.ShowToast("출석이 완료되었습니다!");
        }
    }

    /// <summary>
    /// 길드 관리 버튼 클릭 (길드마스터/부마스터 전용)
    /// </summary>
    private void OnClickManage()
    {
        // 권한 체크
        if (!isMasterOrSub)
        {
            ToastManager.Instance.ShowToast("길드 관리 권한이 없습니다!");
            return;
        }

        // 권한이 있다면 “길드 관리” 팝업 등 새 창을 열 수 있음
        // 예: GuildManagePopupManager.Instance.OpenManagePopup();
        Debug.Log("[GuildInfoPanel] 길드 관리 창 열기");
    }

    /// <summary>
    /// 길드 탈퇴 버튼 클릭
    /// </summary>
    private void OnClickLeave()
    {
        // 탈퇴 여부를 묻는 팝업
        // 예: GuildLeavePopupManager.Instance.OpenLeavePopup();
        Debug.Log("[GuildInfoPanel] 길드 탈퇴 팝업 열기");
    }
}
