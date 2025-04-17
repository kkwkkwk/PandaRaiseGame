using System.Collections.Generic;
using UnityEngine;

public class CurrencyTabController : MonoBehaviour
{
    [Header("재화 탭 패널들")]
    public GameObject freePanel;      // 무료 탭
    public GameObject japhaPanel;     // 잡화 탭
    public GameObject diaPanel;       // 다이아 탭
    public GameObject mileagePanel;   // 마일리지 탭

    [Header("하위 메뉴 매니저 (Open…Panel 호출)")]
    public FreeManager freeManager;
    public JaphaManager japhaManager;
    public DiamondManager diamondManager;
    public MileageManager mileageManager;

    private void Start()
    {
        ShowFreePanel();
    }

    private void HideAllTabs()
    {
        freePanel.SetActive(false);
        japhaPanel.SetActive(false);
        diaPanel.SetActive(false);
        mileagePanel.SetActive(false);
    }

    public void ShowFreePanel()
    {
        HideAllTabs();
        freePanel.SetActive(true);
        freeManager.OpenFreePanel();
    }

    public void ShowJaphaPanel()
    {
        HideAllTabs();
        japhaPanel.SetActive(true);
        japhaManager.OpenJaphaPanel();
    }

    public void ShowDiaPanel()
    {
        HideAllTabs();
        diaPanel.SetActive(true);
        diamondManager.OpenDiamondPanel();
    }

    public void ShowMileagePanel()
    {
        HideAllTabs();
        mileagePanel.SetActive(true);
        mileageManager.OpenMileagePanel();
    }
}
