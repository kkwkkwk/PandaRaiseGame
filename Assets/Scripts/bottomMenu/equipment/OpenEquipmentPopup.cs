using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenEquipmentPopup : MonoBehaviour
{
    public void OnClickOpenPanel() // 장비 버튼 클릭
    {
        UnityEngine.Debug.Log("Equipment Button Clicked");
        if (ChatPopupManager.Instance.isFullScreenActive)
        {
            ChatPopupManager.Instance.CloseChat();
        }
        EquipmentPopupManager.Instance.OpenPanel();
    }
}
