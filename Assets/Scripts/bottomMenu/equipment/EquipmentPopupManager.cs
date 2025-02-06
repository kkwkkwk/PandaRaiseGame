using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class EquipmentPopupManager : MonoBehaviour
{
    // SingleTon instance. 어디서든 EquipmentPopupManager.Instance로 접근 가능하게 설정(Canvas를 여러개로 나눔으로서 필요)
    // 딴 스크립트에서 EquipmentPopupManager.Instance.OpenEquipmentPanel(); 로 함수 호출 가능
    public static EquipmentPopupManager Instance { get; private set; }

    const int sortOrder = 10;          // override sort order 순서

    public GameObject PopupCanvas;     // 팝업 canvas

    public void OpenPanel()
    {   // 팝업 활성화 
        if (PopupCanvas != null)
        {
            PopupCanvas.SetActive(true);
        }
        ChatPopupManager.Instance.OpenMiddleChatPreview();  // 중앙에 채팅 미리보기 ui 생성
        ChatPopupManager.Instance.CloseBottomChatPreview(); // 하단에 채팅 미리보기 ui 제거
        ChatPopupManager.Instance.middlePreview = true;
    }
    public void ClosePanel()
    {  // 팝업 비활성화
        if (PopupCanvas != null)
        {
            PopupCanvas.SetActive(false);
        }
        ChatPopupManager.Instance.CloseMiddleChatPreview();  // 중앙에 채팅 미리보기 ui 제거
        ChatPopupManager.Instance.OpenBottomChatPreview();   // 하단에 채팅 미리보기 ui 생성
        ChatPopupManager.Instance.middlePreview = false;
    }

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
        Canvas canvas = GetComponent<Canvas>();
        if (PopupCanvas != null)
        {
            canvas.overrideSorting = true;     // ✅ Override Sorting 활성화
            canvas.sortingOrder = sortOrder;   // ✅ 팝업이 앞쪽으로 오도록 설정
            PopupCanvas.SetActive(false);
        }
    }
    
}
