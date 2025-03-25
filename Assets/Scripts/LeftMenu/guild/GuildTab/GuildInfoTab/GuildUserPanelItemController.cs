using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GuildUserPanelItemController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI userNameText;
    public TextMeshProUGUI userClassText;
    public TextMeshProUGUI userPowerText;
    public Image userConnectionStatusImage;
    public Button guildUserSettingIcon;        // 왼쪽 흰색 사각형에 해당하는 Image 컴포넌트

    private GuildMemberData currentMemberData;

    [Header("Connection Status Sprites")]
    public Sprite onlineSprite;
    public Sprite offlineSprite;

    [Header("Setting Icon (For Guild Master / Sub Master)")]
    public Sprite guildUserSettingIcon_sprite;  // 왼쪽 흰색 정사각형 부분(아이콘)을 담은 오브젝트

    private void Awake()
    {
        onlineSprite = Resources.Load<Sprite>("Sprites/Guild/OnlineStatus");
        offlineSprite = Resources.Load<Sprite>("Sprites/Guild/OfflineStatus");
        guildUserSettingIcon_sprite = Resources.Load<Sprite>("Sprites/Guild/setting_icon");

        if (onlineSprite == null)
            Debug.LogError("Online sprite 로드 실패");
        if (offlineSprite == null)
            Debug.LogError("Offline sprite 로드 실패");
        if (guildUserSettingIcon_sprite == null)
            Debug.LogError("guild user setting sprite 로드 실패");
    }

    /// <summary>
    /// GuildMemberData를 받아와 Prefab UI 요소에 적용
    /// </summary>
    public void SetData(GuildMemberData memberData, string myUserRole)
    {
        currentMemberData = memberData;

        Debug.Log($"[GuildUserPanelItemController] SetData() => myUserRole={myUserRole}, rowUserName={memberData.userName}, rowRole={memberData.userClass}");

        if (userNameText)   userNameText.text = memberData.userName;
        if (userClassText)  userClassText.text = memberData.userClass;
        if (userPowerText)  userPowerText.text = memberData.userPower.ToString();

        if (userConnectionStatusImage != null)
            userConnectionStatusImage.sprite = memberData.isOnline ? onlineSprite : offlineSprite;


        // (1) 기본적으로 아이콘은 안 보이도록
        bool showIcon = false;

        // 2) 로직: 
        //    - 길드마스터: 모든 구성원에게 아이콘 표시 (단, ‘내 자신’을 제외하고 싶다면 entityId 비교로 제외)
        //    - 부마스터: 일반 길드원만
        //    - 일반: 표시 X
        if (myUserRole == "길드마스터")
        {
            Debug.Log($"[GuildUserPanelItemController] I am GuildMaster. rowUserClass={memberData.userClass}");
            if (memberData.userClass == "길드마스터")
                showIcon = false;
            else
                showIcon = true;
        }
        else if (myUserRole == "부마스터")
        {
            Debug.Log("[GuildUserPanelItemController] I am SubMaster => 일반 길드원만 추방 가능");
            if (memberData.userClass == "길드원")
                showIcon = true;
            else
                showIcon = false;
        }
        guildUserSettingIcon.gameObject.SetActive(showIcon);

        var iconImage = guildUserSettingIcon.GetComponent<Image>();
        if (iconImage != null && guildUserSettingIcon_sprite != null)
        {
            iconImage.sprite = guildUserSettingIcon_sprite;
        }

        guildUserSettingIcon.onClick.RemoveAllListeners();
        guildUserSettingIcon.onClick.AddListener(() => OnClickSettingIcon(myUserRole));
    }
    /// <summary>
    /// 설정 아이콘 클릭 시 호출 – 로그인 유저의 역할에 따라 다른 팝업을 오픈
    /// </summary>
    /// <param name="myUserRole">로그인 유저의 역할</param>
    private void OnClickSettingIcon(string myUserRole)
    {
        Debug.Log($"[GuildUserPanelItemController] OnClickSettingIcon => myUserRole={myUserRole}, rowUserClass={currentMemberData.userClass}");
        if (myUserRole == "길드마스터")
        {
            // 어떤 유저를 클릭해도 GuildMasterPopup으로 이동
            GuildMasterPopupManager.Instance.OpenGuildMasterPopup(currentMemberData);
        }
        else if (myUserRole == "부마스터")
        {
            GuildBanPopupManager.Instance.OpenGuildBanPopup(currentMemberData);
        }
        else
        {
            // 일반 유저라면 사실 버튼이 안 보이겠지만, 혹시 모르니
            Debug.LogWarning("[OnClickSettingIcon] 일반 유저는 권한 없음");
        }
    }
}