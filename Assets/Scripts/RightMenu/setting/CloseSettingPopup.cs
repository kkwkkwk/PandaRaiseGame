using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CloseSettingPopup : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)  // 팝업 외 공간 클릭 한 경우 --> 팝업 비활성화
    {
        if (SettingPopupManager.Instance != null)
        {
            SettingPopupManager.Instance.CloseSettingPopup(); // 팝업 비활성화
        }
        else
        {
            UnityEngine.Debug.Log("popupController가 설정되지 않았습니다.");
        }
    }
}
