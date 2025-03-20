using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenGuildGeneratePopup : MonoBehaviour
{
    public void OnClickOpenGuildPanel() // 길드 생성 버튼 클릭
    {
        GuildGeneratePopupManager.Instance.OpenGuildGeneratePopup();
    }
}
