using System.Collections;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using UnityEngine.Networking;


[Serializable]
public class PlayFabIdRequestData
{
    public string playFabId;
}

public class LoginPlayFabUser : MonoBehaviour
{
    public static LoginPlayFabUser Instance;
    public TextMeshProUGUI logText; // UI 로그 표시용

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    // 로그인 성공 시 PlayFab ID를 전달하는 이벤트
    public static event Action<string> OnPlayFabLoginSuccessEvent;

    // Azure Function 호출 URL (실제 URL과 함수 키로 교체)
    private string loginCountUrl = "https://pandaraisegame-dailylogin.azurewebsites.net/api/DailyLoginCount?code=9ej0IZ4F5KNmHmWEc4GJxYJHf2WkGQgkQKc8yS-o6j2jAzFu_Cm-vg==";
    private string loginRewardUrl = "https://pandaraisegame-dailylogin.azurewebsites.net/api/DailyLoginReward?code=sSAMUqYTNoDQh02WphrHPxKYRdRD75Nfr9RDARtWv6CEAzFuE4855A==";

    /// <summary>
    /// GPGS에서 받은 Auth Code를 사용하여 PlayFab 로그인 요청 (GPGS 방식)
    /// </summary>
    /// <param name="serverAuthCode">Google Auth Code</param>
    public void GPGSLoginToPlayFab(string serverAuthCode)
    {
        LogMessage("GPGS-PlayFab 로그인 시도 중...");
        LogMessage("authcode: " + serverAuthCode);
        LogMessage("titleid: " + PlayFabSettings.TitleId);

        // PlayFab 로그인 요청 생성
        var request = new LoginWithGooglePlayGamesServicesRequest
        {
            ServerAuthCode = serverAuthCode,
            CreateAccount = true,
            TitleId = PlayFabSettings.TitleId
        };
        PlayFabClientAPI.LoginWithGooglePlayGamesServices(request, OnPlayFabLoginSuccess, OnPlayFabLoginFailure);
    }

    /// <summary>
    /// GoogleSignIn을 통한 PlayFab 로그인 요청 (Google 방식)
    /// </summary>
    public void GoogleLoginToPlayFab(string serverAuthCode)
    {
        LogMessage("Google-PlayFab 로그인 시도 중...");
        LogMessage("authcode: " + serverAuthCode);
        LogMessage("titleid: " + PlayFabSettings.TitleId);

        var request = new LoginWithGoogleAccountRequest
        {
            ServerAuthCode = serverAuthCode,
            CreateAccount = true,
            TitleId = PlayFabSettings.TitleId
        };
        PlayFabClientAPI.LoginWithGoogleAccount(request, OnPlayFabLoginSuccess, OnPlayFabLoginFailure);
    }

    /// <summary>
    /// PlayFab 로그인 성공 시 호출됨.
    /// 여기서 Azure Function을 호출하여 데일리 리워드를 지급한 후 메인 화면으로 전환합니다.
    /// </summary>
    private void OnPlayFabLoginSuccess(LoginResult result)
    {
        GlobalData.playFabId = result.PlayFabId; // PlayFab ID 저장
        GlobalData.entityToken = result.EntityToken;
        LogMessage("✅ PlayFab 로그인 성공! PlayFab ID: " + result.PlayFabId + result.EntityToken);
        OnPlayFabLoginSuccessEvent?.Invoke(result.PlayFabId);

        // Azure Function 호출을 위해 코루틴 시작
        StartCoroutine(CallDailyLoginFunctions(result.PlayFabId));
    }

    /// <summary>
    /// PlayFab 로그인 실패 시 호출됨.
    /// </summary>
    private void OnPlayFabLoginFailure(PlayFabError error)
    {
        LogMessage("❌ PlayFab 로그인 실패: " + error.GenerateErrorReport());
    }

