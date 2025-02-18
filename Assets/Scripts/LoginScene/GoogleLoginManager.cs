using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Scene 전환에 사용
using UnityEngine.UI; // UI 컨트롤에 사용
using Google;

public class GoogleLoginManager : MonoBehaviour
{
    public Button GoogleLoginButton; // ✅ Google 로그인 버튼
    private GoogleSignInConfiguration configuration; // ✅ Google 로그인 설정 객체

    void Start()
    {
        // ✅ Google 로그인 설정 초기화
        configuration = new GoogleSignInConfiguration
        {
            RequestIdToken = true, // Google ID 토큰 요청 (Firebase 등과 연동 가능)
            WebClientId = "74415822987-5s663358988psf2bv1bpmae6i1uh7olj.apps.googleusercontent.com",  // Google Cloud Console에서 생성한 Web Client ID
            RequestAuthCode = true
        };

        // ✅ 로그인 버튼 클릭 시 OnGoogleLoginClick 함수 실행
        GoogleLoginButton.onClick.AddListener(OnGoogleLoginClick);
    }

    void OnGoogleLoginClick()
    {
        // ✅ Unity 에디터에서는 실행 안 하고, Android & iOS에서만 실행하도록 설정
        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
        {
            // ✅ Google 로그인 설정 적용
            GoogleSignIn.Configuration = configuration;

            // ✅ Google 로그인 요청 실행
            GoogleSignIn.DefaultInstance.SignIn().ContinueWith(task =>
            {
                if (task.IsCanceled)
                {
                    // ❌ 사용자가 Google 로그인 취소한 경우
                    Debug.LogWarning("❌ Google 로그인 취소됨.");
                }
                else if (task.IsFaulted)
                {
                    // ❌ 로그인 실패 (예: 네트워크 오류, 권한 문제 등)
                    Debug.LogError("❌ Google 로그인 실패: " + task.Exception);
                }
                else
                {
                    // ✅ Google 로그인 성공
                    Debug.Log("✅ Google 로그인 성공!");
                    Debug.Log("사용자 이메일: " + task.Result.Email); // 이메일 출력
                    Debug.Log("사용자 이름: " + task.Result.DisplayName); // 이름 출력
                    if (!string.IsNullOrEmpty(task.Result.AuthCode))
                    {
                        // 이 authCode를 PlayFab에 전달
                        LoginPlayFabUser.Instance.GoogleLoginToPlayFab(task.Result.AuthCode);
                    }
                }
            });
        }
        else
        {
            SceneManager.LoadScene("Main_Screen");
        }
    }
}
