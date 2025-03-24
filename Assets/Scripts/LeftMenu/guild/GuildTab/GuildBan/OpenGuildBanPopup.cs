using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenGuildBanPopup : MonoBehaviour
{
    public void OnClickOpenGuildBanPanel() // 길드 버튼 클릭
    {
        GuildBanPopupManager.Instance.OpenGuildBanPopup();
    }
}
