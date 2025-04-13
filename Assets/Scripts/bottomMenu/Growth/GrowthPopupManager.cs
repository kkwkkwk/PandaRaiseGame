using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GrowthPopupManager : MonoBehaviour
{
    public static GrowthPopupManager Instance { get; private set; }

    [Header("Popup Canvas")]
    public GameObject GrowthPopupCanvas;            // 팝업 canvas

    [Header("Control Buttons")]
    public Button closeBtn;                         // 닫기 버튼
    public Button LevelUpBtn;                       // 레벨업 버튼
    public Button LevelStatBtn;                     // 레벨업 버튼
    public Button goldGrowthBtn;                    // 성장(골드) 버튼

    [Header("Panel References")]
    public GameObject levelUpPanel;                 // 레벨업 패널
    public GameObject levelStatPanel;               // 레벨 스탯 패널
    public GameObject goldGrowthPanel;              // 성장 패널

    [Header("Gold Info Panel (CloseBtn 옆)")]
    public GameObject goldInfoPanel;

    public void OpenGrowthPanel()
    {   // 팝업 활성화 
        if (GrowthPopupCanvas != null)
        {
            GrowthPopupCanvas.SetActive(true);
            // 팝업을 열 때 기본 탭으로 레벨업 패널로 활성화
            OnClickTab(0);
        }
        ChatPopupManager.Instance.ChatCanvas.SetActive(false);

    }
    public void CloseGrowthPanel()
    {  // 팝업 비활성화
        if (GrowthPopupCanvas != null)
            GrowthPopupCanvas.SetActive(false);
        // 현재 골드 표시 패널도 끔
        if (goldInfoPanel != null)
            goldInfoPanel.SetActive(false);
        ChatPopupManager.Instance.ChatCanvas.SetActive(true);
    }

    private void Awake() // Unity의 Awake() 메서드에서 싱글톤 설정
    {
        // 만약 Instance가 비어 있으면, 이 스크립트를 싱글톤 인스턴스로 설정
        if (Instance == null)
            Instance = this; // 현재 객체를 싱글톤으로 지정
        else
            // 이미 싱글톤 인스턴스가 존재하면, 중복된 객체를 제거
            Destroy(gameObject);
    }

    void Start()
    {
        // 닫기 버튼 연결
        if (closeBtn != null)
            closeBtn.onClick.AddListener(CloseGrowthPanel);
        // 탭 버튼 클릭 리스너 추가
        if (LevelUpBtn != null)
            LevelUpBtn.onClick.AddListener(() => OnClickTab(0));
        if (LevelStatBtn != null)
            LevelStatBtn.onClick.AddListener(() => OnClickTab(1));
        if (goldGrowthBtn != null)
            goldGrowthBtn.onClick.AddListener(() => OnClickTab(2));

        // 현재 골드 표시 패널은 처음에 숨긴 상태
        if (goldInfoPanel != null)
            goldInfoPanel.SetActive(false);

        if (GrowthPopupCanvas != null)
        {
            GrowthPopupCanvas.SetActive(false);
        }
    }
    /// <summary>
    /// 탭 버튼 클릭 시 호출되는 메서드.
    /// 0: 레벨업 패널, 1: 레벨 스탯 패널, 2: 성장(골드) 패널
    /// </summary>
    /// <param name="tabIndex">선택된 탭 인덱스</param>
    private void OnClickTab(int tabIndex)
    {
        // 모든 패널을 먼저 비활성화
        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);
        if (levelStatPanel != null)
            levelStatPanel.SetActive(false);
        if (goldGrowthPanel != null)
            goldGrowthPanel.SetActive(false);

        // 골드 정보 패널도 off
        if (goldInfoPanel != null)
            goldInfoPanel.SetActive(false);

        // 선택된 탭에 해당하는 패널 활성화
        switch (tabIndex)
        {
            case 0:
                levelUpPanel.SetActive(true);
                break;
            case 1:
                    levelStatPanel.SetActive(true);
                break;
            case 2:
                    goldGrowthPanel.SetActive(true);
                // 골드 성장 탭에서만 goldInfoPanel 활성화
                if (goldInfoPanel != null)
                    goldInfoPanel.SetActive(true);
                break;
            default:
                Debug.LogWarning("[GrowthPopupManager] 알 수 없는 탭 인덱스: " + tabIndex);
                break;
        }

        Debug.Log($"[GrowthPopupManager] 탭 {tabIndex} 클릭됨. 패널 전환 완료.");
    }
}
