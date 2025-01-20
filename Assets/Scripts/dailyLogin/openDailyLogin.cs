using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class openDailyLogin : MonoBehaviour
{
    public void OnClickOpenDailyLoginPanel() // 장비 버튼 클릭
    {
        
        popup_for_login.Instance.OpenDailyLoginPopup();
    }
}
