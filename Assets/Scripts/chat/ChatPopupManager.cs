using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json.Linq; // JSON 파싱을 위해 Newtonsoft 사용 가능


public class ChatPopupManager : MonoBehaviour
{
    // 실제 Azure Function URL로 교체하세요.
    // 뒤에 ?code=... 등 Function Key 필요 시 추가
    private string getChatConnectionUrl = "https://pandaraisegame-chat.azurewebsites.net/api/GetChatConnection?code=byQ95MmRXdloyEJx6PKlr7c7JEO1JiY178upVNQXo-JGAzFuwT_hYw==";

    // Web PubSub에 연결하기 위한 URL (받아온 후 ChatManager에 전달)
    private string accessUrl;

    // 싱글톤
    public static ChatPopupManager Instance { get; private set; }

    // --------------------------
    // 채팅창 UI 관련 필드
    // --------------------------
    public GameObject ChatCanvas;
    public GameObject ChatPopup_Panel;
    public GameObject ChatPreview_Panel;
    public GameObject ChatPreview2_Panel;
    public GameObject ChatBottom_Panel;
    public TextMeshProUGUI ChatPreviewText;

    public bool middlePreview;      // 중앙 채팅 오픈 여부
    public bool isFullScreenActive = false; // 전체 채팅 화면 on/off 여부

    // --------------------------
    // ChatManager 참조
    // --------------------------
    public ChatManager chatManager;

    // --------------------------
    // GetChatConnection 응답 DTO
    // --------------------------
    [System.Serializable]
    private class ChatConnectionResponse
    {
        public string playFabId;
        public string guild;
        public string hub;
        public int expiresInMinutes;
        public string accessUrl;
        public string[] roles;
        public string[] autoJoinGroups;
    }

    #region Unity Lifecycle
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
        Debug.Log("채팅창 설정 완료");

        // 로그인 후 GlobalData.playFabId가 준비되었다고 가정하고,
        // GetChatConnection 호출하여 WebSocket URL(accessUrl) 받아오기
        StartCoroutine(GetChatConnectionCoroutine());
    }
    #endregion

    /// <summary>
    /// 서버의 GetChatConnection 함수를 호출하여 accessUrl 및 기타 정보를 받아온다.
    /// </summary>
    private IEnumerator GetChatConnectionCoroutine()
    {
        // (1) 전송할 JSON { "playFabId": "..." }
        string jsonData = $"{{\"playFabId\":\"{GlobalData.playFabId}\"}}";
        Debug.Log($"[GetChatConnection] 요청 JSON: {jsonData}");

        // (2) UnityWebRequest 설정
        UnityWebRequest request = new UnityWebRequest(getChatConnectionUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // (3) 요청 전송
        yield return request.SendWebRequest();

        // (4) 결과 확인
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[GetChatConnection] 호출 실패: {request.error}");
        }
        else
        {
            string responseJson = request.downloadHandler.text;
            Debug.Log($"[GetChatConnection] 호출 성공. 응답: {responseJson}");

            // (5) JsonUtility를 사용해 파싱
            ChatConnectionResponse response = JsonUtility.FromJson<ChatConnectionResponse>(responseJson);
            if (response != null)
            {
                this.accessUrl = response.accessUrl;
                Debug.Log($"[GetChatConnection] accessUrl: {accessUrl}");
                Debug.Log($"[GetChatConnection] guild: {response.guild}, hub: {response.hub}");

                // (6) ChatManager에 WebSocket 연결 지시
                if (chatManager != null)
                {
                    chatManager.ConnectToWebSocket(accessUrl);
                }
                else
                {
                    Debug.LogWarning("[ChatPopupManager] chatManager가 연결되지 않았습니다. 씬에서 ChatManager 할당 필요!");
                }
            }
            else
            {
                Debug.LogError("[GetChatConnection] 응답 파싱 실패!");
            }
        }
    }

    #region UI Toggle Methods
    public void OpenBottomChatPreview() // 하단 채팅 미리보기 패널 열기
    {
        if (ChatPreview_Panel != null)
        {
            ChatPreview_Panel.SetActive(true);
        }
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

        // 채팅 팝업 열릴 때, ChatManager에게 알림 (스크롤 맨 아래로 이동 등)
        if (chatManager != null)
        {
            chatManager.OnChatPopupOpened();
            Debug.Log("OnChatPopupOpened 함수 호출");
        }

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
        if (middlePreview)
        {
            OpenMiddleChatPreview();
        }
        else
        {
            OpenBottomChatPreview();
        }
    }
    #endregion
}
