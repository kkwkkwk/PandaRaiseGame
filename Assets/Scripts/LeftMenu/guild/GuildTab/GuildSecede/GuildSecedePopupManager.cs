using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;

public class GuildSecedePopupManager : MonoBehaviour
{
    public static GuildSecedePopupManager Instance { get; private set; }

    [Header("Popup Canvas")]
    public GameObject GuildSecedeCanvas;     // 길드 탈퇴 팝업 Canvas

    [Header("UI References")]
    public Button secedeButton;                     // 탈퇴 버튼
    public Button closeButton;                      // 닫기 버튼

    // 서버 탈퇴 API (예시 URL)
    private string guildSecedeURL = "https://pandaraisegame-guild.azurewebsites.net/api/LeaveGuild?code=ATH8OaSBlS_Knk-DA7a-E2QM7w3I-h7xoAIdiYe4XLp1AzFu6656MQ==";

    private void Awake()
    {
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
        // 팝업 초기 비활성
        if (GuildSecedeCanvas != null)
            GuildSecedeCanvas.SetActive(false);

        // 버튼 이벤트 연결
        if (secedeButton != null)
            secedeButton.onClick.AddListener(OnClickSecede);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseGuildSecedePopup);
    }

    /// <summary>
    /// 길드 권한 부여 팝업 열기
    /// </summary>
    public void OpenGuildSecedePopup()
    {
        if (GuildSecedeCanvas != null)
        {
            GuildSecedeCanvas.SetActive(true);
            Debug.Log("[GuildSecedePopup] 열기");
        }

    }

    /// <summary>
    /// 팝업 닫기
    /// </summary>
    public void CloseGuildSecedePopup()
    {
        if (GuildSecedeCanvas != null)
        {
            GuildSecedeCanvas.SetActive(false);
            Debug.Log("[GuildSecedePopup] 닫기");
        }
    }

    /// <summary>
    /// "계급 부여" 버튼 클릭
    /// </summary>
    private void OnClickSecede()
    {

        // 탈퇴 버튼 onclick event 추가 필요
        // 서버에 '계급 변경' 요청
        StartCoroutine(GuildSecedeCoroutine());
    }

    /// <summary>
    /// 서버에 '계급 변경' 요청을 보내는 코루틴
    /// </summary>
    private IEnumerator GuildSecedeCoroutine()
    {
        var requestBody = new
        {
            PlayFabId = GlobalData.playFabId,             // 내 PlayFab ID
            EntityToken = new
            {
                // 로그인 시점에 받은 엔터티 토큰
                EntityToken = GlobalData.entityToken.EntityToken,
                Entity = new
                {
                    Id = GlobalData.entityToken.Entity.Id,
                    Type = GlobalData.entityToken.Entity.Type
                }
            }
        };
        // JSON 직렬화
        string jsonData = JsonConvert.SerializeObject(requestBody);
        Debug.Log("[GuildSecedeCoroutine] 탈퇴 요청 JSON: " + jsonData);

        UnityWebRequest request = new UnityWebRequest(guildSecedeURL, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[GuildSecedeCoroutine] 서버 요청 실패: " + request.error);
            // 실패 안내
        }
        else
        {
            string responseJson = request.downloadHandler.text;
            Debug.Log($"[GuildSecedeCoroutine] 길드 탈퇴 요청 성공, 응답: {responseJson}");

            // 탈퇴 팝업 닫기
            CloseGuildSecedePopup();
            // 길드 팝업 닫기
            GuildPopupManager.Instance.CloseGuildPopup(); // 팝업 비활성화

        }
    }
}
