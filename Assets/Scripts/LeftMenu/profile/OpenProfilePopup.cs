using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenProfilePopup : MonoBehaviour
{
    public void OnClickOpenProfilePanel() // 프로필 버튼 클릭
    {
        ProfilePopupManager.Instance.OpenProfilePopup();
    }
}
