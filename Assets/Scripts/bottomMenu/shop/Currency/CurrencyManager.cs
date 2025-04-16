using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("재화(Currency) 탭 전체 패널")]
    public GameObject currencyPopupPanel;

    [Header("하위 메뉴 매니저(무료·잡화·다이아·마일리지)")]
    public FreeManager freeManager;
    public JaphaManager japhaManager;
    public DiamondManager diamondManager;
    public MileageManager mileageManager;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void OpenCurrencyPanel()
    {
        if (currencyPopupPanel != null)
            currencyPopupPanel.SetActive(true);

        if (freeManager != null) freeManager.OpenFreePanel();
        if (japhaManager != null) japhaManager.OpenJaphaPanel();
        if (diamondManager != null) diamondManager.OpenDiamondPanel();
        if (mileageManager != null) mileageManager.OpenMileagePanel();

        Debug.Log("[CurrencyManager] OpenCurrencyPanel - 하위4메뉴 모두 Open");
    }

    public void CloseCurrencyPanel()
    {
        if (currencyPopupPanel != null)
            currencyPopupPanel.SetActive(false);

        if (freeManager != null) freeManager.CloseFreePanel();
        if (japhaManager != null) japhaManager.CloseJaphaPanel();
        if (diamondManager != null) diamondManager.CloseDiamondPanel();
        if (mileageManager != null) mileageManager.CloseMileagePanel();

        Debug.Log("[CurrencyManager] CloseCurrencyPanel - 하위4메뉴 모두 Close");
    }
}
