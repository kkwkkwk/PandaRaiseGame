using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class ProfilePopupManager : MonoBehaviour
{
    public static ProfilePopupManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject ProfileCanvas;     // 프로필 Canvas

    public TMP_Text player_name_Text;    // 닉네임 표시용 Text
    public TMP_Text sign_in_date_Text;   // 가입일 표시용 Text
    public TMP_Text login_count_Text;    // 로그인 횟수 표시용 Text

    // 실제 함수 URL & code 파라미터로 교체
    private string getProfileUrl = "https://pandaraisegame-profile.azurewebsites.net/api/GetProfile?code=A6sTFNwqBptB0yFUMDg9L9iWkQQNfcqGGNFtTMXlgNreAzFu6qM4bQ==";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[ProfilePopupManager] 싱글톤 Instance 할당 완료.");
        }
        else
        {
            Debug.LogWarning("[ProfilePopupManager] 이미 싱글톤 인스턴스가 존재하므로, 중복 객체를 파괴합니다.");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (ProfileCanvas != null)
        {
            StartCoroutine(StartSequence());
            Debug.Log("[ProfilePopupManager] Start() - 초기 상태: ProfileCanvas 비활성화");
        }
    }
    private IEnumerator StartSequence()
    {
        // 1) 먼저 프로필 조회
        yield return StartCoroutine(CallGetProfile());

        // 2) 코루틴 완료 후 비활성화
        ProfileCanvas.SetActive(false);
        Debug.Log("[LeaderboardPopupManager] StartSequence() 완료 후 Canvas 비활성화");
    }

    /// <summary>
    /// 프로필 팝업 열기
    /// </summary>
    public void OpenProfilePopup()
    {
        Debug.Log("[ProfilePopupManager] OpenProfilePopup() 호출됨");

        if (ProfileCanvas != null)
        {
            ProfileCanvas.SetActive(true);
            Debug.Log("[ProfilePopupManager] 프로필 열기 완료 (Canvas 활성화)");
        }
        else
        {
            Debug.LogWarning("[ProfilePopupManager] ProfileCanvas가 설정되어 있지 않습니다.");
        }

        // GlobalData.playFabId가 무엇인지 로그로 확인
        Debug.Log($"[ProfilePopupManager] 현재 GlobalData.playFabId: {GlobalData.playFabId}");

        // GetProfile 함수 호출
        StartCoroutine(CallGetProfile());
    }

    /// <summary>
    /// 프로필 팝업 닫기
    /// </summary>
    public void CloseProfilePopup()
    {
        Debug.Log("[ProfilePopupManager] CloseProfilePopup() 호출됨");

        if (ProfileCanvas != null)
        {
            ProfileCanvas.SetActive(false);
            Debug.Log("[ProfilePopupManager] 프로필 닫기 완료 (Canvas 비활성화)");
        }
        else
        {
            Debug.LogWarning("[ProfilePopupManager] ProfileCanvas가 설정되어 있지 않습니다.");
        }
    }

    /// <summary>
    /// 외부에서 프로필 데이터 새로고침할 때 호출
    /// </summary>
    public void RefreshProfileData()
    {
        Debug.Log("[ProfilePopupManager] RefreshProfileData() - 다시 GetProfile 호출합니다.");
        StartCoroutine(CallGetProfile());
    }

    /// <summary>
    /// Azure Function (GetProfile) 호출 코루틴
    /// </summary>
    private IEnumerator CallGetProfile()
    {
        Debug.Log("[ProfilePopupManager] CallGetProfile() 코루틴 시작");

        // 1) 전송할 JSON
        string jsonData = $"{{\"playFabId\":\"{GlobalData.playFabId}\"}}";
        Debug.Log($"[ProfilePopupManager] RequestBody JSON: {jsonData}");

        // 2) UnityWebRequest (POST)
        UnityWebRequest request = new UnityWebRequest(getProfileUrl, "POST");

        // 3) 업로드/다운로드 핸들러 설정
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        // 4) 헤더 설정
        request.SetRequestHeader("Content-Type", "application/json");
        Debug.Log("[ProfilePopupManager] UnityWebRequest 헤더 설정 완료. (Content-Type: application/json)");

        // 5) 요청 전송
        Debug.Log("[ProfilePopupManager] 서버에 GetProfile 요청 전송 중...");
        yield return request.SendWebRequest();
        Debug.Log("[ProfilePopupManager] 서버 응답 수신 완료.");

        // 6) 결과 확인
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[ProfilePopupManager] [GetProfile] 호출 실패: {request.error}");
        }
        else
        {
            // 서버 응답
            string responseJson = request.downloadHandler.text;
            Debug.Log($"[ProfilePopupManager] [GetProfile] 호출 성공. 서버 응답: {responseJson}");

            // 7) JSON -> ProfileData
            ProfileData profileData = JsonUtility.FromJson<ProfileData>(responseJson);
            if (profileData == null)
            {
                Debug.LogError("[ProfilePopupManager] JsonUtility.FromJson 결과가 null입니다. JSON 형식을 확인해 주세요.");
            }
            else
            {
                // 8) UI에 반영
                Debug.Log("[ProfilePopupManager] UI에 프로필 데이터 적용 시작...");
                Debug.Log($"[ProfilePopupManager] Parsed - displayName: {profileData.displayName}, createdKst: {profileData.createdKst}, lastLogin: {profileData.lastLogin}, loginCount: {profileData.loginCount}");

                if (player_name_Text != null)
                {
                    player_name_Text.text = profileData.displayName;
                    Debug.Log($"[ProfilePopupManager] player_name_Text 갱신 -> {profileData.displayName}");
                }
                else
                {
                    Debug.LogWarning("[ProfilePopupManager] player_name_Text 레퍼런스가 null입니다. Inspector에서 연결을 확인하세요.");
                }

                if (sign_in_date_Text != null)
                {
                    sign_in_date_Text.text = profileData.createdKst;
                    Debug.Log($"[ProfilePopupManager] sign_in_date_Text 갱신 -> {profileData.createdKst}");
                }
                else
                {
                    Debug.LogWarning("[ProfilePopupManager] sign_in_date_Text 레퍼런스가 null입니다. Inspector에서 연결을 확인하세요.");
                }

                if (login_count_Text != null)
                {
                    login_count_Text.text = profileData.loginCount.ToString();
                    Debug.Log($"[ProfilePopupManager] login_count_Text 갱신 -> {profileData.loginCount}");
                }
                else
                {
                    Debug.LogWarning("[ProfilePopupManager] login_count_Text 레퍼런스가 null입니다. Inspector에서 연결을 확인하세요.");
                }
            }
        }

        Debug.Log("[ProfilePopupManager] CallGetProfile() 코루틴 종료");
    }

    [System.Serializable]
    public class ProfileData
    {
        // ★ JSON 필드명과 완전히 동일하게 소문자로 변경
        public string playFabId;
        public string displayName;
        public string createdKst;
        public string lastLogin;
        public int loginCount;
    }
}
