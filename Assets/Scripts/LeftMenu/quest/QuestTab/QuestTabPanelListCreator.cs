using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

public class QuestTabPanelListCreator : MonoBehaviour
{
    [Header("Prefab & Content")]
    [Tooltip("퀘스트 항목 프리팹")]
    public GameObject questItemPrefab;

    [Tooltip("ScrollView의 Content (자식 오브젝트가 실제 목록)")]
    public Transform contentParent;

    [Header("Tab Buttons")]
    [Tooltip("일일 퀘스트 탭 버튼")]
    public Button dailyTabButton;
    [Tooltip("주간 퀘스트 탭 버튼")]
    public Button weeklyTabButton;
    [Tooltip("반복 퀘스트 탭 버튼")]
    public Button repeatableTabButton;
    [Tooltip("업적 탭 버튼")]
    public Button achievementTabButton;

    private int currentTabIndex = 0;
    private QuestDataCollection questDataCollection; // 파싱된 퀘스트 데이터

    void Start()
    {
        if (dailyTabButton != null)
            dailyTabButton.onClick.AddListener(() => OnClickTab(0));
        if (weeklyTabButton != null)
            weeklyTabButton.onClick.AddListener(() => OnClickTab(1));
        if (repeatableTabButton != null)
            repeatableTabButton.onClick.AddListener(() => OnClickTab(2));
        if (achievementTabButton != null)
            achievementTabButton.onClick.AddListener(() => OnClickTab(3));

        // 초기에는 데이터가 없으면 OnClickTab(0)가 빈 UI를 구성할 수 있으므로, 데이터가 주입된 후 SetQuestDataFromString에서 갱신합니다.
        OnClickTab(0);
    }

    /// <summary>
    /// 서버에서 문자열로 전달받은 QuestData JSON을 파싱하고 UI를 갱신합니다.
    /// </summary>
    public void SetQuestDataFromString(string questDataJson)
    {
        try
        {
            questDataCollection = JsonConvert.DeserializeObject<QuestDataCollection>(questDataJson);
            Debug.Log("[QuestTabPanelListCreator] Quest 데이터가 설정되었습니다. (문자열에서 파싱)");
            OnClickTab(currentTabIndex);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[QuestTabPanelListCreator] JSON 파싱 오류: " + ex.Message);
        }
    }

    /// <summary>
    /// 탭을 클릭하면 해당 탭에 맞는 퀘스트 목록으로 UI를 갱신합니다.
    /// 0: Daily, 1: Weekly, 2: Repeatable, 3: Achievements
    /// </summary>
    public void OnClickTab(int tabIndex)
    {
        currentTabIndex = tabIndex;
        Debug.Log($"[QuestTabPanelListCreator] 탭 {tabIndex} 클릭됨. 리스트 갱신 시작.");

        ClearList();

        List<QuestData> questList = GetQuestDataForTab(tabIndex);
        Debug.Log($"[QuestTabPanelListCreator] 탭 {tabIndex}에 해당하는 퀘스트 개수: {questList.Count}");

        foreach (var quest in questList)
        {
            GameObject questItemObj = Instantiate(questItemPrefab, contentParent);
            var controller = questItemObj.GetComponent<QuestTabPanelItemController>();
            if (controller != null)
            {
                controller.SetData(quest);
            }
            else
            {
                Debug.LogError("[QuestTabPanelListCreator] 생성된 프리팹에 QuestTabPanelItemController가 없습니다!");
            }
        }
    }

    private void ClearList()
    {
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }
        Debug.Log("[QuestTabPanelListCreator] 기존 퀘스트 아이템을 모두 제거했습니다.");
    }

    private List<QuestData> GetQuestDataForTab(int tabIndex)
    {
        if (questDataCollection == null)
        {
            Debug.Log("[QuestTabPanelListCreator] Quest 데이터가 아직 로드되지 않았습니다.");
            return new List<QuestData>();
        }
        switch (tabIndex)
        {
            case 0:
                return questDataCollection.DailyQuestList;
            case 1:
                return questDataCollection.WeeklyQuestList;
            case 2:
                return questDataCollection.RepeatQuestList;
            case 3:
                return questDataCollection.AchievementQuestList;
            default:
                Debug.LogWarning($"[QuestTabPanelListCreator] 알 수 없는 탭 인덱스: {tabIndex}");
                return new List<QuestData>();
        }
    }
}

[System.Serializable]
public class QuestDataCollection
{
    [JsonProperty("dailyQuestList")]
    public List<QuestData> DailyQuestList { get; set; }

    [JsonProperty("weeklyQuestList")]
    public List<QuestData> WeeklyQuestList { get; set; }

    [JsonProperty("repeatQuestList")]
    public List<QuestData> RepeatQuestList { get; set; }

    [JsonProperty("achievementQuestList")]
    public List<QuestData> AchievementQuestList { get; set; }
}
