using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenDungeonPopup : MonoBehaviour
{
    public void OnClickOpenPanel() // 장비 버튼 클릭
    {
        UnityEngine.Debug.Log("Dungeon Button Clicked");
        if (ChatPopupManager.Instance.isFullScreenActive)
        {
            ChatPopupManager.Instance.CloseChat();
        }
        DungeonPopupManager.Instance.OpenPanel();
    }
}

