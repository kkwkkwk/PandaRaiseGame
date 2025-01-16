using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class popup_for_chat : MonoBehaviour
{
    // SingleTon instance. 어디서든 popup_for_chat.Instance로 접근 가능하게 설정(Canvas를 여러개로 나눔으로서 필요)
    // 딴 스크립트에서 popup_for_chat.Instance.function(); 로 함수 호출 가능
    public static popup_for_chat Instance { get; private set; }

    public GameObject chatCanvas;               // 채팅창 canvas
    public GameObject chatPopup_Panel;          // 활성화/비활성화 시킬 전체화면 채팅 패널
    public GameObject chatpreview_Panel;        // 활성화/비활성화 시킬 하단 채팅 패널
    public GameObject chatpreview2_Panel;       // 활성화/비활성화 시킬 중앙 채팅 패널
    public GameObject chatBottom_Panel;         // 활성화/비활성화 시킬 최하단 패널
    public TextMeshProUGUI chatPreviewText;     // 채팅 내용을 표시하는 Text 컴포넌트

    public bool middlePreview; // 중앙 채팅 오픈 여부
    public bool isFullScreenActive; // 전체 채팅 화면 on/off 여부

    public void OpenBottomChatPreview() // 하단 채팅 미리보기 패널 열기
    {
        if (chatpreview_Panel != null)
        {
            chatpreview_Panel.SetActive(true);
        }
        // 자식 요소 모두 활성화
        foreach (Transform child in chatpreview_Panel.transform)
        {
            child.gameObject.SetActive(true);
        }
    }
    public void CloseBottomChatPreview() // 하단 채팅 미리보기 패널 닫기
    {
        if (chatpreview_Panel != null)
        {
            chatpreview_Panel.SetActive(false);
            // 자식 요소 모두 비활성화
            foreach (Transform child in chatpreview_Panel.transform)
            {
                child.gameObject.SetActive(false);
            }
        }
    }
    public void OpenMiddleChatPreview() // 중단 채팅 미리보기 열기
    {
        if (chatpreview2_Panel != null)
        {
            chatpreview2_Panel.SetActive(true);
        }
    }
    public void CloseMiddleChatPreview() // 중단 채팅 미리보기 닫기
    {
        if (chatpreview2_Panel != null)
        {
            chatpreview2_Panel.SetActive(false);
        }
    }

    public void OpenFullScreenChat() // 전체 채팅 열기
    {
        if (chatPopup_Panel != null)
        {
            chatPopup_Panel.SetActive(true);
        }
        isFullScreenActive = true;
        if (chatBottom_Panel != null)
        {
            chatBottom_Panel.SetActive(true);
        }
        Debug.Log("전체 화면 채팅 열림");
        CloseBottomChatPreview();
    }

    public void CloseFullScreenChat() // 전체 채팅 닫기
    {
        if (chatPopup_Panel != null)
        {
            chatPopup_Panel.SetActive(false);
        }
        if (chatBottom_Panel != null)
        {
            chatBottom_Panel.SetActive(false);
        }
        isFullScreenActive = false;
        Debug.Log("전체 화면 채팅 닫힘");
    }

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

    private void Start() // 시작 초기 설정
    {
        CloseFullScreenChat();
        CloseMiddleChatPreview();
        middlePreview = false;
        isFullScreenActive = false;
    }

    public void OpenChat() // 채팅창 전체화면 활성화
    {
        OpenFullScreenChat();
        if (middlePreview) // 중앙 채팅이 오픈되어 있는 경우
        {
            CloseMiddleChatPreview();
        }
    }

    public void CloseChat() // 채팅창 전체화면 비활성화
    {
        CloseFullScreenChat();
        if (middlePreview) // 중앙 채팅이 오픈되어 있는 경우
        {
            OpenMiddleChatPreview();
        }
        else
        {
            OpenBottomChatPreview(); // 하단 채팅이 오픈되어 있는 경우
        }
    }
}
