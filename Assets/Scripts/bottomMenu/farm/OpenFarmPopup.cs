using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OpenFarmPopup : MonoBehaviour
{
    public void OnClickOpenFarm() // 농장 버튼 클릭
    {
            Debug.Log("Farm Button Clicked");
            FarmPopupManager.Instance.OpenFarmPanel();
    }
}