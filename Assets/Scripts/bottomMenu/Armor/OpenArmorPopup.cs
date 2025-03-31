using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenArmorPopup : MonoBehaviour
{
    public void OnClickOpenPanel() // 장비 버튼 클릭
    {
        UnityEngine.Debug.Log("Armor Button Clicked");
        if (ChatPopupManager.Instance.isFullScreenActive)
        {
            ChatPopupManager.Instance.CloseChat();
        }
        ArmorPopupManager.Instance.OpenArmorPanel();
    }
}
