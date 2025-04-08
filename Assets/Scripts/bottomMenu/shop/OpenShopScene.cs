using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // SceneManager 사용을 위해 추가

public class OpenShopScene : MonoBehaviour
{
    // ShopUIController 컴포넌트가 붙은 오브젝트를 참조 (Inspector에 드래그)
    public ShopUIController shopUIController;

    public void OnClickOpenShop() // 상점 버튼 클릭 이벤트에 연결
    {
        Debug.Log("Shop Button Clicked");
        // 만약 ChatPopupManager가 전체화면 팝업을 띄우고 있다면 닫음
        if (ChatPopupManager.Instance != null && ChatPopupManager.Instance.isFullScreenActive)
        {
            ChatPopupManager.Instance.CloseChat();
        }
        // 씬 전환이 아니라 상점 팝업 UI를 활성화
        if (shopUIController != null)
        {
            shopUIController.OpenShop();
        }
        else
        {
            Debug.LogWarning("ShopUIController reference가 할당되어 있지 않습니다!");
        }
    }
}
