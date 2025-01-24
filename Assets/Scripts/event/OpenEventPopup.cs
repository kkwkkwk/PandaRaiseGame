using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenEventPopup : MonoBehaviour
{
    public void OnClickOpenEventPanel() // 장비 버튼 클릭
    {
        PopupForEvent.Instance.OpenEventPopup();
        EventList.Instance.PopulateImages();
    }
}
