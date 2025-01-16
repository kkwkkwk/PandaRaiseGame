using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class openPopupbtn : MonoBehaviour
{
    public void OnClickOpenPanel() // 장비 버튼 클릭
    {
        if (popup_for_chat.Instance.isFullScreenActive)
        {
            popup_for_chat.Instance.CloseChat();
        }
        popup_for_equipment.Instance.OpenPanel();
    }
}