    /// <summary>
    /// Azure Function(DailyLoginCount)을 호출하여 
    /// 플레이어의 로그인 횟수를 +1 업데이트 하는 코루틴.
    /// </summary>
    private IEnumerator CallDailyLoginCount(string playFabId)
    {
        // 1. JSON 직렬화 (RequestBody)
        PlayFabIdRequestData requestData = new PlayFabIdRequestData { playFabId = playFabId };
        string jsonData = JsonUtility.ToJson(requestData);

        LogMessage("[DailyLoginCount] 전송할 JSON: " + jsonData);

        // 2. UnityWebRequest 생성
        UnityWebRequest request = new UnityWebRequest(loginCountUrl, "POST");

        // 3. JSON 데이터를 바이트 배열로 변환 후 업로드 핸들러에 세팅
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        // 4. 다운로드 핸들러 세팅
        request.downloadHandler = new DownloadHandlerBuffer();
        // 5. Content-Type 설정
        request.SetRequestHeader("Content-Type", "application/json");

        LogMessage("[DailyLoginCount] Azure Function 호출 시작...");

        // 6. 전송 및 대기
        yield return request.SendWebRequest();

        // 7. 응답 결과 확인
        if (request.result != UnityWebRequest.Result.Success)
        {
            LogMessage("[DailyLoginCount] 호출 실패: " + request.error);
        }
        else
        {
            LogMessage("[DailyLoginCount] 호출 성공: " + request.downloadHandler.text);
        }
    }


    /// <summary>
    /// Azure Function을 호출하여 데일리 리워드를 지급하는 코루틴.
    /// 플레이어의 PlayFabId를 포함한 JSON 데이터를 전송합니다.
    /// </summary>
    /// <param name="playFabId">플레이어의 PlayFabId (string)</param>
    /// <returns>IEnumerator</returns>
    private IEnumerator CallDailyLoginReward(string playFabId)
    {
        // 1. 직렬화 가능한 클래스를 사용해 JSON 데이터 생성:
        PlayFabIdRequestData requestData = new PlayFabIdRequestData { playFabId = playFabId };
        string jsonData = JsonUtility.ToJson(requestData);

        // 디버그용: 전송할 JSON 확인
        LogMessage("전송할 JSON: " + jsonData);

        // 2. UnityWebRequest 생성 (POST 방식)
        UnityWebRequest request = new UnityWebRequest(loginRewardUrl, "POST");

        // 3. 요청 본문 설정: JSON 데이터를 UTF-8 바이트 배열로 변환
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);

        // 4. 응답 핸들러 설정: 서버로부터 응답 데이터를 받을 버퍼 생성
        request.downloadHandler = new DownloadHandlerBuffer();

        // 5. HTTP 헤더 설정: Content-Type을 application/json으로 지정
        request.SetRequestHeader("Content-Type", "application/json");

        LogMessage("Azure Function 호출 시작...");

        // 6. HTTP 요청 전송 및 대기
        yield return request.SendWebRequest();

        // 7. 응답 결과 확인: 실패 또는 성공에 따라 로그 출력
        if (request.result != UnityWebRequest.Result.Success)
        {
            LogMessage("Azure Function 호출 실패: " + request.error);
        }
        else
        {
            LogMessage("Azure Function 호출 성공: " + request.downloadHandler.text);
        }

        // 8. 함수 호출 후 메인 화면으로 전환
        SceneManager.LoadScene("Main_Screen");
    }

    private IEnumerator CallDailyLoginFunctions(string playFabId)
    {
        // 1) 로그인 횟수 업데이트 먼저
        yield return StartCoroutine(CallDailyLoginCount(playFabId));

        // 2) 보상 지급
        yield return StartCoroutine(CallDailyLoginReward(playFabId));

        // 3) 모든 작업이 끝난 후 씬 전환
        SceneManager.LoadScene("Main_Screen");
    }


    /// <summary>
    /// UI에 로그 메시지를 출력하는 함수.
    /// </summary>
    /// <param name="message">출력할 메시지 (string)</param>
    private void LogMessage(string message)
    {
        Debug.Log(message);
        if (logText != null)
        {
            logText.text += message + "\n";
        }
    }
}
