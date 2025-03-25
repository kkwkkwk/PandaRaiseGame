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

        if (userNameText)   userNameText.text = memberData.userName;
        if (userClassText)  userClassText.text = memberData.userClass;
        if (userPowerText)  userPowerText.text = memberData.userPower.ToString();

        if (userConnectionStatusImage != null)
            userConnectionStatusImage.sprite = memberData.isOnline ? onlineSprite : offlineSprite;


        // (1) 기본적으로 아이콘은 안 보이도록
        bool showIcon = false;

        // (2) "길드마스터", "부마스터" 계정만 아이콘 표시
        //     단, 특별 조건: 길드마스터는 자기 자신은 추방 X
        //     부마스터는 길드마스터/부마스터는 추방 X
        if (myUserRole == "길드마스터")
        {
            // 자기 자신인 경우 (이 로직은 실제론 entityId 비교 권장)
            // 간단히 "memberData.userName == 내 userName"으로 판단할 수도 있음
            // 여기서는 "길드마스터"는 자신 or 다른 마스터는 제외
            // => if (memberData.userClass == "길드마스터") => 숨김
            if (memberData.userClass == "길드마스터")
                showIcon = false;
            else
                showIcon = true;
        }
        else if (myUserRole == "부마스터")
        {
            // 부마스터 => 길드마스터/부마스터는 추방 불가 => 아이콘 숨김
            // 일반 길드원만 추방 가능 => showIcon = (rowUserClass == "길드원")
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
        if (myUserRole == "길드마스터")
        {
            // 길드마스터인 경우,
            // 만약 해당 행의 멤버가 길드마스터라면 GuildMasterCanvas 기반 팝업 오픈,
            // 그 외(일반 길드원, 부마스터)는 길드 추방 팝업 오픈
            if (currentMemberData.userClass == "길드마스터")
            {
                // GuildMasterPopupManager 사용
                GuildMasterPopupManager.Instance.OpenGuildMasterPopup(currentMemberData);
            }
            else
            {
                // 기존 추방 팝업
                GuildBanPopupManager.Instance.OpenGuildBanPopup(currentMemberData);
            }
        }
        else if (myUserRole == "부마스터")
        {
            // 부마스터는 일반 길드원만 관리 가능하므로,
            // (아이콘이 표시된 경우, 해당 멤버는 일반 길드원임)
            GuildBanPopupManager.Instance.OpenGuildBanPopup(currentMemberData);
        }
    }
}