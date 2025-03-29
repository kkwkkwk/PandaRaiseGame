using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;

public class GuildManagerPopupManager : MonoBehaviour
{
    public static GuildManagerPopupManager Instance { get; private set; }

    [Header("Popup Canvas")]
    public GameObject guildManagerCanvas; // 길드 관리 팝업

    [Header("Notice UI")]
    public TMP_InputField noticeInputField;  // 길드 공지 수정용 InputField
    public Button fixButton;                 // "수정" 버튼
    public Button saveButton;                // "저장" 버튼

    [Header("Join Request List UI")]
    public GuildManagerPanelListCreator managerPanelListCreator;  // 가입 신청 리스트를 표시하는 스크립트

    [Header("Close Button")]
    public Button closeButton;  // 닫기 버튼

    // 실제 서버 API 주소 (예시 URL)
    private string updateNoticeURL = "https://example.com/UpdateGuildNotice";
    private string getJoinRequestsURL = "https://example.com/GetJoinRequests";
    private string approveJoinURL = "https://example.com/ApproveJoinRequest";
    private string declineJoinURL = "https://example.com/DeclineJoinRequest";

    // 가입 신청 목록
    private List<GuildManagerData> joinRequests = new List<GuildManagerData>();

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
        // 처음 팝업은 닫힌 상태
        if (guildManagerCanvas != null)
            guildManagerCanvas.SetActive(false);

