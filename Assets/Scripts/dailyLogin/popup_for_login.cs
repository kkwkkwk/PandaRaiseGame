using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class popup_for_login : MonoBehaviour
{

    public static popup_for_login Instance { get; private set; }

    public GameObject dailyLoginCanvas;     // 출석체크 canvas

    public void OpenDailyLoginPopup()
    {   // 팝업 활성화 
        if (dailyLoginCanvas != null)
        {
            dailyLoginCanvas.SetActive(true);
        }

    }
    public void CloseDailyLoginPopup()
    {   // 팝업 비활성화 
        if (dailyLoginCanvas != null)
        {
            dailyLoginCanvas.SetActive(false);
        }
    }

    private void Awake() // Unity의 Awake() 메서드에서 싱글톤 설정
    {
        // 만약 Instance가 비어 있으면, 이 스크립트를 싱글톤 인스턴스로 설정
        if (Instance == null)
        {
            Instance = this; // 현재 객체를 싱글톤으로 지정
        }
        else
        {
            // 이미 싱글톤 인스턴스가 존재하면, 중복된 객체를 제거
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start() 
    {
        OpenDailyLoginPopup(); // 게임 접속시 출첵화면 띄우기
    }

}
