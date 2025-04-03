using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuildPopupManager : MonoBehaviour
{

    public static GuildPopupManager Instance { get; private set; }

    public GameObject GuildCanvas;     // 길드 canvas

    public void OpenGuildPopup()
    {   // 팝업 활성화 
        if (GuildCanvas != null)
        {
            GuildCanvas.SetActive(true);
            UnityEngine.Debug.Log("길드 열기");
        }

    }
    public void CloseGuildPopup()
    {   // 팝업 비활성화 
        if (GuildCanvas != null)
        {
            GuildCanvas.SetActive(false);
            UnityEngine.Debug.Log("길드 닫기");

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
        if (GuildCanvas != null)
        {
            StartCoroutine(StartSequence());
        }
    }
    private IEnumerator StartSequence()
    {
        // 1) 길드 팝업 열기
        if (GuildCanvas != null)
        {
            GuildCanvas.SetActive(true);
            Debug.Log("GuildCanvas 활성화");
        }

        // 2) 길드 정보 로드 (GuildTabPanelController 코루틴 호출)
        if (GuildTabPanelController.Instance != null)
        {
            // 여기서 RefreshGuildData() 안에 있는 코루틴을 직접 호출하거나
            // 래퍼 코루틴이 있다면, 그걸 yield return 해주면 됩니다.
            yield return StartCoroutine(GuildTabPanelController.Instance.GetGuildInfoAndMission());
            Debug.Log("길드 정보 및 미션 로드 완료");
        }
        else
        {
            Debug.LogWarning("GuildTabPanelController 인스턴스가 없습니다.");
        }

        // 3) 팝업 닫기
        if (GuildCanvas != null)
        {
            GuildCanvas.SetActive(false);
            Debug.Log("GuildCanvas 비활성화");
        }
    }
}