        // 버튼 연결
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseGuildManagerPopup);

        if (fixButton != null)
            fixButton.onClick.AddListener(OnClickFixNotice);

        if (saveButton != null)
            saveButton.onClick.AddListener(OnClickSaveNotice);

        // 기본적으로 InputField는 읽기 전용 (수정 불가)
        if (noticeInputField != null)
            noticeInputField.interactable = false;
    }

    /// <summary>
    /// 길드 관리 팝업 열기
    /// - 공지 표시
    /// - 가입 신청 목록 받아오기
    /// </summary>
    public void OpenGuildManagerPopup(string notice)
    {
        if (guildManagerCanvas != null)
            guildManagerCanvas.SetActive(true);

        Debug.Log("[GuildManagerPopup] 길드 관리 팝업 열기");

        // 공지 표시
        if (noticeInputField != null)
            noticeInputField.text = notice;

        // 가입 신청 목록 갱신
        StartCoroutine(FetchJoinRequestsCoroutine());
    }

    /// <summary>
    /// 길드 관리 팝업 닫기
    /// </summary>
    public void CloseGuildManagerPopup()
    {
        if (guildManagerCanvas != null)
            guildManagerCanvas.SetActive(false);

        Debug.Log("[GuildManagerPopup] 길드 관리 팝업 닫기");
    }

    #region 공지 수정 기능
    /// <summary>
    /// "수정" 버튼 클릭 -> InputField 활성화
    /// </summary>
    private void OnClickFixNotice()
    {
        if (noticeInputField != null)
            noticeInputField.interactable = true;
        Debug.Log("[GuildManagerPopup] 공지 수정 모드 활성화");
    }

    /// <summary>
    /// "저장" 버튼 클릭 -> 서버에 수정 요청
    /// </summary>
    private void OnClickSaveNotice()
    {
        if (noticeInputField == null) return;

        string newNotice = noticeInputField.text;
        StartCoroutine(UpdateNoticeCoroutine(newNotice));
    }

    private IEnumerator UpdateNoticeCoroutine(string newNotice)
    {
        // 서버에 보낼 JSON: groupId + entityToken + newNotice
        var requestJson = new
        {
            entityToken = new
            {
                Entity = new
                {
                    Id = GlobalData.entityToken.Entity.Id,
                    Type = GlobalData.entityToken.Entity.Type
                },
                EntityToken = GlobalData.entityToken.EntityToken,
                TokenExpiration = GlobalData.entityToken.TokenExpiration
            },
            newNotice = newNotice
        };

        string jsonData = JsonConvert.SerializeObject(requestJson);
        Debug.Log("[UpdateNotice] 요청: " + jsonData);

        UnityWebRequest request = new UnityWebRequest(updateNoticeURL, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[UpdateNotice] 실패: " + request.error);
            ToastManager.Instance.ShowToast("길드 공지 수정 실패");
        }
        else
        {
            string responseJson = request.downloadHandler.text;
            Debug.Log("[UpdateNotice] 성공, 응답: " + responseJson);
            ToastManager.Instance.ShowToast("길드 공지가 수정되었습니다!");
            noticeInputField.interactable = false;

            // 필요하다면 다시 길드 정보 재조회
            GuildTabPanelController.Instance.RefreshGuildData();
        }
    }
    #endregion

    #region 가입 신청 관리 기능

    /// <summary>
    /// 서버에서 가입 신청 목록 받아오기
    /// </summary>
    private IEnumerator FetchJoinRequestsCoroutine()
    {
        // 예시로 playFabId만 전송
        var payload = new
        {
            playFabId = GlobalData.playFabId
        };
        string jsonData = JsonConvert.SerializeObject(payload);

        UnityWebRequest request = new UnityWebRequest(getJoinRequestsURL, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[GuildManagerPopup] 가입 신청 목록 불러오기 실패: " + request.error);
            ToastManager.Instance.ShowToast("가입 신청 목록을 불러올 수 없습니다.");
        }
        else
        {
            string resp = request.downloadHandler.text;
            Debug.Log("[GuildManagerPopup] 가입 신청 목록 응답: " + resp);

            try
            {
                joinRequests = JsonConvert.DeserializeObject<List<GuildManagerData>>(resp);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[GuildManagerPopup] JSON 파싱 오류: " + ex.Message);
                ToastManager.Instance.ShowToast("가입 신청 목록 파싱 실패");
                yield break;
            }

            // 리스트 UI 갱신
            if (managerPanelListCreator != null)
                managerPanelListCreator.SetGuildManagerList(joinRequests);
        }
    }

    /// <summary>
    /// 가입 신청 승인
    /// </summary>
    public void ApproveJoinRequest(GuildManagerData requestData)
    {
        StartCoroutine(ApproveJoinRequestCoroutine(requestData));
    }

    private IEnumerator ApproveJoinRequestCoroutine(GuildManagerData requestData)
    {
        var payload = new
        {
            playFabId = GlobalData.playFabId,
            applicantId = requestData.applicantId
        };
        string jsonData = JsonConvert.SerializeObject(payload);

        UnityWebRequest request = new UnityWebRequest(approveJoinURL, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[GuildManagerPopup] 가입 승인 실패: " + request.error);
            ToastManager.Instance.ShowToast("가입 승인에 실패했습니다.");
        }
        else
        {
            string resp = request.downloadHandler.text;
            Debug.Log("[GuildManagerPopup] 가입 승인 성공, 응답: " + resp);
            ToastManager.Instance.ShowToast("가입 신청이 승인되었습니다!");

            // 다시 최신 데이터 반영
            GuildTabPanelController.Instance.RefreshGuildData();
            StartCoroutine(FetchJoinRequestsCoroutine());
        }
    }

    /// <summary>
    /// 가입 신청 거절
    /// </summary>
    public void DeclineJoinRequest(GuildManagerData requestData)
    {
        StartCoroutine(DeclineJoinRequestCoroutine(requestData));
    }

    private IEnumerator DeclineJoinRequestCoroutine(GuildManagerData requestData)
    {
        var payload = new
        {
            playFabId = GlobalData.playFabId,
            applicantId = requestData.applicantId
        };
        string jsonData = JsonConvert.SerializeObject(payload);

        UnityWebRequest request = new UnityWebRequest(declineJoinURL, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[GuildManagerPopup] 가입 거절 실패: " + request.error);
            ToastManager.Instance.ShowToast("가입 거절에 실패했습니다.");
        }
        else
        {
            string resp = request.downloadHandler.text;
            Debug.Log("[GuildManagerPopup] 가입 거절 성공, 응답: " + resp);
            ToastManager.Instance.ShowToast("가입 신청이 거절되었습니다.");

            GuildTabPanelController.Instance.RefreshGuildData();
            StartCoroutine(FetchJoinRequestsCoroutine());
        }
    }
    #endregion
}
