using UnityEngine;

public class ShopUIController : MonoBehaviour
{
    [Tooltip("상점 팝업 패널 오브젝트(초기엔 비활성화 상태로 설정)")]
    public GameObject shopPanel;

    public void OpenShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            Debug.Log("Shop Popup Opened");
        }
    }

    public void CloseShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
            Debug.Log("Shop Popup Closed");
        }
    }
}
