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
    public GameObject guildUserSettingIcon;        // 왼쪽 흰색 사각형에 해당하는 Image 컴포넌트

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
    public void SetData(GuildMemberData memberData)
    {
        if (userNameText != null)
            userNameText.text = memberData.userName;

        if (userClassText != null)
            userClassText.text = memberData.userClass;

        if (userPowerText != null)
            userPowerText.text = memberData.userPower.ToString();

        if (userConnectionStatusImage != null)
        {
            userConnectionStatusImage.sprite = memberData.isOnline ? onlineSprite : offlineSprite;
        }

        // -------------------------------
        //    길드 계급이 "부마스터" 또는 "길드마스터"인 경우만
        //    '설정 아이콘' 오브젝트를 활성화
        // -------------------------------
        bool isMasterOrSub = (memberData.userClass == "길드마스터" || memberData.userClass == "부마스터");

        if (guildUserSettingIcon_sprite != null)
        {
            if (guildUserSettingIcon != null)
            {
                // (1) 아이콘 활성화/비활성
                guildUserSettingIcon.SetActive(isMasterOrSub);

                // (2) Image 컴포넌트에 sprite 할당
                if (isMasterOrSub)
                {
                    var iconImage = guildUserSettingIcon.GetComponent<Image>();
                    if (iconImage != null && guildUserSettingIcon_sprite != null)
                    {
                        iconImage.sprite = guildUserSettingIcon_sprite;
                    }
                }
            }
        }
    }
}
