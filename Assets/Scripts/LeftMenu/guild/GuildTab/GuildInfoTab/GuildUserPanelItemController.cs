using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GuildUserPanelItemController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI userNameText;       // guild_user_name_text
    public TextMeshProUGUI userClassText;      // guild_user_class_text
    public TextMeshProUGUI userPowerText;      // guild_user_power_text
    public Image userConnectionStatusImage;    // GuildUserConnectionStatusImage_Panel 내 Image

    [Header("Connection Status Sprites")]
    public Sprite onlineSprite;                // 접속 중일 때 표시할 이미지
    public Sprite offlineSprite;               // 미접속일 때 표시할 이미지

    private void Awake()
    {
        onlineSprite = Resources.Load<Sprite>("Sprites/Guild/OnlineStatus");   // Assets/Resources/Sprites/Guild/OnlineStatus.png
        offlineSprite = Resources.Load<Sprite>("Sprites/Guild/OfflineStatus"); // Assets/Resources/Sprites/Guild/OfflineStatus.png

        if (onlineSprite == null)
            Debug.LogError("Online sprite 로드 실패");
        if (offlineSprite == null)
            Debug.LogError("Offline sprite 로드 실패");
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
    }
}
