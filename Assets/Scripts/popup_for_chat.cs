using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class popup_for_chat : MonoBehaviour
{
    // SingleTon instance. 어디서든 popup_for_chat.Instance로 접근 가능하게 설정(Canvas를 여러개로 나눔으로서 필요)
    // 딴 스크립트에서 popup_for_chat.Instance.function(); 로 함수 호출 가능
    public static popup_for_chat Instance { get; private set; }

    public GameObject chatCanvas;      // 채팅창 canvas
    public GameObject chatPopup_Panel; // 활성화/비활성화 시킬 패널
    // public Text chatText;              // 채팅 내용을 표시할 Text 컴포넌트

    private void Awake()
    {
        // 싱글톤 인스턴스 초기화
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 채팅창 비활성화
        if (chatPopup_Panel != null)
        {
            chatPopup_Panel.SetActive(false);
        }
    }

    public void OpenChat()
    {
        if (chatPopup_Panel != null)
        {
            chatPopup_Panel.SetActive(true);
        }
    }

    public void CloseChat()
    {
        if (chatPopup_Panel != null)
        {
            chatPopup_Panel.SetActive(false);
        }
    }

    /*
    public void AddMessage(string message)
    {
        if (chatText != null)
        {
            chatText.text += message + "\n"; // 새 메시지 추가
        }
    }
    */
}
