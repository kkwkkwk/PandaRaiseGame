using System.Collections.Generic;
using UnityEngine;

public class CurrencyTabController : MonoBehaviour
{
    [Header("재화 탭 패널들")]
    public GameObject freePanel;      // 무료 탭
    public GameObject japhwaPanel;     // 잡화 탭
    public GameObject diaPanel;       // 다이아 탭
    public GameObject mileagePanel;   // 마일리지 탭

    [Header("하위 메뉴 매니저 (Open…Panel 호출)")]
    public ShopFreeManager freeManager;
    public ShopJaphwaManager japhwaManager;
    public ShopDiamondManager diamondManager;
    public ShopMileageManager mileageManager;

    private void Start()
    {
        // 씬 시작 시 기본으로 무료 탭을 연다
        ShowFreePanel();
    }

    private void OnEnable()
    {
        // 이 스크립트가 활성화될 때(재화 탭을 다시 열 때)도 무료 탭을 기본으로 연다
        ShowFreePanel();
    }

    private void HideAllTabs()
    {
        freePanel?.SetActive(false);
        japhwaPanel?.SetActive(false);
        diaPanel?.SetActive(false);
        mileagePanel?.SetActive(false);
    }

    public void ShowFreePanel()
    {
        HideAllTabs();
        freePanel?.SetActive(true);
        Debug.Log("[CurrencyTabController] 무료 탭 열림");
        freeManager?.OpenFreePanel();
    }

    public void ShowJaphwaPanel()
    {
        HideAllTabs();
        japhwaPanel?.SetActive(true);
        Debug.Log("[CurrencyTabController] 잡화 탭 열림");
        japhwaManager?.OpenJaphwaPanel();
    }

    public void ShowDiaPanel()
    {
        HideAllTabs();
        diaPanel?.SetActive(true);
        Debug.Log("[CurrencyTabController] 다이아 탭 열림");
        diamondManager?.OpenDiamondPanel();
    }

    public void ShowMileagePanel()
    {
        HideAllTabs();
        mileagePanel?.SetActive(true);
        Debug.Log("[CurrencyTabController] 마일리지 탭 열림");
        mileageManager?.OpenMileagePanel();
    }
}