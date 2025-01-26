using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OpenFarmScene : MonoBehaviour
{
    public void OnClickOpenFarm() // 농장 버튼 클릭
    {
        UnityEngine.Debug.Log("Farm Button Clicked");
        if (ChatPopupManager.Instance.isFullScreenActive)
        {
            ChatPopupManager.Instance.CloseChat();
        }
        SceneManager.LoadScene("Farm_Screen"); // Farm 씬으로 이동
    }
}