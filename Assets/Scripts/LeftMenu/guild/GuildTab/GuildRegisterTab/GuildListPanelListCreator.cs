using System.Collections.Generic;
using UnityEngine;

public class GuildListPanelListCreator : MonoBehaviour
{
    [Header("Prefab & Content")]
    public GameObject guildListItemPrefab;   // Project 창의 GuildListItem_Prefab 에셋을 할당
    public Transform contentParent;            // Scene 내 ScrollView의 Content GameObject를 할당

    /// <summary>
    /// 기존에 생성된 목록을 삭제하고, guildDataList의 길드 데이터를 기반으로 Prefab을 생성해 배치합니다.
    /// </summary>
    public void SetGuildList(List<GuildData> guildDataList)
    {
        ClearList();

        if (guildDataList == null)
        {
            Debug.LogWarning("[GuildListPanelListCreator] guildDataList가 null입니다.");
            return;
        }

        foreach (var guild in guildDataList)
        {
            // 1. Prefab 인스턴스 생성
            GameObject itemObj = Instantiate(guildListItemPrefab, contentParent);

            // 2. Prefab에 붙은 컨트롤러 가져오기
            var controller = itemObj.GetComponent<GuildListItemController>();
            if (controller != null)
            {
                // 3. 데이터 적용
                controller.SetData(guild);
            }
            else
            {
                Debug.LogError("[GuildListPanelListCreator] GuildListItemController가 프리팹에 없습니다!");
            }
        }
    }

    /// <summary>
    /// Content 내에 존재하는 기존 자식 오브젝트들을 모두 삭제합니다.
    /// </summary>
    private void ClearList()
    {
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }
    }
}
