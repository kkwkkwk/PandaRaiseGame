using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class openPopupbtn : MonoBehaviour
{
    public void OnClickOpenPanel()
    {
        // 싱글톤을 통해 equipment_Panel 열기
        popup_for_equipment.Instance.OpenPanel();
    }
}
