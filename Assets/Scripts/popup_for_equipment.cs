using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class popup_for_equipment : MonoBehaviour
{

    public GameObject equipment_Panel; // Ȱ��ȭ/��Ȱ��ȭ ��ų �г�

    void Start() { // ó���� �˾� �г� ��Ȱ��ȭ 
        if (equipment_Panel != null) {
            equipment_Panel.SetActive(false);
        }
    }
    public void OpenPanel() {   // �˾� Ȱ��ȭ 
        if (equipment_Panel != null)
        {
            equipment_Panel.SetActive(true);
        }
    }
    public void ClosePanel() {  // �˾� ��Ȱ��ȭ
        if (equipment_Panel != null)
        {
            equipment_Panel.SetActive(false);
        }
    }
}
