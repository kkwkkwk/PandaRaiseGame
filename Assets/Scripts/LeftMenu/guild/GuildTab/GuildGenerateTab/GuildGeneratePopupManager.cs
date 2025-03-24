using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;            // TMP_InputField 사용 시
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;

public class GuildGeneratePopupManager : MonoBehaviour
{
    public static GuildGeneratePopupManager Instance { get; private set; }

    [Header("Popup Canvas")]
    public GameObject GuildGenerateCanvas;     // 길드 생성 패널 (Canvas)

    [Header("UI References")]
    public TMP_InputField guildNameInputField; // 길드 이름 입력 필드
    public Button createGuildButton;           // 길드 생성 버튼

  
    private string createGuildURL = "https://pandaraisegame-guild.azurewebsites.net/api/CreateGuild?code=PBi5G3ni054ZB9hvtm63r63waa90OK-WygcCgVFkdYkLAzFu9BH6Xw==";

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

    void Start()
    {
        // 팝업 초기 비활성화
        if (GuildGenerateCanvas != null)
        {
            GuildGenerateCanvas.SetActive(false);
        }

        // 버튼 클릭 이벤트 연결
        if (createGuildButton != null)
        {
            createGuildButton.onClick.AddListener(OnClickCreateGuild);
        }
    }

    /// <summary>
    /// 길드 생성 팝업 열기
    /// </summary>
    public void OpenGuildGeneratePopup()
    {
        if (GuildGenerateCanvas != null)
        {
            GuildGenerateCanvas.SetActive(true);
            Debug.Log("길드 생성 팝업 열림");
        }
    }

    /// <summary>
    /// 길드 생성 팝업 닫기
    /// </summary>
    public void CloseGuildGeneratePopup()
    {
        if (GuildGenerateCanvas != null)
        {
            GuildGenerateCanvas.SetActive(false);
            Debug.Log("길드 생성 팝업 닫힘");
        }
    }

    /// <summary>
    /// [버튼] 길드 생성 버튼 클릭 시 호출
    /// </summary>
    private void OnClickCreateGuild()
    {
        // 1) 입력 필드에서 길드 이름을 가져옴
        string guildName = guildNameInputField != null
                           ? guildNameInputField.text.Trim()
                           : "";

        // 2) 길드 이름 길이 제한 체크 (1~6 글자)
        //    (추가로 영문/숫자까지 포함해도 되는지 정책에 따라 다름)
        if (string.IsNullOrEmpty(guildName))
        {
            Debug.LogWarning("길드 이름이 비어 있습니다!");
            // 만약 Toast 나 안내 메시지를 띄우고 싶다면 여기서 처리
            ToastManager.Instance.ShowToast("길드 이름을 입력해주세요.");
            return;
        }
        if (guildName.Length > 6)
        {
            Debug.LogWarning("길드 이름이 7글자 이상입니다. (최대 6글자)");
            // 마찬가지로 안내 메시지 처리
            ToastManager.Instance.ShowToast("길드 이름은 최대 6글자까지만 입력 가능합니다.");
            return;
        }

        // 3) 조건 통과 -> 코루틴으로 서버에 길드 생성 요청
        StartCoroutine(CallCreateGuildCoroutine(guildName));
    }

    /// <summary>
    /// 서버에 길드 생성 요청을 보내는 코루틴
    /// </summary>
    private IEnumerator CallCreateGuildCoroutine(string guildName)
    {
        // 예시 JSON { "playFabId":"...", "guildName":"..." }
        // 필요 시 GlobalData.playFabId 대신 entityToken 등 추가 정보도 보낼 수 있음.
        string jsonData = $"{{\"playFabId\":\"{GlobalData.playFabId}\",\"guildName\":\"{guildName}\"}}";
        Debug.Log($"[CreateGuild] 요청 JSON: {jsonData}");

        UnityWebRequest request = new UnityWebRequest(createGuildURL, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[CreateGuild] 서버 요청 실패: {request.error}");
            // 서버 오류 시에도 안내 메시지를 띄우거나 처리
        }
        else
        {
            string responseJson = request.downloadHandler.text;
            Debug.Log($"[CreateGuild] 요청 성공. 응답: {responseJson}");

            // (선택) 응답 JSON 파싱
            // CreateGuildResponse res = JsonConvert.DeserializeObject<CreateGuildResponse>(responseJson);
            // if (res != null) { ... }

            // 팝업 닫기
            CloseGuildGeneratePopup();

            // (필요 시) 길드 UI 리프레시
            // GuildTabPanelController.Instance.RefreshGuildData();
            // 또는 OnEnable() 다시 호출 등
        }
    }

    // (선택) 서버 응답 DTO
    [System.Serializable]
    public class CreateGuildResponse
    {
        public string guildId;
        public string guildName;
        public string message;
    }
}
