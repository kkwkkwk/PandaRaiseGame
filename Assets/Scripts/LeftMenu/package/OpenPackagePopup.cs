using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenPackagePopup : MonoBehaviour
{
    public void OnClickOpenPackagePanel() // 패키지 버튼 클릭
    {
        PackagePopupManager.Instance.OpenPackagePopup();
    }
}
