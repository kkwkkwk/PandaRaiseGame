using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenFixNamePopup : MonoBehaviour
{
    public void OnClickOpenFixNamePanel() // 이름 수정 버튼 클릭
    {
        FixNamePopupManager.Instance.OpenFixNamePopup();
    }
}
