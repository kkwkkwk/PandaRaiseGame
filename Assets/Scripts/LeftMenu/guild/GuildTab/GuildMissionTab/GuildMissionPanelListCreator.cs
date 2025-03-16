using System.Collections.Generic;
using UnityEngine;

public class GuildMissionPanelListCreator : MonoBehaviour
{
    [Header("Prefab & Content")]
    public GameObject guildMissionPrefab;   // GuildMissionItem_Prefab
    public Transform contentParent;         // ScrollView의 Content

    /// <summary>
    /// 기존 미션 목록을 Clear하고, guildMissionList에 있는 데이터를 Prefab으로 표시
    /// </summary>
    public void SetGuildMissionList(List<GuildMissionData> guildMissionList)
    {
        ClearList();

        if (guildMissionList == null)
        {
            Debug.LogWarning("[GuildMissionPanelListCreator] guildMissionList가 null입니다.");
            return;
        }

        foreach (var mission in guildMissionList)
        {
            // 1) 프리팹 생성
            GameObject itemObj = Instantiate(guildMissionPrefab, contentParent);

            // 2) 컨트롤러 스크립트 찾기
            var controller = itemObj.GetComponent<GuildMissionItemController>();
            if (controller != null)
            {
                // 3) 데이터 적용
                controller.SetData(mission);
            }
            else
            {
                Debug.LogError("[GuildMissionPanelListCreator] GuildMissionItemController가 없습니다!");
            }
        }
    }

    /// <summary>
    /// 기존 Prefab들을 모두 제거
    /// </summary>
    private void ClearList()
    {
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }
    }
}
