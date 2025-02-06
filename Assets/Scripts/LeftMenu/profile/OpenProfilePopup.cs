using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenProfilePopup : MonoBehaviour
{
    public void OnClickOpenProfilePanel() // 데일리 로그인 버튼 클릭
    {
        ProfilePopupManager.Instance.OpenProfilePopup();
    }
}
