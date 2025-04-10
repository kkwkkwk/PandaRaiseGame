using UnityEngine;

public class CurrencyTabController : MonoBehaviour
{
    [Header("탭 패널들")]
    public GameObject freePanel;    // 무료 탭 패널
    public GameObject japhaPanel;   // 잡화 탭 패널
    public GameObject diaPanel;     // 다이아 탭 패널
    public GameObject mileagePanel; // 마일리지 탭 패널

    private void Start()
    {
        // 씬 시작(또는 해당 스크립트 활성화) 시 기본 탭인 무료 패널 열기
        ShowFreePanel();
    }

    /// <summary>
    /// 모든 탭 패널을 비활성화
    /// </summary>
    private void HideAllTabs()
    {
        if (freePanel != null) freePanel.SetActive(false);
        if (japhaPanel != null) japhaPanel.SetActive(false);
        if (diaPanel != null) diaPanel.SetActive(false);
        if (mileagePanel != null) mileagePanel.SetActive(false);
    }

    /// <summary>
    /// 무료 패널 열기 (기본 탭)
    /// </summary>
    public void ShowFreePanel()
    {
        HideAllTabs();
        if (freePanel != null)
        {
            freePanel.SetActive(true);
            Debug.Log("[CurrencyTabController] 무료 탭 열림 (기본 탭)");
        }
    }

    /// <summary>
    /// 잡화 탭 열기
    /// </summary>
    public void ShowJaphaPanel()
    {
        HideAllTabs();
        if (japhaPanel != null)
        {
            japhaPanel.SetActive(true);
            Debug.Log("[CurrencyTabController] 잡화 탭 열림");
        }
    }

    /// <summary>
    /// 다이아 탭 열기
    /// </summary>
    public void ShowDiaPanel()
    {
        HideAllTabs();
        if (diaPanel != null)
        {
            diaPanel.SetActive(true);
            Debug.Log("[CurrencyTabController] 다이아 탭 열림");
        }
    }

    /// <summary>
    /// 마일리지 탭 열기
    /// </summary>
    public void ShowMileagePanel()
    {
        HideAllTabs();
        if (mileagePanel != null)
        {
            mileagePanel.SetActive(true);
            Debug.Log("[CurrencyTabController] 마일리지 탭 열림");
        }
    }
}
