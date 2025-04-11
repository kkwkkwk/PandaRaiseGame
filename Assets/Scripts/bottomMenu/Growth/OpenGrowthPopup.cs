using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenGrowthPopup : MonoBehaviour
{
    public void OnClickOpenPanel() // 스킬 버튼 클릭
    {
        Debug.Log("Growth Button Clicked");
        GrowthPopupManager.Instance.OpenGrowthPanel();
    }
}
