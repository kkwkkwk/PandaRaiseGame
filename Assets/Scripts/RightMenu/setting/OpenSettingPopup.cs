using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenSettingPopup : MonoBehaviour
{
    public void OnClickOpenSettingPanel() // 설정 버튼 클릭
    {
        SettingPopupManager.Instance.OpenSettingPopup();
    }
}
