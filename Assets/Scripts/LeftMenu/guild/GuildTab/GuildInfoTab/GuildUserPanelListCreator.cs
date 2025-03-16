using System.Collections.Generic;
using UnityEngine;

public class GuildUserPanelListCreator : MonoBehaviour
{
    [Header("Prefab & Content")]
    public GameObject guildUserPrefab;   // GuildUserPrefab
    public Transform contentParent;       // ScrollView의 Content (GuildUserPrefab들이 들어갈 곳)

    /// <summary>
    /// 기존 목록 Clear 후, guildMemberList에 있는 길드원들을 Prefab으로 표시
    /// </summary>
    public void SetGuildUserList(List<GuildMemberData> guildMemberList)
    {
        ClearList();

        if (guildMemberList == null)
        {
            Debug.LogWarning("[GuildUserPanelListCreator] guildMemberList가 null입니다.");
            return;
        }

        foreach (var member in guildMemberList)
        {
            // 1) 프리팹 생성
            GameObject itemObj = Instantiate(guildUserPrefab, contentParent);

            // 2) 컨트롤러 스크립트 찾기
            var controller = itemObj.GetComponent<GuildUserPanelItemController>();
            if (controller != null)
            {
                // 3) 데이터 적용
                controller.SetData(member);
            }
            else
            {
                Debug.LogError("[GuildUserPanelListCreator] GuildUserPanelItemController가 없습니다!");
            }
        }
    }

    /// <summary>
    /// ScrollView의 Content 내 기존 자식 오브젝트들(Prefab들) 전부 제거
    /// </summary>
    private void ClearList()
    {
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }
    }
}
