using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewardPopupManager : MonoBehaviour
{

    public static RewardPopupManager Instance { get; private set; }

    public GameObject RewardCanvas;     // 리워드 canvas

    public void OpenRewardPopup()
    {   // 팝업 활성화 
        if (RewardCanvas != null)
        {
            RewardCanvas.SetActive(true);
            UnityEngine.Debug.Log("리워드 열기");
        }

    }
    public void CloseRewardPopup()
    {   // 팝업 비활성화 
        if (RewardCanvas != null)
        {
            RewardCanvas.SetActive(false);
            UnityEngine.Debug.Log("리워드 닫기");
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
    { // 처음에 팝업 패널 비활성화 
        if (RewardCanvas != null)
        {
            RewardCanvas.SetActive(false);
        }
    }
}
