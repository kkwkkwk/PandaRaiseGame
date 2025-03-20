using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;  // Newtonsoft (Json.NET) 사용 시

public class GuildTabPanelController : MonoBehaviour
{
    [Header("Server Info")]
    [Tooltip("길드 정보를 가져오기 위한 서버 URL(Azure Function 등)")]
    public string getGuildInfoURL = "https://your-azure-function-url/GetGuildInfo";

    [Header("Guild Join State")]
    [Tooltip("현재 유저가 길드에 가입되어 있는지 여부")]
    public bool isJoinedGuild = false;

    [Header("Tab Buttons")]
    public Button guildInfoTabButton;       // 길드 정보 탭
    public Button guildMissionTabButton;    // 길드 미션 탭
    public Button guildStatTabButton;       // 길드 스탯 탭
    public Button guildRegisterTabButton;   // 길드 가입 탭
    public Button guildRankTabButton;       // 길드 랭킹 탭

    [Header("Tab Panels")]
    [Tooltip("길드 정보 UI 패널")]
    public GameObject guildInfoPanel;       // GuildPopupMainUI_Panel
    [Tooltip("길드 미션 UI 패널")]
    public GameObject guildMissionPanel;    // GuildPopupMissionUI_Panel
    [Tooltip("길드 스탯 UI 패널")]
    public GameObject guildStatPanel;       // GuildPopupStatUI_Panel
    [Tooltip("길드 가입 UI 패널")]
    public GameObject guildRegisterPanel;   // GuildPopupRegisterUI_Panel
    [Tooltip("길드 랭킹 UI 패널")]
    public GameObject guildRankPanel;       // GuildPopupRankUI_Panel


    [Header("Guild User List")]
    public GuildUserPanelListCreator guildUserListCreator;  // ScrollView에 프리팹을 생성해줄 스크립트
    // 서버에서 가져온 길드원 정보 리스트
    private List<GuildMemberData> currentGuildMemberList;

    [Header("Guild Mission List")]
    public GuildMissionPanelListCreator guildMissionListCreator;
    // 서버나 다른 곳에서 받아올 길드 미션 목록
    private List<GuildMissionData> currentGuildMissionList;

    [Header("Guild Register List (추천 길드)")]
    public GuildListPanelListCreator guildListPanelListCreator;  // ScrollView에 추천 길드 목록 표시용
    private List<GuildData> currentGuildRegisterList;  // 추천 길드 목록

    /// <summary>
    /// 현재 선택된 탭 인덱스 (0: 길드정보, 1: 미션, 2: 스탯, 3: 가입, 4: 랭킹)
    /// </summary>
    private int currentTabIndex;

    // 서버 응답 DTO
    private class GuildInfoResponse
    {
        public string playFabId;        // 유저의 플레이팹 아이디
        public string guildId;          // 유저가 가입한 길드 ID (가입하지 않은 경우 null 또는 빈 문자열)
        public string guildName;        // 길드 이름

        public int guildRank;        // 길드 랭킹
        public int guildLevel;       // 길드 레벨
        public int guildHeadCount;   // 길드 인원 수

        // 길드원 목록 (있으면 가입한 상태로 판단)
        public List<GuildMemberData> guildMemberList;
        // 길드 미션 목록 (길드 정보 응답에 포함)
        public List<GuildMissionData> guildMissionList;
        // 가입하지 않은 경우(추천 길드 목록) 서버에서 내려올 수도 있음
        public List<GuildData> guildRegisterList;
    }

