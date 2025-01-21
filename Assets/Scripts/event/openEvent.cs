using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class openEvent : MonoBehaviour
{
    public void OnClickOpenEventPanel() // 장비 버튼 클릭
    {
        popup_for_event.Instance.OpenEventPopup();
        eventList.Instance.PopulateImages();
    }
}
