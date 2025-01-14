using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class popup_close : MonoBehaviour, IPointerClickHandler
{

    public popup_for_equipment popupController; // �˾� ��Ʈ�ѷ� ����
    public void OnPointerClick(PointerEventData eventData)  // �˾� �� ���� Ŭ�� �� ��� --> �˾� ��Ȱ��ȭ
    { 
        if (popupController != null)
        {
            popupController.ClosePanel(); // �˾� ��Ȱ��ȭ
        }
        else
        {
            Debug.LogWarning("popupController�� �������� �ʾҽ��ϴ�.");
        }
    }
}
