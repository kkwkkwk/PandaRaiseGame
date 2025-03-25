using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;

public class GuildAuthorizePopupManager : MonoBehaviour
{
    public static GuildAuthorizePopupManager Instance { get; private set; }

    [Header("Popup Canvas")]
    public GameObject GuildAuthorizeCanvas;     // 길드 권한 부여 팝업 Canvas

    [Header("UI References")]
    public TextMeshProUGUI targetMemberNameText;    // 상단에 길드원 이름 표시
    public TMP_Dropdown roleDropdown;               // 새 계급 선택 Dropdown
    public Button authorizeButton;                  // 계급 부여 버튼
    public Button closeButton;                      // 닫기 버튼

    // 서버 계급 변경 API (예시 URL)
    private string changeRoleURL = "https://your-azure-function-url/ChangeGuildRole?code=YOUR_FUNCTION_KEY";

    // 현재 팝업에서 조작 중인 길드원
    private GuildMemberData currentSelectedMember;

    // 길드마스터(나) 자신이 PlayFabId or entityId (본인을 '길드원'으로 바꾸거나 등 처리 시 필요)
    // 필요하다면 GuildMasterPopupManager에서 같이 넘겨받거나, GuildTabPanelController에서 static하게 참조
    // 여기서는 간단히 GlobalData.playFabId만 사용 예시
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 팝업 초기 비활성
        if (GuildAuthorizeCanvas != null)
        {
            GuildAuthorizeCanvas.SetActive(false);
        }

        // 버튼 이벤트 연결
        if (authorizeButton != null)
            authorizeButton.onClick.AddListener(OnClickAuthorize);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseGuildAuthorizePopup);
    }

    /// <summary>
    /// 길드 권한 부여 팝업 열기
    /// </summary>
    public void OpenGuildAuthorizePopup(GuildMemberData memberData)
    {
        currentSelectedMember = memberData;

        if (GuildAuthorizeCanvas != null)
        {
            GuildAuthorizeCanvas.SetActive(true);
            Debug.Log("[GuildAuthorizePopup] 열기");
        }

        // 상단 이름 표시
        if (targetMemberNameText != null && memberData != null)
        {
            targetMemberNameText.text = memberData.userName;
        }

        // Dropdown 초기화 (예시: 0=길드원, 1=부마스터, 2=길드마스터)
        if (roleDropdown != null)
        {
            // 필요하면 ClearOptions() 후 새로 옵션을 넣거나,
            // Inspector에서 미리 "길드원", "부마스터", "길드마스터"를 셋업해둘 수 있음.
            roleDropdown.value = 0;
        }
    }

    /// <summary>
    /// 팝업 닫기
    /// </summary>
    public void CloseGuildAuthorizePopup()
    {
        if (GuildAuthorizeCanvas != null)
        {
            GuildAuthorizeCanvas.SetActive(false);
            Debug.Log("[GuildAuthorizePopup] 닫기");
        }
        currentSelectedMember = null;
    }

    /// <summary>
    /// "계급 부여" 버튼 클릭
    /// </summary>
    private void OnClickAuthorize()
    {
        if (currentSelectedMember == null)
        {
            Debug.LogWarning("[GuildAuthorizePopup] 대상 길드원 정보 없음");
            CloseGuildAuthorizePopup();
            return;
        }

        // Dropdown에서 선택된 계급
        string newRole = GetSelectedRole();
        Debug.Log($"[GuildAuthorizePopup] 계급 부여 버튼 클릭. 대상={currentSelectedMember.userName}, 새 계급={newRole}");

        // 서버에 '계급 변경' 요청
        StartCoroutine(CallChangeRoleCoroutine(currentSelectedMember, newRole));
    }

    /// <summary>
    /// Dropdown 인덱스를 보고 계급 문자열을 반환 (예시)
    /// </summary>
    private string GetSelectedRole()
    {
        if (roleDropdown == null) return "길드원";

        int idx = roleDropdown.value; // 0=길드원,1=부마스터,2=길드마스터 (Inspector에서 Options 설정)
        switch (idx)
        {
            case 1: return "부마스터";
            case 2: return "길드마스터";
            default: return "길드원";
        }
    }

    /// <summary>
    /// 서버에 '계급 변경' 요청을 보내는 코루틴
    /// </summary>
    private IEnumerator CallChangeRoleCoroutine(GuildMemberData memberData, string newRole)
    {
        // 예: 서버에서 "requestorId"=본인, "targetName"=memberData.userName, "newRole"=newRole
        // 만약 '길드마스터'로 바꾸면, 서버 측 로직에서 "본인=길드원" 처리도 해줌
        string jsonData = $"{{\"requestorId\":\"{GlobalData.entityToken.Entity.Id}\",\"targetId\":\"{memberData.entityId}\",\"newRole\":\"{newRole}\"}}";


        Debug.Log("[CallChangeRoleCoroutine] " + jsonData);

        UnityWebRequest request = new UnityWebRequest(changeRoleURL, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[CallChangeRoleCoroutine] 서버 요청 실패: " + request.error);
            // 실패 안내
        }
        else
        {
            string responseJson = request.downloadHandler.text;
            Debug.Log($"[CallChangeRoleCoroutine] 계급 변경 요청 성공, 응답: {responseJson}");

            // TODO: 응답 파싱 -> 성공 여부 판단
            // if (res.success) { ... }

            // 서버 재조회
            GuildTabPanelController.Instance.RefreshGuildData();
            // 팝업 닫기
            CloseGuildAuthorizePopup();

            // 갱신 방법:
            
        }
    }
}
