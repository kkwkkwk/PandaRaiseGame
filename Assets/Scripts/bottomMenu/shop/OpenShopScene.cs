using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // SceneManager 사용을 위해 추가

public class OpenShopScene : MonoBehaviour
{
    public void OnClickOpenShop() // 상점 버튼 클릭
    {
        UnityEngine.Debug.Log("Shop Button Clicked");
        if (ChatPopupManager.Instance.isFullScreenActive)
        {
            ChatPopupManager.Instance.CloseChat();
        }
        SceneManager.LoadScene("Shop_Screen"); // Farm 씬으로 이동
    }

}