    void Awake()
    {
        if (guildInfoTabButton != null)
        {
            Debug.Log("[GuildTabPanelController] guildInfoTabButton 할당됨");
            guildInfoTabButton.onClick.AddListener(() => OnClickTab(0));
        }
        else Debug.LogWarning("[GuildTabPanelController] guildInfoTabButton가 인스펙터에 할당되지 않았습니다!");

        if (guildMissionTabButton != null)
        {
            Debug.Log("[GuildTabPanelController] guildMissionTabButton 할당됨");
            guildMissionTabButton.onClick.AddListener(() => OnClickTab(1));
        }
        else Debug.LogWarning("[GuildTabPanelController] guildMissionTabButton가 인스펙터에 할당되지 않았습니다!");

        if (guildStatTabButton != null)
        {
            Debug.Log("[GuildTabPanelController] guildStatTabButton 할당됨");
            guildStatTabButton.onClick.AddListener(() => OnClickTab(2));
        }
        else Debug.LogWarning("[GuildTabPanelController] guildStatTabButton가 인스펙터에 할당되지 않았습니다!");

        if (guildRegisterTabButton != null)
        {
            Debug.Log("[GuildTabPanelController] guildRegisterTabButton 할당됨");
            guildRegisterTabButton.onClick.AddListener(() => OnClickTab(3));
        }
        else Debug.LogWarning("[GuildTabPanelController] guildRegisterTabButton가 인스펙터에 할당되지 않았습니다!");

        if (guildRankTabButton != null)
        {
            Debug.Log("[GuildTabPanelController] guildRankTabButton 할당됨");
            guildRankTabButton.onClick.AddListener(() => OnClickTab(4));
        }
        else Debug.LogWarning("[GuildTabPanelController] guildRankTabButton가 인스펙터에 할당되지 않았습니다!");

        if (guildInfoTabButton) guildInfoTabButton.gameObject.SetActive(false);
        if (guildMissionTabButton) guildMissionTabButton.gameObject.SetActive(false);
        if (guildStatTabButton) guildStatTabButton.gameObject.SetActive(false);
        if (guildRegisterTabButton) guildRegisterTabButton.gameObject.SetActive(false);
        if (guildRankTabButton) guildRankTabButton.gameObject.SetActive(false);
        Debug.Log("[GuildTabPanelController] Awake(). All buttons forcibly disabled.");
    }

    void OnEnable()
    {
        Debug.Log("[GuildTabPanelController] OnEnable() -> StartCoroutine(GetGuildInfoCoroutine())");
        StartCoroutine(GetGuildInfoCoroutine());
    }

    void Start()
    {
        Debug.Log("[GuildTabPanelController] Start()");
    }

