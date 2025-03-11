using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// [추가] WebSocketSharp 네임스페이스
using WebSocketSharp;
using System.Net.WebSockets;
using Unity.VisualScripting;

public class ChatManager : MonoBehaviour
{
    [Tooltip("스크롤뷰의 ScrollRect")]
    public ScrollRect chatScrollRect;

    [Tooltip("스크롤뷰 Content (채팅 메시지가 들어갈 부모)")]
    public Transform contentTransform;

    [Header("Chat Prefabs")]
    [Tooltip("내가 채팅 친 경우 사용할 프리팹")]
    public GameObject playerChatPrefab;

    [Tooltip("상대방이 채팅 친 경우 사용할 프리팹")]
    public GameObject otherPlayerChatPrefab;

    [Tooltip("수신한 메시지를 메인 스레드에서 처리하기 위한 큐")]
    private Queue<string> messageQueue = new Queue<string>();

    [Tooltip("WebSocketSharp용 WebSocket 인스턴스")]
    private WebSocketSharp.WebSocket webSocket;

    /// <summary>
    /// Chat Popup 열릴 때 호출
    /// </summary>
    public void OnChatPopupOpened()
    {
        Debug.Log("[ChatManager] 채팅 팝업 열림. ScrollToBottom() 예정.");
        StartCoroutine(ScrollToBottom());
    }

    /// <summary>
    /// 채팅 메시지 추가: 이름 + 내용
    /// isMine = true면 내가 보낸 채팅, false면 상대방 채팅
    /// </summary>
    public void AddChatMessage(string senderName, string message, bool isMine)
    {
        Debug.Log($"[ChatManager] AddChatMessage -> isMine: {isMine}, 보낸이: {senderName}, 내용: {message}");

        GameObject prefabToUse = isMine ? playerChatPrefab : otherPlayerChatPrefab;
        if (prefabToUse == null)
        {
            Debug.LogWarning("[ChatManager] 채팅 프리팹이 인스펙터에 연결되어 있지 않습니다.");
            return;
        }

        // 프리팹 생성
        GameObject newMessageObj = Instantiate(prefabToUse, contentTransform);

        // ChatMessageItem에 메시지 할당
        ChatMessageItem chatItem = newMessageObj.GetComponent<ChatMessageItem>();
        if (chatItem != null)
        {
            chatItem.SetMessage(senderName, message);
        }

        // 자동 스크롤
        StartCoroutine(ScrollToBottom());
    }

    // 자동 스크롤을 위한 코루틴
    private IEnumerator ScrollToBottom()
    {
        yield return null; // 한 프레임 대기
        if (chatScrollRect != null)
        {
            chatScrollRect.verticalNormalizedPosition = 0f;
            Debug.Log("[ChatManager] ScrollRect를 맨 아래로 이동 완료.");
        }
    }

    // ------------------------------------------------------------------------
    // [추가] WebSocket 연결 & 메시지 수신
    // ------------------------------------------------------------------------

    /// <summary>
    /// Azure Web PubSub에서 받은 accessUrl을 사용해 WebSocket 연결 시도
    /// (ChatPopupManager 등에서 이 함수를 호출)
    /// </summary>
    public void ConnectToWebSocket(string accessUrl)
    {
        if (string.IsNullOrEmpty(accessUrl))
        {
            Debug.LogError("[ChatManager] WebSocket accessUrl이 비어있음. 연결 불가");
            return;
        }

        Debug.Log($"[ChatManager] WebSocket 연결 시도: {accessUrl}");
        // webSocketSharp.WebSocket 생성
        webSocket = new WebSocketSharp.WebSocket(accessUrl);

        // 콜백 등록
        webSocket.OnOpen += (sender, e) =>
        {
            Debug.Log("[ChatManager] WebSocket 연결 성공!");
        };

        webSocket.OnMessage += (sender, e) =>
        {
            // 서버로부터 메시지 수신
            // e.Data는 string(텍스트)이므로 바로 사용 가능
            Debug.Log($"[ChatManager] WebSocket OnMessage: {e.Data}");

            // 다른 쓰레드에서 실행되므로, Unity 메인 스레드에서 처리하도록 큐에 쌓는다.
            lock (messageQueue)
            {
                messageQueue.Enqueue(e.Data);
            }
        };

        webSocket.OnError += (sender, e) =>
        {
            Debug.LogError($"[ChatManager] WebSocket 오류: {e.Message}");
        };

        webSocket.OnClose += (sender, e) =>
        {
            Debug.Log($"[ChatManager] WebSocket 연결 종료. code={e.Code}, reason={e.Reason}");
        };

        // 실제 연결 시도
        webSocket.ConnectAsync();
    }

    /// <summary>
    /// WebSocket 연결 해제
    /// (필요한 시점에 호출: 예를 들어 게임 종료나 채팅 끄기 등)
    /// </summary>
    public void DisconnectWebSocket()
    {
        if (webSocket != null && webSocket.IsAlive)
        {
            webSocket.Close();
            webSocket = null;
        }
    }

    /// <summary>
    /// 매 프레임마다 수신 큐 확인 → 채팅 추가
    /// </summary>
    private void Update()
    {
        if (messageQueue.Count > 0)
        {
            string msg;
            lock (messageQueue)
            {
                msg = messageQueue.Dequeue();
            }
            // 예: 서버에서 "플레이어ID: 메시지내용" 형태로 보냈다고 가정
            //     또는 JSON을 넘긴다면 JSON 파싱 로직 추가
            ParseAndAddMessage(msg);
        }
    }

    /// <summary>
    /// 수신된 문자열(msg)을 파싱해, isMine=false 로 AddChatMessage 호출
    /// (물론, 여기서 playFabId가 내 아이디면 isMine=true로 표기할 수도 있음)
    /// </summary>
    private void ParseAndAddMessage(string rawMessage)
    {
        // 간단히 "senderName: content" 형태를 파싱한다고 가정
        // 실제로는 JSON 등으로 파싱하는 것이 안전
        string senderName = "Unknown";
        string content = rawMessage;

        // ":" 기준으로 분리
        int idx = rawMessage.IndexOf(":");
        if (idx >= 0)
        {
            senderName = rawMessage.Substring(0, idx).Trim();
            content = rawMessage.Substring(idx + 1).Trim();
        }

        bool isMine = (senderName == GlobalData.playFabId);
        if (isMine)
        {
            // 이미 내 로컬에 표시했으므로 굳이 2번 표시하지 않음
            return;
        }
        AddChatMessage(senderName, content, false);
    }
}