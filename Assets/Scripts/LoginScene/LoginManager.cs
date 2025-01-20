using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Scene 전환에 사용
using UnityEngine.UI; // UI 컨트롤에 사용

public class LoginManager : MonoBehaviour
{
    public Button GoogleLoginButton; // 구글 로그인 버튼
    public Button AppleLoginButton; // 애플 로그인 버튼
    //public Button GuestLoginButton; // 비회원 로그인 버튼

    // Start is called before the first frame update
    void Start()
    {
        // 각 버튼에 클릭 이벤트 등록
        GoogleLoginButton.onClick.AddListener(OnGoogleLoginClick);
        AppleLoginButton.onClick.AddListener(OnAppleLoginClick);
        //GuestLoginButton.onClick.AddListener(OnGuestLoginClick);
    }

    // 구글 로그인 버튼 클릭 시
    void OnGoogleLoginClick()
    {
        UnityEngine.Debug.Log("Google Login Button Clicked");
        SceneManager.LoadScene("Main_Screen"); // Main_Screen 씬으로 전환
    }

    // 애플 로그인 버튼 클릭 시
    void OnAppleLoginClick()
    {
        UnityEngine.Debug.Log("Apple Login Button Clicked");
        SceneManager.LoadScene("Main_Screen"); // Main_Screen 씬으로 전환
    }

    // 비회원 로그인 버튼 클릭 시
    /*void OnGuestLoginClick()
    {
        UnityEngine.Debug.Log("Guest Login Button Clicked");
        SceneManager.LoadScene("Main_Screen"); // Main_Screen 씬으로 전환
    }*/

    // Update is called once per frame
    void Update()
    {

    }
}