    /// <summary>
    /// 서버에 플레이어의 길드 정보를 요청하여 isJoinedGuild 값을 세팅하고 UI를 초기화.
    /// </summary>
    private IEnumerator GetGuildInfoCoroutine()
    {

        // (1) 요청 JSON
        string jsonData = $"{{\"playFabId\":\"{GlobalData.playFabId}\"}}";
        Debug.Log($"[GuildTabPanelController] [GetGuildInfo] 요청 JSON: {jsonData}");

        // (2) UnityWebRequest 설정 (POST 예시)
        UnityWebRequest request = new UnityWebRequest(getGuildInfoURL, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // (3) 요청 전송
        yield return request.SendWebRequest();

        // (4) 결과 확인
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[GetGuildInfo] 호출 실패: {request.error}");
            Debug.Log("[GetGuildInfo] 서버 통신 실패로 isJoinedGuild를 false로 설정하고 초기화 진행.");

            /*
             서버 연결 용

             // 서버 연결 실패 시, false 세팅
            isJoinedGuild = false;
            currentGuildMemberList = null;
            currentGuildMissionList = null;
            currentGuildRegisterList = null;
            InitializeGuildTabs();
             
             */


            //코딩 테스트 용

            // 가입 여부 임의로 true/false
            isJoinedGuild = true;

            // 임시 길드원 데이터
            currentGuildMemberList = new List<GuildMemberData>()
            {
                new GuildMemberData() { userName="홍길동", userClass="길드원", userPower=12345, isOnline=true },
                new GuildMemberData() { userName="김길동", userClass="길드마스터", userPower=99999, isOnline=false },
                new GuildMemberData() { userName="박길동", userClass="부길마", userPower=50000, isOnline=true },
            };

            // 미션 목록 임의 생성
            currentGuildMissionList = new List<GuildMissionData>()
            {
                new GuildMissionData() { missionContent="길드 출석하기", isCleared=false },
                new GuildMissionData() { missionContent="길드 보스 처치", isCleared=true },
                new GuildMissionData() { missionContent="길드 레이드 5회 클리어", isCleared=false },
            };
            // 여기에 "추천 길드" 임시 데이터 추가
            currentGuildRegisterList = new List<GuildData>()
            {
                new GuildData(){ guildName="길드A", guildLevel=10, guildHeadCount=50 },
                new GuildData(){ guildName="길드B", guildLevel=8, guildHeadCount=40 },
                new GuildData(){ guildName="길드C", guildLevel=12, guildHeadCount=30 }
            };
            InitializeGuildTabs();


        }
        else
        {
            string responseJson = request.downloadHandler.text;
            Debug.Log($"[GetGuildInfo] 호출 성공, 응답: {responseJson}");

            try
            {
                GuildInfoResponse response = JsonConvert.DeserializeObject<GuildInfoResponse>(responseJson);

                if (response != null)
                {
                    // guildId가 null 또는 빈 문자열이면 가입하지 않은 것으로 판단
                    isJoinedGuild = !string.IsNullOrEmpty(response.guildId);
                    Debug.Log($"[GetGuildInfo] 응답 파싱 완료. isJoinedGuild={isJoinedGuild}");

                    if (isJoinedGuild)
                    {
                        // 길드원 목록
                        currentGuildMemberList = response.guildMemberList;

                        // 길드 미션 목록
                        currentGuildMissionList = response.guildMissionList;

                        // 추천 길드 목록은 사용하지 않음
                        currentGuildRegisterList = null;
                    }
                    else
                    {
                        // 미가입 상태: 추천 길드 목록 할당
                        currentGuildMemberList = null;
                        currentGuildMissionList = null;

                        // 만약 서버 응답에 추천 길드 목록이 있다면 아래와 같이 할당
                        // currentGuildRegisterList = response.guildRegisterList;
                        // 임시 테스트 데이터 (추천 길드 목록)
                        currentGuildRegisterList = new List<GuildData>()
                        {
                            new GuildData(){ guildName="길드A", guildLevel=10, guildHeadCount=50 },
                            new GuildData(){ guildName="길드B", guildLevel=8, guildHeadCount=40 },
                            new GuildData(){ guildName="길드C", guildLevel=12, guildHeadCount=30 }
                        };
                    }
                    InitializeGuildTabs();
                }
                else
                {
                    Debug.LogError("[GetGuildInfo] 응답 파싱 실패! (response == null)");
                    isJoinedGuild = false;
                    currentGuildMemberList = null;
                    currentGuildMissionList = null;
                    currentGuildRegisterList = null;
                    InitializeGuildTabs();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GetGuildInfo] JSON 파싱 오류: {ex.Message}");
                isJoinedGuild = false;
                currentGuildMemberList = null;
                currentGuildMissionList = null;
                currentGuildRegisterList = null;
                InitializeGuildTabs();
            }
        }
    }

    /// <summary>
    /// 길드 가입 여부에 따라 탭(버튼)과 기본 탭을 세팅하는 함수
    /// </summary>
    public void InitializeGuildTabs()
    {
        Debug.Log($"[InitializeGuildTabs] isJoinedGuild = {isJoinedGuild}");

        // (1) 먼저 전부 끄기
        guildInfoTabButton.gameObject.SetActive(false);
        guildMissionTabButton.gameObject.SetActive(false);
        guildStatTabButton.gameObject.SetActive(false);
        guildRegisterTabButton.gameObject.SetActive(false);
        guildRankTabButton.gameObject.SetActive(false);

        Debug.Log("[InitializeGuildTabs] 먼저 모든 버튼을 SetActive(false) 처리 완료.");

        // (2) 그 다음 분기에 따라 필요한 것만 켜기
        if (isJoinedGuild)
        {
            guildInfoTabButton.gameObject.SetActive(true);
            guildMissionTabButton.gameObject.SetActive(true);
            guildStatTabButton.gameObject.SetActive(true);
            guildRankTabButton.gameObject.SetActive(true);
            Debug.Log("[InitializeGuildTabs] 길드 가입 상태 -> 정보/미션/스탯/랭킹 4개 탭만 켬.");

            OnClickTab(0); // 길드 정보 탭
        }
        else
        {
            guildRegisterTabButton.gameObject.SetActive(true);
            guildRankTabButton.gameObject.SetActive(true);
            Debug.Log("[InitializeGuildTabs] 길드 미가입 상태 -> 가입/랭킹 2개 탭만 켬.");

            OnClickTab(3); // 길드 가입 탭
        }
    }


