using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class QuestTabPanelListCreator : MonoBehaviour
{
    [Header("Prefab & Content")]
    [Tooltip("퀘스트 프리팹")]
    public GameObject questDetailPrefab;

    [Tooltip("ScrollView의 Content(자식 오브젝트가 실제 목록이 됨)")]
    public Transform contentParent;

    [Header("Tab Buttons")]
    [Tooltip("일일 퀘스트 탭 버튼")]
    public Button dailyTabButton;

    [Tooltip("주간 퀘스트 탭 버튼")]
    public Button weeklyTabButton;

    [Tooltip("반복 퀘스트 탭 버튼")]
    public Button repeatableTabButton;

    [Tooltip("업적 탭 버튼")]
    public Button achievementsTabButton;

    /// <summary>
    /// 초기화: 탭 버튼에 이벤트 등록 및 기본 탭 표시
    /// </summary>
    void Start()
    {
        // 탭 버튼 클릭 시 OnClickTab 호출
        dailyTabButton.onClick.AddListener(() => OnClickTab(0));
        weeklyTabButton.onClick.AddListener(() => OnClickTab(1));
        repeatableTabButton.onClick.AddListener(() => OnClickTab(2));
        achievementsTabButton.onClick.AddListener(() => OnClickTab(3));

        // 처음에는 0번(일퀘) 탭 데이터 표시
        OnClickTab(0);
    }

    /// <summary>
    /// 탭을 클릭했을 때 리스트를 새로 갱신한다.
    /// </summary>
    /// <param name="tabIndex">0: Daily, 1: Weekly, 2: Repeatable, 3: Achievements</param>
    public void OnClickTab(int tabIndex)
    {
        Debug.Log($"[QuestTabPanelListCreator] 탭 {tabIndex} 클릭됨. 리스트 갱신 시작.");

        // 1) 기존 리스트 제거
        ClearList();

        // 2) 탭 인덱스에 맞는 퀘스트 데이터 목록 가져오기
        List<QuestData> questList = GetQuestDataForTab(tabIndex);
        Debug.Log($"[QuestTabPanelListCreator] 탭 {tabIndex}에 해당하는 퀘스트 개수: {questList.Count}");

        // 3) 각 퀘스트 데이터를 바탕으로 프리팹 생성
        foreach (var quest in questList)
        {
            // 프리팹을 contentParent 밑에 인스턴스화
            GameObject questItemObj = Instantiate(questDetailPrefab, contentParent);

            // 프리팹에 붙은 QuestTabPanelItemController 스크립트를 찾아서 데이터 할당
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

    /// <summary>
    /// 기존에 생성된 퀘스트 항목을 모두 제거한다.
    /// </summary>
    private void ClearList()
    {
        // contentParent 아래 모든 자식(퀘스트 아이템)을 제거
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }

        Debug.Log("[QuestTabPanelListCreator] 기존 퀘스트 아이템을 모두 제거했습니다.");
    }

    /// <summary>
    /// 탭 인덱스별로 다른 데이터 목록을 반환한다.
    /// 실제로는 서버API나 DB에서 가져올 수도 있음.
    /// </summary>
    private List<QuestData> GetQuestDataForTab(int tabIndex)
    {
        switch (tabIndex)
        {
            case 0:
                return GetDailyQuests();      // 일일
            case 1:
                return GetWeeklyQuests();     // 주간
            case 2:
                return GetRepeatableQuests(); // 반복
            case 3:
                return GetAchievements();     // 업적
        }
        // 혹시 모를 예외 상황
        Debug.LogWarning($"[QuestTabPanelListCreator] 알 수 없는 탭 인덱스: {tabIndex}");
        return new List<QuestData>();
    }

    // 아래 함수들은 예시: 실제론 서버나 ScriptableObject 등에서 로드할 수 있음.

    private List<QuestData> GetDailyQuests()
    {
        Debug.Log("[QuestTabPanelListCreator] 일일퀘 데이터를 불러옵니다.");
        return new List<QuestData>()
        {
            new QuestData("일일 퀘스트 1: 몬스터 10마리 처치"),
            new QuestData("일일 퀘스트 2: 아이템 5개 수집"),
            new QuestData("일일 퀘스트 3: 파티 플레이 1회")
        };
    }

    private List<QuestData> GetWeeklyQuests()
    {
        Debug.Log("[QuestTabPanelListCreator] 주간퀘 데이터를 불러옵니다.");
        return new List<QuestData>()
        {
            new QuestData("주간 퀘스트 1: 보스 처치 3회"),
            new QuestData("주간 퀘스트 2: PVP 승리 2회")
        };
    }

    private List<QuestData> GetRepeatableQuests()
    {
        Debug.Log("[QuestTabPanelListCreator] 반복퀘 데이터를 불러옵니다.");
        return new List<QuestData>()
        {
            new QuestData("반복 퀘스트 1: 물약 제조하기"),
            new QuestData("반복 퀘스트 2: 낚시 1회"),
            new QuestData("반복 퀘스트 3: 채집 2회"),
        };
    }

    private List<QuestData> GetAchievements()
    {
        Debug.Log("[QuestTabPanelListCreator] 업적 데이터를 불러옵니다.");
        return new List<QuestData>()
        {
            new QuestData("업적 A: 친구 10명 추가"),
            new QuestData("업적 B: 첫 길드 가입"),
        };
    }
}
