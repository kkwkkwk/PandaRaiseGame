using UnityEngine;

public class OpenShopPopup : MonoBehaviour
{
    public void OnClickOpenShop() // 상점 버튼 클릭 이벤트에 연결
    {
        Debug.Log("Shop Button Clicked");

        // ChatPopupManager가 활성 상태라면 닫기 (필요 시)
        if (ChatPopupManager.Instance != null && ChatPopupManager.Instance.isFullScreenActive)
        {
            ChatPopupManager.Instance.CloseChat();
        }

        // ShopManager를 통해 상점 팝업 토글
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.ToggleShop();
        }
        else
        {
            Debug.LogWarning("ShopManager instance가 할당되어 있지 않습니다!");
        }
    }
}
