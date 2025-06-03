using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenDungeonPopup : MonoBehaviour
{
    public void OnClickOpenPanel()
    {
        UnityEngine.Debug.Log("Dungeon Button Clicked");
        if (ChatPopupManager.Instance.isFullScreenActive)
        {
            ChatPopupManager.Instance.CloseChat();
        }
        DungeonPopupManager.Instance.OpenDungeonPanel();
    }
}

