using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Shop Popup Panel")]
    [Tooltip("상점 팝업 전체 패널 오브젝트 (초기엔 비활성화 상태)")]
    public GameObject shopPopupPanel;

    [Header("Content Panels (ShopMain_Panel 내)")]
    [Tooltip("스페셜 탭 콘텐츠 패널 (기본 탭)")]
    public GameObject specialPanel;
    [Tooltip("재화 탭 콘텐츠 패널")]
    public GameObject currencyPanel;
    [Tooltip("뽑기 탭 콘텐츠 패널")]
    public GameObject gachaPanel;
    [Tooltip("VIP 탭 콘텐츠 패널")]
    public GameObject vipPanel;

    private bool isShopOpen = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Start(): 게임 실행 시 shopPopupPanel 비활성화
    // ─────────────────────────────────────────────────────────────────────
    private void Start()
    {
        if (shopPopupPanel != null)
        {
            shopPopupPanel.SetActive(false);
            Debug.Log("[ShopManager] Start(): 상점 팝업 패널을 비활성화했습니다.");
        }
    }

    /// <summary>
    /// 상점 팝업을 열거나 닫는 토글 함수 (상점 버튼에서 호출)
    /// </summary>
    public void ToggleShop()
    {
        if (isShopOpen)
        {
            CloseShop();
        }
        else
        {
            OpenShop();
        }
    }

    /// <summary>
    /// 상점 팝업을 열며 기본으로 스페셜 패널을 표시
    /// </summary>
    public void OpenShop()
    {
        if (shopPopupPanel != null)
        {
            shopPopupPanel.SetActive(true);
            isShopOpen = true;
            ShowSpecialPanel(); // 기본 탭: 스페셜
            Debug.Log("Shop Popup Opened");
        }
        else
        {
            Debug.LogWarning("ShopPopupPanel reference가 할당되어 있지 않습니다!");
        }
    }

    /// <summary>
    /// 상점 팝업을 닫음
    /// </summary>
    public void CloseShop()
    {
        if (shopPopupPanel != null)
        {
            shopPopupPanel.SetActive(false);
            isShopOpen = false;
            Debug.Log("Shop Popup Closed");
        }
    }

    // ─────────────────────────────
    // 탭 전환 기능
    // ─────────────────────────────

    /// <summary>
    /// 모든 콘텐츠 패널을 숨김
    /// </summary>
    private void HideAllContentPanels()
    {
        if (specialPanel != null) specialPanel.SetActive(false);
        if (currencyPanel != null) currencyPanel.SetActive(false);
        if (gachaPanel != null) gachaPanel.SetActive(false);
        if (vipPanel != null) vipPanel.SetActive(false);
    }

    /// <summary>
    /// 스페셜 탭 패널을 활성화 (기본 탭)
    /// </summary>
    public void ShowSpecialPanel()
    {
        HideAllContentPanels();
        if (specialPanel != null)
        {
            specialPanel.SetActive(true);
            Debug.Log("Special Panel Opened (기본)");
        }
    }

    /// <summary>
    /// 재화 탭 패널을 활성화
    /// </summary>
    public void ShowCurrencyPanel()
    {
        HideAllContentPanels();
        if (currencyPanel != null)
        {
            currencyPanel.SetActive(true);
            Debug.Log("Currency Panel Opened");
        }
    }

    /// <summary>
    /// 뽑기 탭 패널을 활성화
    /// </summary>
    public void ShowGachaPanel()
    {
        HideAllContentPanels();
        if (gachaPanel != null)
        {
            gachaPanel.SetActive(true);
            Debug.Log("Gacha Panel Opened");
        }
    }

    /// <summary>
    /// VIP 탭 패널을 활성화
    /// </summary>
    public void ShowVIPPanel()
    {
        HideAllContentPanels();
        if (vipPanel != null)
        {
            vipPanel.SetActive(true);
            Debug.Log("VIP Panel Opened");
        }
    }
}
