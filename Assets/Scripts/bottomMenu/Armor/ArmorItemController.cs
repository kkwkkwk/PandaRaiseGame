using System.Collections.Generic;
using UnityEngine;

public class ArmorItemController : MonoBehaviour
{
    [SerializeField] private Transform[] equipmentImageParents; // Size = 5

    /// <summary>
    /// 이 Row_Panel에서 표시해야 할 방어구 데이터 목록(최대 5개)
    /// </summary>
    /// <param name="rowData">한 줄에 표시할 방어구들 (최대 5개)</param>
    /// <param name="equipmentImagePrefab">단일 이미지 Prefab</param>
    public void SetData(List<ArmorData> rowData, GameObject equipmentImagePrefab)
    {
        // 1) 모든 슬롯에 기존 자식(이미지)가 있다면 제거
        for (int i = 0; i < equipmentImageParents.Length; i++)
        {
            var slot = equipmentImageParents[i];
            for (int c = slot.childCount - 1; c >= 0; c--)
            {
                Destroy(slot.GetChild(c).gameObject);
            }
        }

        // 2) rowData의 개수만큼 (최대 5) 슬롯에 이미지 프리팹을 Instantiate
        for (int i = 0; i < rowData.Count && i < equipmentImageParents.Length; i++)
        {
            var armorData = rowData[i];

            // equipmentImagePrefab을 현재 슬롯(equipmentImageParents[i])에 자식으로 생성
            GameObject clone = Instantiate(equipmentImagePrefab, equipmentImageParents[i]);

            // 1) clone이 UI용 RectTransform인지 확인
            RectTransform childRect = clone.GetComponent<RectTransform>();
            if (childRect != null)
            {
                // 2) 부모도 RectTransform이어야 함
                //    만약 parent가 일반 Transform이면, UI 레이아웃이 정상 동작하지 않습니다.
                //    (대부분은 Canvas 안의 Panel/빈오브젝트 등일 것이므로 RectTransform일 겁니다)

                // 위치/스케일 초기화
                clone.transform.localScale = Vector3.one;

                // Anchor를 (0,0)~(1,1)로 설정 → 부모와 똑같은 크기로 확장
                childRect.anchorMin = Vector2.zero;
                childRect.anchorMax = Vector2.one;
                childRect.offsetMin = Vector2.zero;
                childRect.offsetMax = Vector2.zero;
                childRect.anchoredPosition = Vector2.zero;
            }

            // 이미지 Prefab 내부의 스크립트(EquipmentImageController) 호출 → 텍스트 세팅
            var imgController = clone.GetComponent<EquipmentImageController>();
            if (imgController != null)
            {
                imgController.ArmorSetData(armorData);
            }
        }
    }
}
