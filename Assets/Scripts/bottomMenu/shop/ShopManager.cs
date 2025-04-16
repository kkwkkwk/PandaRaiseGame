using UnityEngine;

/// <summary>
/// 상점을 총괄하는 매니저:
/// 1) 상점 팝업 열기/닫기 (OpenShop/CloseShop) 시
///    SpecialManager, CurrencyManager, GachaManager, VipManager 등을 한 번에 로딩(링크 한 번에 호출).
/// 2) 메인 탭(스페셜·재화·뽑기·VIP) 전환 기능(ShowSpecialPanel 등)만 남김.
/// </summary>
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Shop Popup Panel")]
    public GameObject shopPopupPanel;

    [Header("메인 탭 패널 (상점 타입)")]
    public GameObject specialPanel;   // 스페셜 탭
    public GameObject currencyPanel;  // 재화 탭
    public GameObject gachaPanel;     // 뽑기 탭
    public GameObject vipPanel;       // VIP 탭

    [Header("상위 4메뉴 매니저 (링크 한 번에 로딩용)")]
    public SpecialManager specialManager;
    public CurrencyManager currencyManager;
    public GachaManager gachaManager;
    public VipManager vipManager;

    private bool isShopOpen = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // 상점은 기본적으로 비활성화 상태
        if (shopPopupPanel != null)
            shopPopupPanel.SetActive(false);
        isShopOpen = false;
    }

    /// <summary>
    /// 상점을 열고, 4개 상위 매니저를 한 번에 호출하여 로딩한다.
    /// </summary>
    public void OpenShop()
    {
        if (shopPopupPanel != null)
        {
            shopPopupPanel.SetActive(true);
            isShopOpen = true;

            // 스페셜·재화·뽑기·VIP 매니저를 전부 호출 (링크 한 번에 로딩)
            if (specialManager != null)
                specialManager.OpenSpecialPanel();
            if (currencyManager != null)
                currencyManager.OpenCurrencyPanel();
            if (gachaManager != null)
                gachaManager.OpenGachaPanel();
            if (vipManager != null)
                vipManager.OpenVipPanel();

            Debug.Log("[ShopManager] OpenShop - 상위 4메뉴 매니저 모두 호출 완료");
        }
        else
        {
            Debug.LogWarning("[ShopManager] shopPopupPanel가 할당되지 않음!");
        }
    }

    /// <summary>
    /// 상점을 닫고, 4개 상위 매니저도 필요하다면 모두 닫게 한다.
    /// </summary>
    public void CloseShop()
    {
        if (shopPopupPanel != null)
        {
            shopPopupPanel.SetActive(false);
            isShopOpen = false;

            // 필요하면 하위 매니저들도 닫음
            if (specialManager != null)
                specialManager.CloseSpecialPanel();
            if (currencyManager != null)
                currencyManager.CloseCurrencyPanel();
            if (gachaManager != null)
                gachaManager.CloseGachaPanel();
            if (vipManager != null)
                vipManager.CloseVipPanel();

            Debug.Log("[ShopManager] CloseShop - 상점 및 4메뉴 닫음");
        }
        else
        {
            Debug.LogWarning("[ShopManager] shopPopupPanel가 할당되지 않음!");
        }
    }

    /// <summary>
    /// 쇼핑창 열려있으면 닫고, 닫혀있으면 연다.
    /// </summary>
    public void ToggleShop()
    {
        if (isShopOpen) CloseShop();
        else OpenShop();
    }

    // ─────────────────────────────────────────────────
    // 메인 탭 전환 기능 (스페셜·재화·뽑기·VIP)
    // ─────────────────────────────────────────────────
    private void HideAllMainPanels()
    {
        if (specialPanel) specialPanel.SetActive(false);
        if (currencyPanel) currencyPanel.SetActive(false);
        if (gachaPanel) gachaPanel.SetActive(false);
        if (vipPanel) vipPanel.SetActive(false);
    }

    public void ShowSpecialPanel()
    {
        HideAllMainPanels();
        if (specialPanel)
            specialPanel.SetActive(true);
        Debug.Log("[ShopManager] ShowSpecialPanel()");
    }

    public void ShowCurrencyPanel()
    {
        HideAllMainPanels();
        if (currencyPanel)
            currencyPanel.SetActive(true);
        Debug.Log("[ShopManager] ShowCurrencyPanel()");
    }

    public void ShowGachaPanel()
    {
        HideAllMainPanels();
        if (gachaPanel)
            gachaPanel.SetActive(true);
        Debug.Log("[ShopManager] ShowGachaPanel()");
    }

    public void ShowVIPPanel()
    {
        HideAllMainPanels();
        if (vipPanel)
            vipPanel.SetActive(true);
        Debug.Log("[ShopManager] ShowVIPPanel()");
    }
}
