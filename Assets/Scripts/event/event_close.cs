using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class event_close : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)  // 팝업 외 공간 클릭 한 경우 --> 팝업 비활성화
    {
        if (popup_for_event.Instance != null)
        {
            popup_for_event.Instance.CloseEventPopup(); // 팝업 비활성화
        }
        else
        {
            Debug.LogWarning("popupController가 설정되지 않았습니다.");
        }
    }
}
