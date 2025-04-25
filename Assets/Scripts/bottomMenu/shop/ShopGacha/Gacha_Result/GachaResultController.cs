using UnityEngine;
using System.Collections.Generic;

public class GachaResultController : MonoBehaviour
{
    [Header("결과 패널 (전체)")]
    [Tooltip("GachaResult 패널 GameObject (초기엔 비활성화)")]
    public GameObject resultPanel;

    [Header("결과 아이템 배치용 Content Parent")]
    [Tooltip("GachaResultItem 프리팹이 Instantiate될 부모 트랜스폼")]
    public Transform contentParent;

    [Header("결과 아이템 Prefab")]
    [Tooltip("GachaResultItem 스크립트가 붙은 프리팹")]
    public GameObject itemPrefab;

    private void Awake()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    /// <summary>
    /// 서버에서 받아온 가챠 결과 리스트를 표시하고 패널을 활성화
    /// 동일한 아이템은 묶어서 카운트를 표시
    /// </summary>
    public void ShowResults(List<OwnedItemData> results)
    {
        if (resultPanel == null || contentParent == null || itemPrefab == null)
            return;

        ClearResults();

        // 1) 같은 아이템끼리 묶어서 개수 세기
        var map = new Dictionary<string, (OwnedItemData item, int count)>();
        foreach (var owned in results)
        {
            string key;
            if (owned.WeaponItemData != null)
                key = "Weapon:" + owned.WeaponItemData.ItemName;
            else if (owned.ArmorItemData != null)
                key = "Armor:" + owned.ArmorItemData.ItemName;
            else if (owned.SkillItemData != null)
                key = "Skill:" + owned.SkillItemData.ItemName;
            else
                continue;

            if (map.ContainsKey(key))
                map[key] = (map[key].item, map[key].count + 1);
            else
                map[key] = (owned, 1);
        }

        // 2) 묶인 결과들을 프리팹으로 한 번씩만 생성
        foreach (var entry in map.Values)
        {
            var owned = entry.item;
            int count = entry.count;
            var go = Instantiate(itemPrefab, contentParent);
            var ctrl = go.GetComponent<GachaResultItemController>();
            if (ctrl == null) continue;

            if (owned.WeaponItemData != null)
                ctrl.Setup(owned.WeaponItemData, count);
            else if (owned.ArmorItemData != null)
                ctrl.Setup(owned.ArmorItemData, count);
            else if (owned.SkillItemData != null)
                ctrl.Setup(owned.SkillItemData, count);
        }

        resultPanel.SetActive(true);
    }

    /// <summary>
    /// 결과 패널을 닫고 내부 콘텐츠를 정리합니다.
    /// </summary>
    public void HideResults()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
        ClearResults();
    }

    private void ClearResults()
    {
        if (contentParent == null) return;
        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);
    }
}
