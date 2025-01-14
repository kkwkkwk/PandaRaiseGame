using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class popup_for_equipment : MonoBehaviour
{

    public GameObject equipment_Panel; // 활성화/비활성화 시킬 패널

    void Start() { // 처음에 팝업 패널 비활성화 
        if (equipment_Panel != null) {
            equipment_Panel.SetActive(false);
        }
    }
    public void OpenPanel() {   // 팝업 활성화 
        if (equipment_Panel != null)
        {
            equipment_Panel.SetActive(true);
        }
    }
    public void ClosePanel() {  // 팝업 비활성화
        if (equipment_Panel != null)
        {
            equipment_Panel.SetActive(false);
        }
    }
}
