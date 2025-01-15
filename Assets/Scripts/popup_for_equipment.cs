using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class popup_for_equipment : MonoBehaviour
{
    // SingleTon instance. 어디서든 popup_for_equipment.Instance로 접근 가능하게 설정(Canvas를 여러개로 나눔으로서 필요)
    // 딴 스크립트에서 popup_for_equipment.Instance.OpenEquipmentPanel(); 로 함수 호출 가능
    public static popup_for_equipment Instance { get; private set; }


    public GameObject popupCanvas;     // 팝업 canvas
    public GameObject equipment_Panel; // 활성화/비활성화 시킬 패널
    


    private void Awake() // Unity의 Awake() 메서드에서 싱글톤 설정
    {
        // 만약 Instance가 비어 있으면, 이 스크립트를 싱글톤 인스턴스로 설정
        if (Instance == null)
        {
            Instance = this; // 현재 객체를 싱글톤으로 지정
        }
        else
        {
            // 이미 싱글톤 인스턴스가 존재하면, 중복된 객체를 제거
            Destroy(gameObject);
        }
    }

    void Start() { // 처음에 팝업 패널 비활성화 
        if (popupCanvas != null) {
            popupCanvas.SetActive(false);
            equipment_Panel.SetActive(false);
        }
    }
    public void OpenPanel() {   // 팝업 활성화 
        if (equipment_Panel != null)
        {
            popupCanvas.SetActive(true);
            equipment_Panel.SetActive(true);
        }
    }
    public void ClosePanel() {  // 팝업 비활성화
        if (equipment_Panel != null)
        {
            popupCanvas.SetActive(false);
            equipment_Panel.SetActive(false);
        }
    }
}
