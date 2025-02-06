using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DailyLoginPopupManager : MonoBehaviour
{

    public static DailyLoginPopupManager Instance { get; private set; }

    public GameObject DailyLoginCanvas;     // 출석체크 canvas
    const int sortOrder = 14;               // override sort order 순서

    public void OpenDailyLoginPopup()
    {   // 팝업 활성화 
        if (DailyLoginCanvas != null)
        {
            DailyLoginCanvas.SetActive(true);
            UnityEngine.Debug.Log("데일리 로그인 열기");
        }

    }
    public void CloseDailyLoginPopup()
    {   // 팝업 비활성화 
        if (DailyLoginCanvas != null)
        {
            DailyLoginCanvas.SetActive(false);
            UnityEngine.Debug.Log("데일리 로그인 닫기");
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
        Canvas canvas = GetComponent<Canvas>();
        canvas.overrideSorting = true;     // ✅ Override Sorting 활성화
        canvas.sortingOrder = sortOrder;   // ✅ 팝업이 앞쪽으로 오도록 설정
        UnityEngine.Debug.Log("메인 화면 시작");
        OpenDailyLoginPopup(); // 게임 접속시 출첵화면 띄우기
    }
}