    /// <summary>
    /// 탭 버튼 클릭 시 실행되는 함수
    /// </summary>
    /// <param name="tabIndex">0: 길드 정보, 1: 길드 미션, 2: 길드 스탯, 3: 길드 가입, 4: 길드 랭킹</param>
    public void OnClickTab(int tabIndex)
    {
        currentTabIndex = tabIndex;
        Debug.Log($"[OnClickTab] 탭 {tabIndex} 클릭됨 -> HideAllPanels 후 해당 탭 패널 활성화");
        HideAllPanels();

        switch (tabIndex)
        {
            case 0:
                if (guildInfoPanel != null)
                {
                    guildInfoPanel.SetActive(true);
                    Debug.Log($"[OnClickTab] guildInfoPanel SetActive(true)");
                }
                if (guildUserListCreator != null && currentGuildMemberList != null) // 여기서 길드원 프리팹들을 ScrollView에 배치
                {
                    Debug.Log($"[OnClickTab(0)] 길드원 목록을 ScrollView에 표시합니다. 멤버 수: {currentGuildMemberList.Count}");
                    guildUserListCreator.SetGuildUserList(currentGuildMemberList);
                }
                else
                {
                    Debug.LogWarning("[OnClickTab(0)] guildUserListCreator 또는 currentGuildMemberList가 null입니다.");
                }
                break;
            case 1:
                if (guildMissionPanel != null)
                {
                    guildMissionPanel.SetActive(true);
                    Debug.Log($"[OnClickTab] guildMissionPanel SetActive(true)");
                }
                if (guildMissionListCreator != null && currentGuildMissionList != null) // 여기서 미션 프리팹들을 ScrollView에 배치
                {
                    Debug.Log($"[OnClickTab(1)] 길드 미션 목록 표시. 미션 수: {currentGuildMissionList.Count}");
                    guildMissionListCreator.SetGuildMissionList(currentGuildMissionList);
                }
                else
                {
                    Debug.LogWarning("[OnClickTab(1)] guildMissionListCreator 또는 currentGuildMissionList가 null입니다.");
                }
                break;
            case 2:
                if (guildStatPanel != null)
                {
                    guildStatPanel.SetActive(true);
                    Debug.Log($"[OnClickTab] guildStatPanel SetActive(true)");
                }
                break;
            case 3:
                if (guildRegisterPanel != null)
                {
                    guildRegisterPanel.SetActive(true);
                    Debug.Log("[OnClickTab] guildRegisterPanel 활성화");

                    // 미가입 상태: 추천 길드 목록(가입 가능한 길드 목록) 표시
                    if (guildListPanelListCreator != null && currentGuildRegisterList != null)
                    {
                        Debug.Log($"[OnClickTab(3)] 추천 길드 목록 표시, 길드 수: {currentGuildRegisterList.Count}");
                        guildListPanelListCreator.SetGuildList(currentGuildRegisterList);
                    }
                }
                break;
            case 4:
                if (guildRankPanel != null)
                {
                    guildRankPanel.SetActive(true);
                    Debug.Log($"[OnClickTab] guildRankPanel SetActive(true)");
                }
                break;
            default:
                Debug.LogWarning($"[OnClickTab] 알 수 없는 탭 인덱스: {tabIndex}");
                break;
        }
    }

    /// <summary>
    /// 모든 패널을 비활성화하여, 새 탭 패널만 보이게 합니다.
    /// </summary>
    private void HideAllPanels()
    {
        // 안전하게 null 체크 후 SetActive(false) 처리
        if (guildInfoPanel)
        {
            guildInfoPanel.SetActive(false);
            Debug.Log($"[HideAllPanels] guildInfoPanel SetActive(false)");
        }
        if (guildMissionPanel)
        {
            guildMissionPanel.SetActive(false);
            Debug.Log($"[HideAllPanels] guildMissionPanel SetActive(false)");
        }
        if (guildStatPanel)
        {
            guildStatPanel.SetActive(false);
            Debug.Log($"[HideAllPanels] guildStatPanel SetActive(false)");
        }
        if (guildRegisterPanel)
        {
            guildRegisterPanel.SetActive(false);
            Debug.Log($"[HideAllPanels] guildRegisterPanel SetActive(false)");
        }
        if (guildRankPanel)
        {
            guildRankPanel.SetActive(false);
            Debug.Log($"[HideAllPanels] guildRankPanel SetActive(false)");
        }
    }
}
