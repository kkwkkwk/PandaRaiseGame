using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class LoginPlayFabUser : MonoBehaviour
{
    public static LoginPlayFabUser Instance;
    public TextMeshProUGUI logText; // UI 로그 표시용

    // 로그인 성공 시 PlayFab ID를 전달하는 이벤트
    public static event Action<string> OnPlayFabLoginSuccessEvent;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    /// <summary>
    /// GPGS에서 받은 Auth Code를 사용하여 PlayFab 로그인 요청
    /// </summary>
    /// <param name="serverAuthCode">Google Auth Code</param>

    public void LoginToPlayFab(string serverAuthCode)
    {
        LogMessage("PlayFab 로그인 시도 중...");

        // PlayFab 로그인 요청 생성
        var request = new LoginWithGoogleAccountRequest
        {
            ServerAuthCode = serverAuthCode, // GPGS에서 받은 인증 코드
            CreateAccount = true, // 계정이 없으면 새로 생성
            TitleId = PlayFabSettings.TitleId // PlayFab 타이틀 ID
        };

        // PlayFab 로그인 실행
        PlayFabClientAPI.LoginWithGoogleAccount(request, OnPlayFabLoginSuccess, OnPlayFabLoginFailure);
    }

    /// <summary>
    /// PlayFab 로그인 성공 시 호출
    /// </summary>
    private void OnPlayFabLoginSuccess(LoginResult result)
    {
        LogMessage("✅ PlayFab 로그인 성공! PlayFab ID: " + result.PlayFabId);
        // ✅ 로그인 성공 이벤트 발생 → PlayFab ID 전달
        OnPlayFabLoginSuccessEvent?.Invoke(result.PlayFabId);

        // 로그인 성공 시 메인 화면으로 이동
        SceneManager.LoadScene("Main_Screen");
    }

    /// <summary>
    /// PlayFab 로그인 실패 시 호출
    /// </summary>
    private void OnPlayFabLoginFailure(PlayFabError error)
    {
        LogMessage("❌ PlayFab 로그인 실패: " + error.GenerateErrorReport());
    }

    /// <summary>
    /// UI에 로그 메시지를 표시하는 함수
    /// </summary>
    private void LogMessage(string message)
    {
        Debug.Log(message);
        if (logText != null)
        {
            logText.text += message + "\n";
        }
    }
}