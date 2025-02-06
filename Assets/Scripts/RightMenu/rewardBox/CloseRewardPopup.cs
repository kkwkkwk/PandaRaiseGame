using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CloseRewardPopup : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)  // 팝업 외 공간 클릭 한 경우 --> 팝업 비활성화
    {
        if (RewardPopupManager.Instance != null)
        {
            RewardPopupManager.Instance.CloseRewardPopup(); // 팝업 비활성화
        }
        else
        {
            UnityEngine.Debug.Log("popupController가 설정되지 않았습니다.");
        }
    }
}
