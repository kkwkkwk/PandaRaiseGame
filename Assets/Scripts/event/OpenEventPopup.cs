using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenEventPopup : MonoBehaviour
{
    public void OnClickOpenEventPanel()
    {
        EventPopupManager.Instance.OpenEventPopup();
        EventList.Instance.PopulateImages();
    }
}
