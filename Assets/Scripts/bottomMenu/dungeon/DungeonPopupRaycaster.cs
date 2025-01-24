using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DungeonPopupRaycaster : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        // 클릭 이벤트를 부모 패널에 전달되지 않도록 함
        eventData.Use();
    }

}
