using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class chatPopupClose : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)  // 팝업 외 공간 클릭 한 경우 --> 팝업 비활성화
    {
        if (popup_for_chat.Instance != null)
        {
            popup_for_chat.Instance.CloseChat(); // 팝업 비활성화
        }
        else
        {
            Debug.LogWarning("popupController가 설정되지 않았습니다.");
        }
    }
}
