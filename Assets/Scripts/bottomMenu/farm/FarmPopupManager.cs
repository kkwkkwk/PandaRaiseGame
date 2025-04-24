using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FarmPopupManager : MonoBehaviour
{

    public static FarmPopupManager Instance { get; private set; }

    [Header("Popup Canvas")]
    public GameObject FarmPopupCanvas;              // 농장 팝업 canvas

    [Header("Control Buttons")]
    public Button closeFarmBtn;                     // 농장 팝업 닫기 버튼
    public Button farmStatBtn;                      // 농장 능력치 버튼
    public Button farmInventoryBtn;                 // 농장 인벤토리 버튼

    public void OpenFarmPanel() // 농장 팝업 열기
    {   // 팝업 활성화 
        if (FarmPopupCanvas != null)
        {
            FarmPopupCanvas.SetActive(true);
        }
        ChatPopupManager.Instance.ChatCanvas.SetActive(false);
    }
    public void CloseFarmPanel() // 농장 팝업 닫기
    {  // 팝업 비활성화
        if (FarmPopupCanvas != null)
            FarmPopupCanvas.SetActive(false);
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
        if (closeFarmBtn != null)
            closeFarmBtn.onClick.AddListener(CloseFarmPanel);
        // 탭 버튼 클릭 리스너 추가
        if (farmStatBtn != null)
            farmStatBtn.onClick.AddListener(OnClickFarmStat);
        if (farmInventoryBtn != null)
            farmInventoryBtn.onClick.AddListener(OnClickFarmInventory);

    }

    public void OnClickFarmStat()   // 농장 능력치 버튼 클릭 시
    {

    }
    public void OnClickFarmInventory()  // 농장 인벤토리 버튼 클릭 시
    {

    }
}
