using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class EventPopupManager : MonoBehaviour
{
    public static EventPopupManager Instance { get; private set; }

    public GameObject EventCanvas;     // 이벤트 canvas


    public void OpenEventPopup()
    {   // 팝업 활성화 
        if (EventCanvas != null)
        {
            EventCanvas.SetActive(true);
        }

    }
    public void CloseEventPopup()
    {   // 팝업 비활성화 
        if (EventCanvas != null)
        {
            EventCanvas.SetActive(false);
        }
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

    void Start()
    { // 처음에 팝업 패널 비활성화 
        Canvas canvas = GetComponent<Canvas>();
        if (EventCanvas != null)
        {
            EventCanvas.SetActive(false);
            UnityEngine.Debug.Log("이벤트창 설정 완료");
        }
    }

}
