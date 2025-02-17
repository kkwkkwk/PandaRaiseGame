using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GooglePlayGames; // GPGS 로그인
using GooglePlayGames.BasicApi;
using TMPro; // TextMeshPro 사용


public class GPGSLoginManager : MonoBehaviour
{
    public TextMeshProUGUI logText; // UI 로그 표시용

    void Start()
    {
        // ✅ Android에서만 GPGS 초기화 실행
        if (Application.platform == RuntimePlatform.Android)
        {
            InitializeGPGS();
        }
        else
        {
            LogMessage("❌ GPGS는 Android에서만 실행할 수 있습니다.");
        }
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
        }
    }

    void LogMessage(string message)
    {
        Debug.Log(message); // Unity 콘솔에도 출력
        if (logText == null)
        {
            logText = GameObject.Find("logText")?.GetComponent<TextMeshProUGUI>();
        }
        logText.text += message + "\n"; // UI에 로그 추가

        // 로그가 너무 길어지면 자동으로 초기화
        if (logText.text.Length > 1000)
        {
            logText.text = "로그 초기화됨\n";
        }
    }
}
