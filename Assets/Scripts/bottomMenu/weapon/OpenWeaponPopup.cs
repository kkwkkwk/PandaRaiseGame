using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenWeaponPopup : MonoBehaviour
{
    public void OnClickOpenPanel() // 무기 버튼 클릭
    {
        UnityEngine.Debug.Log("Weapon Button Clicked");
        if (ChatPopupManager.Instance.isFullScreenActive)
        {
            ChatPopupManager.Instance.CloseChat();
        }
        WeaponPopupManager.Instance.OpenWeaponPanel();
    }
}
