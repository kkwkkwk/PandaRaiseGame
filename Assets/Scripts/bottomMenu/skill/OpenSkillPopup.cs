using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenSkillPopup : MonoBehaviour
{
    public void OnClickOpenPanel() // 스킬 버튼 클릭
    {
        UnityEngine.Debug.Log("Skill Button Clicked");
        if (PopupForChat.Instance.isFullScreenActive)
        {
            PopupForChat.Instance.CloseChat();
        }
        PopupForSkill.Instance.OpenPanel();
    }
}
