using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenEquipmentPopup : MonoBehaviour
{
    public void OnClickOpenPanel() // 장비 버튼 클릭
    {
        UnityEngine.Debug.Log("Equipment Button Clicked");
        if (PopupForChat.Instance.isFullScreenActive)
        {
            PopupForChat.Instance.CloseChat();
        }
        PopupForEquipment.Instance.OpenPanel();
    }
}
