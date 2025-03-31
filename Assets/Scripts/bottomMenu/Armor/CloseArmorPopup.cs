using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CloseArmorPopup : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)  // 팝업 외 공간 클릭 한 경우 --> 팝업 비활성화
    {
        if (ArmorPopupManager.Instance != null)
        {
            ArmorPopupManager.Instance.CloseArmorPanel(); // 팝업 비활성화
        }
        else
        {
            UnityEngine.Debug.Log("Armor popupController가 설정되지 않았습니다.");
        }
    }
}
