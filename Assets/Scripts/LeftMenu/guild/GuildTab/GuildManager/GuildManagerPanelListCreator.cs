using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 가입 신청자 목록을 표시하기 위해
/// ItemPrefab을 동적으로 생성/삭제하는 스크립트
/// </summary>
public class GuildManagerPanelListCreator : MonoBehaviour
{
    [Header("Prefab & Content")]
    public GameObject guildManagerPrefab;  // 가입 신청자 표시용 프리팹
    public Transform contentParent;        // ScrollView Content 등

    /// <summary>
    /// 이전 목록을 싹 지우고 새롭게 생성
    /// </summary>
    public void SetGuildManagerList(List<GuildManagerData> dataList)
    {
        ClearList();

        if (dataList == null || dataList.Count == 0)
            return;

        foreach (var data in dataList)
        {
            GameObject go = Instantiate(guildManagerPrefab, contentParent);
            var itemCtrl = go.GetComponent<GuildManagerItemController>();
            if (itemCtrl != null)
            {
                itemCtrl.Setup(data);
            }
        }
    }

    /// <summary>
    /// Content에 있는 모든 자식 오브젝트 제거
    /// </summary>
    private void ClearList()
    {
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }
    }
}