using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Scene 전환에 사용
using UnityEngine.UI; // UI 컨트롤에 사용
using GooglePlayGames; // GPGS 로그인
using GooglePlayGames.BasicApi;
using PlayFab; // PlayFab 연동
using PlayFab.ClientModels;
using TMPro; // TextMeshPro 사용


public class LoginManager : MonoBehaviour
{
    public Button GoogleLoginButton; // 구글 로그인 버튼
    public Button AppleLoginButton; // 애플 로그인 버튼
    public TextMeshProUGUI logText; // UI 로그 표시용

    void LogMessage(string message)
    {
        Debug.Log(message); // Unity 콘솔에도 출력
        logText.text += message + "\n"; // UI에 로그 추가

        // 로그가 너무 길어지면 자동으로 초기화
        if (logText.text.Length > 1000)
        {
            logText.text = "로그 초기화됨\n";
        }
    }

    void Start()
    {
        // GPGS 초기화
        InitializeGPGS();
        // 각 버튼에 클릭 이벤트 등록
        GoogleLoginButton.onClick.AddListener(OnGoogleLoginClick);
        AppleLoginButton.onClick.AddListener(OnAppleLoginClick);        
    }

    private void InitializeGPGS()
    {
        // GPGS 로그인 세팅
        PlayGamesPlatform.DebugLogEnabled = true;
        PlayGamesPlatform.Activate();
        LogMessage("GPGS 초기화 완료.");
        PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
    }

    internal void ProcessAuthentication(SignInStatus status)
    {
        if (status == SignInStatus.Success) // 로그인 성공 시
        {
            LogMessage("✅ GPGS 로그인 성공!");

            // ✅ 기존 코드 변경 부분: PlayFab 로그인 요청을 추가
            PlayGamesPlatform.Instance.RequestServerSideAccess(false, authCode =>
            {
                if (!string.IsNullOrEmpty(authCode))
                {
                    LogMessage("✅ Google Auth Code 가져오기 성공");
                    LoginPlayFabUser.Instance.LoginToPlayFab(authCode); // PlayFab 로그인 요청
                    // SceneManager.LoadScene("Main_Screen");
                }
                else
                {
                    LogMessage("❌ Google Auth Code 가져오기 실패");
                }
            });
        }
        else //로그인 실패 시
        {
            LogMessage("GPGS 로그인 실패: " + status.ToString());
            // Disable your integration with Play Games Services or show a login button
            // to ask users to sign-in. Clicking it should call
            // PlayGamesPlatform.Instance.ManuallyAuthenticate(ProcessAuthentication).
        }
    }
    /*
    private void ProcessServerAuthCode(string serverAuthCode)
    {
        Debug.Log("Server Auth Code: " + serverAuthCode);   // ServerAuthCode 반환

        var request = new LoginWithGooglePlayGamesServicesRequest   // Google에서 방금 받은 AuthCode를 PlayFab에 ServerAuthCode 매개 변수로 전달
        {
            ServerAuthCode = serverAuthCode,
            CreateAccount = true,
            TitleId = PlayFabSettings.TitleId
        };
        // PlayFab 로그인 성공 실패 유무
        PlayFabClientAPI.LoginWithGooglePlayGamesServices(request, OnLoginWithGooglePlayGamesServicesSuccess, OnLoginWithGooglePlayGamesServicesFailure);
    }

    
    private void OnLoginWithGooglePlayGamesServicesSuccess(LoginResult result)  // PlayFab 로그인 성공
    {
        Debug.Log("PF Login Success LoginWithGooglePlayGamesServices");
    }

    private void OnLoginWithGooglePlayGamesServicesFailure(PlayFabError error)  // PlayFab 로그인 실패
    {
        Debug.Log("PF Login Failure LoginWithGooglePlayGamesServices: " + error.GenerateErrorReport());
    }
    */

    // 구글 로그인 버튼 클릭 시
    void OnGoogleLoginClick()
    {
        Debug.Log("Google Login Button Clicked");
        PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
        SceneManager.LoadScene("Main_Screen"); // Main_Screen 씬으로 전환
    }

    // 애플 로그인 버튼 클릭 시
    void OnAppleLoginClick()
    {
        Debug.Log("Apple Login Button Clicked");
        SceneManager.LoadScene("Main_Screen"); // Main_Screen 씬으로 전환
    }


    // Update is called once per frame
    void Update()
    {

    }
}
