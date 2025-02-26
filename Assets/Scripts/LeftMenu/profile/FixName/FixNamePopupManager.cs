using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;    // ★ UnityEngine.UI.Button 사용 시
using TMPro;            // ★ TextMeshPro 사용 시

public class FixNamePopupManager : MonoBehaviour
{
    public static FixNamePopupManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject FixNameCanvas;        // 팝업 Canvas

    public TMP_InputField inputNewName;     // 새 닉네임 입력 필드
    public Button changeNameButton;         // ★ "닉네임 수정" 버튼 (Inspector 연결)

    // ★ Azure Function (ChangeDisplayName) 호출 주소 (실제 URL & 함수 코드로 교체)
    private string changeDisplayNameUrl = "https://pandaraisegame-profile.azurewebsites.net/api/ChangeDisplayName?code=OcSrBoWr31PlcDjUKUamhwRei2dX7hKMarEOWXWuL3sFAzFuPy_a5g==";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 시작할 때 팝업 숨기기
        if (FixNameCanvas != null)
        {
            FixNameCanvas.SetActive(false);
        }

        // ★ 닉네임 수정 버튼에 이벤트 연결
        if (changeNameButton != null)
        {
            changeNameButton.onClick.AddListener(OnClickChangeName);
        }
    }

    /// <summary>
    /// 팝업 열기
    /// </summary>
    public void OpenFixNamePopup()
    {
        if (FixNameCanvas != null)
        {
            FixNameCanvas.SetActive(true);
            Debug.Log("이름 수정 팝업 열기");
        }
    }

    /// <summary>
    /// 팝업 닫기
    /// </summary>
    public void CloseFixNamePopup()
    {
        if (FixNameCanvas != null)
        {
            FixNameCanvas.SetActive(false);
            Debug.Log("이름 수정 팝업 닫기");
        }
    }

    /// <summary>
    /// [버튼 클릭 시 호출될 함수]
    /// </summary>
    private void OnClickChangeName()
    {
        // InputField / TMP_InputField에서 새 닉네임을 읽어옴
        string newName = inputNewName != null ? inputNewName.text.Trim() : "";

        // 닉네임이 비어 있지 않을 때만 시도
        if (!string.IsNullOrEmpty(newName))
        {
            StartCoroutine(CallChangeDisplayName(newName));
        }
        else
        {
            Debug.LogWarning("새 닉네임이 비어있습니다!");
        }
    }

    /// <summary>
    /// Azure Function (ChangeDisplayName) 호출 코루틴
    /// </summary>
    private IEnumerator CallChangeDisplayName(string newName)
    {
        // 1) 요청 JSON 생성
        //    GlobalData.playFabId는 LoginPlayFabUser.cs에서 로그인 성공 시 저장한 값
        string jsonData = $"{{\"playFabId\":\"{GlobalData.playFabId}\",\"newDisplayName\":\"{newName}\"}}";
        Debug.Log($"[ChangeDisplayName] 요청 JSON: {jsonData}");

        // 2) UnityWebRequest 설정
        UnityWebRequest request = new UnityWebRequest(changeDisplayNameUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        // 3) 헤더 설정
        request.SetRequestHeader("Content-Type", "application/json");

        // 4) 요청 전송 및 대기
        yield return request.SendWebRequest();

        // 5) 결과 확인
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[ChangeDisplayName] 호출 실패: {request.error}");
        }
        else
        {
            string responseJson = request.downloadHandler.text;
            Debug.Log($"[ChangeDisplayName] 호출 성공. 응답: {responseJson}");

            CloseFixNamePopup();
            ProfilePopupManager.Instance.RefreshProfileData();
        }
    }

    // (선택) JSON 파싱용 응답 DTO
    [System.Serializable]
    public class ChangeDisplayNameResponse
    {
        public string PlayFabId;
        public string NewDisplayName;
        public string Message;
    }
}
