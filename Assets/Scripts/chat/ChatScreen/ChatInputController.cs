using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class ChatInputController : MonoBehaviour
{
    [Header("References")]
    public ChatManager chatManager;       // ChatManager references
    public TMP_InputField chatInputField; // 입력칸

    [Header("Chat Settings")]
    public string myName = "Player"; // 이름 (추후 변경)
    public bool isMine = true;       // 본인 여부

    private string sendChatMessageUrl = "https://pandaraisegame-chat.azurewebsites.net/api/SendChatMessage?code=DjDxBJVgXQXK6YrMOYWZd08p9a5q8nfxgFwlNO6HDlDyAzFundGjaQ==";

    private void Start()
    {
        // TMP_InputField의 onSubmit 콜백 등록 (모바일 소프트 키보드 Enter 대응)
        if (chatInputField != null)
        {
            chatInputField.onSubmit.AddListener(OnSubmitInput);
            Debug.Log("[ChatInputController] onSubmit Listener 등록 완료.");
        }
        else
        {
            Debug.LogWarning("[ChatInputController] chatInputField가 인스펙터에서 연결되지 않았습니다.");
        }
    }

    /// <summary>
    /// (A) 버튼으로 전송할 때
    /// </summary>
    public void OnSendButtonClicked()
    {
        Debug.Log("[ChatInputController] '보내기' 버튼 클릭");
        SendMessageIfNotEmpty();
    }

    /// <summary>
    /// (B) 소프트 키보드 Enter(혹은 완료/전송) -> onSubmit 이벤트
    /// </summary>
    private void OnSubmitInput(string submittedText)
    {
        Debug.Log($"[ChatInputController] onSubmit 호출. 입력값: {submittedText}");
        SendMessageIfNotEmpty();
    }

    /// <summary>
    /// 실제 전송 처리 (공통 로직)
    /// </summary>
    private void SendMessageIfNotEmpty()
    {
        // 메시지 내용이 비어있지 않을 때만 전송
        if (!string.IsNullOrEmpty(chatInputField.text))
        {

            // 로컬 UI에 채팅 추가
            string message = chatInputField.text;

            Debug.Log($"[ChatInputController] 채팅 전송 -> 이름: {myName}, 내용: {chatInputField.text}, isMine: {isMine}");
            chatManager.AddChatMessage(myName, chatInputField.text, isMine);

            // (1) Azure Function 호출하여 서버에 전송
            StartCoroutine(SendMessageToServer(message));

            // 입력창 초기화 후 다시 포커싱
            chatInputField.text = "";
            chatInputField.ActivateInputField();
        }
        else
        {
            // Enter나 버튼을 눌렀는데 비어있다면?
            Debug.Log("[ChatInputController] 입력 값이 비어있어 전송하지 않음.");
        }
    }

    /// <summary>
    /// Azure Function의 SendChatMessage 함수를 호출하는 코루틴
    /// </summary>
    private IEnumerator SendMessageToServer(string content)
    {
        if (string.IsNullOrEmpty(GlobalData.playFabId))
        {
            Debug.LogWarning("[ChatInputController] GlobalData.playFabId가 비어있습니다. 로그인 여부 확인 필요.");
            yield break;
        }

        // 요청 JSON 생성
        //    { "playFabId": "xxx", "content": "Hello" } 형태
        string jsonData = $"{{\"playFabId\":\"{GlobalData.playFabId}\",\"content\":\"{content}\"}}";
        Debug.Log($"[SendChatMessage] 요청 JSON: {jsonData}");

        // 2) UnityWebRequest 설정 (POST)
        using (UnityWebRequest request = new UnityWebRequest(sendChatMessageUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            // 3) 헤더 설정
            request.SetRequestHeader("Content-Type", "application/json");

            // 4) 요청 전송
            yield return request.SendWebRequest();

            // 5) 응답 결과 확인
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[SendChatMessage] 호출 실패: {request.error}");
            }
            else
            {
                string response = request.downloadHandler.text;
                Debug.Log($"[SendChatMessage] 호출 성공. 응답: {response}");
            }
        }
    }
}
