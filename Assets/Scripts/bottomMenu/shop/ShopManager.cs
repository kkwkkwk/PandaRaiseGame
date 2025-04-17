using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Shop Popup Panel")]
    public GameObject shopPopupPanel;

    [Header("메인 탭 패널")]
    public GameObject specialPanel;   // 스페셜 탭
    public GameObject currencyPanel;  // 재화 탭
    public GameObject gachaPanel;     // 뽑기 탭
    public GameObject vipPanel;       // VIP 탭

    private bool isShopOpen = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 시작 시 상점은 비활성화
        if (shopPopupPanel != null)
            shopPopupPanel.SetActive(false);
    }

    /// <summary>
    /// 상점을 열고, 기본 탭(스페셜)을 보여줍니다.
    /// </summary>
    public void OpenShop()
    {
        if (shopPopupPanel == null)
        {
            Debug.LogWarning("[ShopManager] shopPopupPanel이 할당되지 않았습니다.");
            return;
        }

        shopPopupPanel.SetActive(true);
        isShopOpen = true;

        // 기본 탭을 스페셜로 설정
        ShowSpecialPanel();
        Debug.Log("[ShopManager] 상점 열림 - 기본 스페셜 탭 활성화");
    }

    /// <summary>
    /// 상점을 닫습니다.
    /// </summary>
    public void CloseShop()
    {
        if (shopPopupPanel == null) return;

        shopPopupPanel.SetActive(false);
        isShopOpen = false;
        Debug.Log("[ShopManager] 상점 닫힘");
    }

    /// <summary>
    /// 상점이 열려있으면 닫고, 닫혀있으면 엽니다.
    /// </summary>
    public void ToggleShop()
    {
        if (isShopOpen) CloseShop();
        else OpenShop();
    }

    // ─────────────────────────────────────────────────
    // 메인 탭 전환 (UI 버튼에 연결)
    // ─────────────────────────────────────────────────

    private void HideAllMainPanels()
    {
        if (specialPanel) specialPanel.SetActive(false);
        if (currencyPanel) currencyPanel.SetActive(false);
        if (gachaPanel) gachaPanel.SetActive(false);
        if (vipPanel) vipPanel.SetActive(false);
    }

    /// <summary>스페셜 탭 표시</summary>
    public void ShowSpecialPanel()
    {
        HideAllMainPanels();
        specialPanel?.SetActive(true);
        Debug.Log("[ShopManager] 스페셜 탭 열림");
    }

    /// <summary>재화 탭 표시</summary>
    public void ShowCurrencyPanel()
    {
        HideAllMainPanels();
        currencyPanel?.SetActive(true);
        Debug.Log("[ShopManager] 재화 탭 열림");
    }

    /// <summary>뽑기 탭 표시</summary>
    public void ShowGachaPanel()
    {
        HideAllMainPanels();
        gachaPanel?.SetActive(true);
        Debug.Log("[ShopManager] 뽑기 탭 열림");
    }

    /// <summary>VIP 탭 표시</summary>
    public void ShowVIPPanel()
    {
        HideAllMainPanels();
        vipPanel?.SetActive(true);
        Debug.Log("[ShopManager] VIP 탭 열림");
    }
}
