using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Scene 전환에 사용
using UnityEngine.UI; // UI 컨트롤에 사용

public class AppleLoginManager : MonoBehaviour
{
    public Button AppleLoginButton; // 애플 로그인 버튼
    // Start is called before the first frame update
    void Start()
    {
        // 각 버튼에 클릭 이벤트 등록
        AppleLoginButton.onClick.AddListener(OnAppleLoginClick);
    }

    // 애플 로그인 버튼 클릭 시
    void OnAppleLoginClick()
    {
        Debug.Log("Apple Login Button Clicked");
        SceneManager.LoadScene("Main_Screen"); // Main_Screen 씬으로 전환
    }
}
