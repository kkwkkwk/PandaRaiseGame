using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenGuildPopup : MonoBehaviour
{
    public void OnClickOpenGuildPanel() // 길드 버튼 클릭
    {
        GuildPopupManager.Instance.OpenGuildPopup();
    }
}
