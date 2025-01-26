using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CloseSkillPopup : MonoBehaviour
{
    public void OnPointerClick(PointerEventData eventData)  // 팝업 외 공간 클릭 한 경우 --> 팝업 비활성화
    {
        if (SkillPopupManager.Instance != null)
        {
            SkillPopupManager.Instance.ClosePanel(); // 팝업 비활성화
        }
        else
        {
            UnityEngine.Debug.Log("popupController가 설정되지 않았습니다.");
        }
    }
}
