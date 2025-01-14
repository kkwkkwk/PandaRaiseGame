using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class popupPanel_Raycaster : MonoBehaviour, IPointerClickHandler
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
        public void OnPointerClick(PointerEventData eventData)
        {
            // 클릭 이벤트를 부모 패널에 전달되지 않도록 함
            eventData.Use();
        }
    
}
