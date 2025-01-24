using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenDailyLoginPopup : MonoBehaviour
{
    public void OnClickOpenDailyLoginPanel() // 데일리 로그인 버튼 클릭
    {
        PopupForLogin.Instance.OpenDailyLoginPopup();
    }
}
