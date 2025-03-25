using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GuildMasterPopupManager : MonoBehaviour
{

    public static GuildMasterPopupManager Instance { get; private set; }

    public GameObject GuildMasterCanvas;            // 길드 마스터 전용 canvas
    public TextMeshProUGUI targetMemberNameText;    // 상단에 표시할 길드원 이름 텍스트

    [Header("Buttons")]
    public Button guildMasterAuthorizeButton;   // 권한 부여 버튼
    public Button guildMasterBanButton;         // 추방 버튼
    public Button closeButton;                  // 팝업 닫기 버튼 (옵션)

    // 팝업에서 사용될 "현재 선택된" 길드원 정보
    private GuildMemberData currentSelectedMember;

    public void OpenGuildMasterPopup(GuildMemberData memberData)
    {   // 팝업 활성화 
        currentSelectedMember = memberData;

        if (GuildMasterCanvas != null)
        {
            GuildMasterCanvas.SetActive(true);
            Debug.Log("[GuildMasterPopup] 길드마스터 팝업 열기");
        }

        // 길드원 이름 표시
        if (targetMemberNameText != null && memberData != null)
        {
            targetMemberNameText.text = memberData.userName;
        }

    }
    public void CloseGuildMasterPopup()
    {   // 팝업 비활성화 
        if (GuildMasterCanvas != null)
        {
            GuildMasterCanvas.SetActive(false);
            Debug.Log("[GuildMasterPopup] 길드마스터 팝업 닫기");
        }
        currentSelectedMember = null;
    }

    private void Awake() // Unity의 Awake() 메서드에서 싱글톤 설정
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        // 초기엔 팝업 비활성화
        if (GuildMasterCanvas != null)
            GuildMasterCanvas.SetActive(false);

        // 버튼 이벤트 연결
        if (guildMasterAuthorizeButton != null)
            guildMasterAuthorizeButton.onClick.AddListener(OnClickAuthorize);

        if (guildMasterBanButton != null)
            guildMasterBanButton.onClick.AddListener(OnClickBan);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseGuildMasterPopup);
    }

    /// <summary>
    /// 권한 부여 버튼 클릭
    /// </summary>
    private void OnClickAuthorize()
    {
        if (currentSelectedMember == null)
        {
            Debug.LogWarning("[GuildMasterPopup] 권한 부여 대상이 없습니다.");
            CloseGuildMasterPopup();
            return;
        }

        Debug.Log($"[GuildMasterPopup] 권한 부여 버튼 클릭. 대상: {currentSelectedMember.userName}");

        GuildAuthorizePopupManager.Instance.OpenGuildAuthorizePopup(currentSelectedMember);
        CloseGuildMasterPopup();
    }

    /// <summary>
    /// 추방 버튼 클릭
    /// </summary>
    private void OnClickBan()
    {
        if (currentSelectedMember == null)
        {
            Debug.LogWarning("[GuildMasterPopup] 추방 대상이 없습니다.");
            CloseGuildMasterPopup();
            return;
        }

        Debug.Log($"[GuildMasterPopup] 추방 버튼 클릭. 대상: {currentSelectedMember.userName}");

        // 기존 GuildBanPopupManager를 열어 추방 로직 재사용
        GuildBanPopupManager.Instance.OpenGuildBanPopup(currentSelectedMember);

        // 여기서는 팝업을 닫을지 유지할지 선택 가능
        CloseGuildMasterPopup();
    }

    
}
