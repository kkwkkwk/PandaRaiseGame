using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
            string key, displayName, grade;
            OwnedItemData data = owned;

            if (data.WeaponItemData != null)
            {
                var d = data.WeaponItemData;
                grade = d.Rank;
                displayName = string.IsNullOrEmpty(d.ItemName) ? grade : d.ItemName;
                key = "Weapon:" + displayName;
                d.ItemName = displayName;
            }
            else if (data.ArmorItemData != null)
            {
                var d = data.ArmorItemData;
                grade = d.Rank;
                displayName = string.IsNullOrEmpty(d.ItemName) ? grade : d.ItemName;
                key = "Armor:" + displayName;
                d.ItemName = displayName;
            }
            else if (data.SkillItemData != null)
            {
                var d = data.SkillItemData;
                grade = d.Rank;
                displayName = string.IsNullOrEmpty(d.ItemName) ? grade : d.ItemName;
                key = "Skill:" + displayName;
                d.ItemName = displayName;
            }
            else
            {
                continue;
            }

            if (map.ContainsKey(key))
                map[key] = (map[key].item, map[key].count + 1);
            else
                map[key] = (data, 1);
        }

        // 2) 원하는 등급 순서 정의
        var gradeOrder = new List<string>
    {
        "COMMON",
        "UNCOMMON",
        "RARE",
        "UNIQUE",
        "EPIC",
        "LEGENDARY"
    };

        // 3) 정렬: 먼저 등급 순서대로, 같으면 이름 순
        var sortedEntries = map.Values
            .OrderBy(e =>
            {
                // 각 OwnedItemData에서 rank 꺼내기
                string rank = "";
                if (e.item.WeaponItemData != null) rank = e.item.WeaponItemData.Rank;
                else if (e.item.ArmorItemData != null) rank = e.item.ArmorItemData.Rank;
                else if (e.item.SkillItemData != null) rank = e.item.SkillItemData.Rank;
                rank = rank?.Trim().ToUpperInvariant() ?? "";
                int idx = gradeOrder.IndexOf(rank);
                return idx >= 0 ? idx : int.MaxValue;
            })
            // (선택) 동일 등급 내에서 이름순으로 추가 정렬
            .ThenBy(e =>
            {
                if (e.item.WeaponItemData != null) return e.item.WeaponItemData.ItemName;
                if (e.item.ArmorItemData != null) return e.item.ArmorItemData.ItemName;
                return e.item.SkillItemData.ItemName;
            })
            .ToList();

        // 4) 정렬된 순서대로 UI 생성
        foreach (var entry in sortedEntries)
        {
            var owned = entry.item;
            int count = entry.count;
            var go = Instantiate(itemPrefab, contentParent);
            var ctrl = go.GetComponent<GachaResultItemController>();
            if (ctrl == null) continue;

            if (owned.WeaponItemData != null)
                ctrl.Setup(owned.WeaponItemData, count, owned.WeaponItemData.Rank);
            else if (owned.ArmorItemData != null)
                ctrl.Setup(owned.ArmorItemData, count, owned.ArmorItemData.Rank);
            else if (owned.SkillItemData != null)
                ctrl.Setup(owned.SkillItemData, count, owned.SkillItemData.Rank);
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
