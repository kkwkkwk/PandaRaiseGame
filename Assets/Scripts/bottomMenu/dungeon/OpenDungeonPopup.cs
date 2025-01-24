using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenDungeonPopup : MonoBehaviour
{
    public void OnClickOpenPanel() // 장비 버튼 클릭
    {
        UnityEngine.Debug.Log("Dungeon Button Clicked");
        if (PopupForChat.Instance.isFullScreenActive)
        {
            PopupForChat.Instance.CloseChat();
        }
        PopupForDungeon.Instance.OpenPanel();
    }
}

