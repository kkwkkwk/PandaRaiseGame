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
            // Ŭ�� �̺�Ʈ�� �θ� �гο� ���޵��� �ʵ��� ��
            eventData.Use();
        }
    
}
