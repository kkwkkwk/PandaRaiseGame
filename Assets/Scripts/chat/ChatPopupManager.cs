using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class ChatPopupManager : MonoBehaviour
{
    // SingleTon instance. 어디서든 popup_for_chat.Instance로 접근 가능하게 설정(Canvas를 여러개로 나눔으로서 필요)
    // 딴 스크립트에서 popup_for_chat.Instance.function(); 로 함수 호출 가능
    public static ChatPopupManager Instance { get; private set; }

    public GameObject ChatCanvas;               // 채팅창 canvas
    public GameObject ChatPopup_Panel;          // 활성화/비활성화 시킬 전체화면 채팅 패널
    public GameObject ChatPreview_Panel;        // 활성화/비활성화 시킬 하단 채팅 패널
    public GameObject ChatPreview2_Panel;       // 활성화/비활성화 시킬 중앙 채팅 패널
    public GameObject ChatBottom_Panel;         // 활성화/비활성화 시킬 최하단 패널
    public TextMeshProUGUI ChatPreviewText;     // 채팅 내용을 표시하는 Text 컴포넌트

    public bool middlePreview; // 중앙 채팅 오픈 여부
    public bool isFullScreenActive; // 전체 채팅 화면 on/off 여부

    public void OpenBottomChatPreview() // 하단 채팅 미리보기 패널 열기
    {
        if (ChatPreview_Panel != null)
        {
            ChatPreview_Panel.SetActive(true);
        }
        // 자식 요소 모두 활성화
        foreach (Transform child in ChatPreview_Panel.transform)
        {
            child.gameObject.SetActive(true);
        }
    }
    public void CloseBottomChatPreview() // 하단 채팅 미리보기 패널 닫기
    {
        if (ChatPreview_Panel != null)
        {
            ChatPreview_Panel.SetActive(false);
            // 자식 요소 모두 비활성화
            foreach (Transform child in ChatPreview_Panel.transform)
            {
                child.gameObject.SetActive(false);
            }
        }
    }
    public void OpenMiddleChatPreview() // 중단 채팅 미리보기 열기
    {
        if (ChatPreview2_Panel != null)
        {
            ChatPreview2_Panel.SetActive(true);
        }
    }
    public void CloseMiddleChatPreview() // 중단 채팅 미리보기 닫기
    {
        if (ChatPreview2_Panel != null)
        {
            ChatPreview2_Panel.SetActive(false);
        }
    }

    public void OpenFullScreenChat() // 전체 채팅 열기
    {
        if (ChatPopup_Panel != null)
        {
            ChatPopup_Panel.SetActive(true);
        }
        isFullScreenActive = true;
        if (ChatBottom_Panel != null)
        {
            ChatBottom_Panel.SetActive(true);
        }
        Debug.Log("전체 화면 채팅 열림");
        CloseBottomChatPreview();
    }

    public void CloseFullScreenChat() // 전체 채팅 닫기
    {
        if (ChatPopup_Panel != null)
        {
            ChatPopup_Panel.SetActive(false);
        }
        if (ChatBottom_Panel != null)
        {
            ChatBottom_Panel.SetActive(false);
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
        Canvas canvas = GetComponent<Canvas>();

        CloseFullScreenChat();
        CloseMiddleChatPreview();
        middlePreview = false;
        isFullScreenActive = false;
        UnityEngine.Debug.Log("채팅창 설정 완료");
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
