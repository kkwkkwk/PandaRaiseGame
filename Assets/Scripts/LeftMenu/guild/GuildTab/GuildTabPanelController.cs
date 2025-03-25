using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class GuildTabPanelController : MonoBehaviour
{

    public static GuildTabPanelController Instance { get; private set; }

    [Header("Server Info")]
    private string getGuildInfoURL = "https://pandaraisegame-guild.azurewebsites.net/api/GetGuildInfo?code=rF0MNAlNGQAzW_jLfF3A5QBKvRCGmR92yd3CTvzb5NopAzFuLTJeAg==";
    private string getGuildMissionURL = "https://pandaraisegame-guild.azurewebsites.net/api/GetGuildMissionInfo?code=_ZkbGvKAPencIUseyN0dApN-WiB9XfrCOp1GBWw7MZ06AzFuiw5Ejw==";

    [Header("Tab Buttons")]
    public Button guildInfoTabButton;
    public Button guildMissionTabButton;
    public Button guildStatTabButton;
    public Button guildRegisterTabButton;
    public Button guildRankTabButton;

    [Header("Tab Panels")]
    public GameObject guildInfoPanel;
    public GameObject guildMissionPanel;
    public GameObject guildStatPanel;
    public GameObject guildRegisterPanel;
    public GameObject guildRankPanel;

    [Header("Guild User List")]
    public GuildUserPanelListCreator guildUserListCreator; // ScrollView 쪽에 프리팹 배치 스크립트
    private List<GuildMemberData> currentGuildMemberList;  // 실제 UI에 표시할 최종 멤버 목록

    [Header("Guild Mission List")]
    public GuildMissionPanelListCreator guildMissionListCreator;  // 미션 목록 표시용
    private List<GuildMissionData> currentGuildMissionList;       // 미션 데이터를 저장

    private bool isJoinedGuild = false;
    private bool myIsMasterOrSub = false; // GuildTabPanelController 클래스 멤버 변수

    void Awake()
    {
        guildInfoTabButton.onClick.AddListener(() => OnClickTab(0));
        guildMissionTabButton.onClick.AddListener(() => OnClickTab(1));
        guildStatTabButton.onClick.AddListener(() => OnClickTab(2));
        guildRegisterTabButton.onClick.AddListener(() => OnClickTab(3));
        guildRankTabButton.onClick.AddListener(() => OnClickTab(4));

        // 초기 비활성
        guildInfoTabButton.gameObject.SetActive(false);
        guildMissionTabButton.gameObject.SetActive(false);
        guildStatTabButton.gameObject.SetActive(false);
        guildRegisterTabButton.gameObject.SetActive(false);
        guildRankTabButton.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // 길드 정보 조회
        StartCoroutine(GetGuildInfoCoroutine());
    }

    /// <summary>
    /// 서버로부터 길드 가입 정보 + 길드원 목록 등을 받아오는 코루틴
    /// </summary>
    private IEnumerator GetGuildInfoCoroutine()
    {
        var requestJson = new
        {
            playFabId = GlobalData.playFabId,
            entityToken = new
            {
                Entity = new
                {
                    Id = GlobalData.entityToken.Entity.Id,
                    Type = GlobalData.entityToken.Entity.Type
                },
                EntityToken = GlobalData.entityToken.EntityToken,
                TokenExpiration = GlobalData.entityToken.TokenExpiration
            }
        };
        string jsonString = JsonConvert.SerializeObject(requestJson);

        UnityWebRequest request = new UnityWebRequest(getGuildInfoURL, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonString);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"GetGuildInfo 실패: {request.error}");
            isJoinedGuild = false;
            currentGuildMemberList = null;

            myIsMasterOrSub = false;

            InitializeGuildTabs();
        }
        else
        {
            string responseJson = request.downloadHandler.text;
            Debug.Log("[GetGuildInfo] 응답: " + responseJson);

            GuildInfoResponse serverResponse = null;
            try
            {
                serverResponse = JsonConvert.DeserializeObject<GuildInfoResponse>(responseJson);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[GetGuildInfo] JSON 파싱 오류: " + ex.Message);
            }

            if (serverResponse == null)
            {
                // 응답이 유효하지 않으면 길드 미가입으로 처리
                isJoinedGuild = false;
                currentGuildMemberList = null;
                myIsMasterOrSub = false;
            }
            else
            {
                isJoinedGuild = serverResponse.isJoined;

                if (isJoinedGuild)
                {
                    // 1) 길드원 목록 세팅
                    currentGuildMemberList = new List<GuildMemberData>();
                    if (serverResponse.memberList != null)
                    {
                        foreach (var m in serverResponse.memberList)
                        {
                            var localData = new GuildMemberData
                            {
                                userName = m.displayName,
                                userClass = m.role,
                                userPower = m.power,
                                isOnline = m.isOnline,
                            };
                            currentGuildMemberList.Add(localData);
                        }
                    }

                    // 2) GuildInfoPanel: 길드 이름 / 레벨 / 소개 설정
                    var infoPanelCtrl = guildInfoPanel.GetComponent<GuildInfoPanelController>();
                    if (infoPanelCtrl != null)
                    {
                        // groupLevel과 groupDesc가 int, string으로 서버 응답 DTO에 정의되어 있다고 가정
                        infoPanelCtrl.SetGuildInfo(
                            serverResponse.groupName,
                            serverResponse.groupLevel,
                            serverResponse.groupDesc
                        );
                    }

                    // 6) 내가 길드마스터/부마스터인지 판별
                    myIsMasterOrSub = false;
                    string myEntityId = GlobalData.entityToken.Entity.Id;

                    if (serverResponse.memberList != null)
                    {
                        foreach (var m in serverResponse.memberList)
                        {
                            if (m.entityId == myEntityId)
                            {
                                // "길드마스터", "부마스터" 등 체크
                                if (m.role == "길드마스터" || m.role == "부마스터")
                                {
                                    myIsMasterOrSub = true;
                                }
                                break;
                            }
                        }
                    }

                    Debug.Log($"[GetGuildInfo] 내가 마스터/부마스터인가? {myIsMasterOrSub}");
                }
                else
                {
                    // 길드 미가입
                    currentGuildMemberList = null;
                }
            }

            // 탭 초기화
            InitializeGuildTabs();
        }
    }



    /// <summary>
    /// 길드 미션 정보를 서버(Azure Function)로부터 받아오는 코루틴
    /// </summary>
    private IEnumerator GetGuildMissionCoroutine()
    {
        Debug.Log("[GuildTabPanelController] 길드 미션 조회 시작...");
        using (UnityWebRequest request = new UnityWebRequest(getGuildMissionURL, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes("{}");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[GuildTabPanelController] 길드 미션 조회 실패: " + request.error);
                yield break;
            }

            string responseJson = request.downloadHandler.text;
            Debug.Log("[GuildTabPanelController] 길드 미션 조회 성공, 응답: " + responseJson);

            // 1) GuildMissionResponse 역직렬화
            GuildMissionResponse response = null;
            try
            {
                response = JsonConvert.DeserializeObject<GuildMissionResponse>(responseJson);
                Debug.Log("[GuildTabPanelController] GuildMissionResponse.GuildMissionData: " + response.GuildMissionData);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[GuildTabPanelController] JSON 파싱 오류(1단계): " + ex.Message);
                yield break;
            }

            if (response == null || string.IsNullOrEmpty(response.GuildMissionData))
            {
                Debug.LogWarning("[GuildTabPanelController] GuildMissionData가 비어있음.");
                yield break;
            }

            // 2) 내부 JSON 객체를 GuildMissionTitleData로 역직렬화
            GuildMissionTitleData missionTitleData = null;
            try
            {
                missionTitleData = JsonConvert.DeserializeObject<GuildMissionTitleData>(response.GuildMissionData);
                if (missionTitleData != null && missionTitleData.GuildMissionList != null)
                {
                    Debug.Log("[GuildTabPanelController] 역직렬화 성공, 미션 개수: " + missionTitleData.GuildMissionList.Count);
                }
                else
                {
                    Debug.LogWarning("[GuildTabPanelController] missionTitleData 또는 미션 목록이 null입니다.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[GuildTabPanelController] JSON 파싱 오류(2단계): " + ex.Message);
                yield break;
            }

            if (missionTitleData == null || missionTitleData.GuildMissionList == null)
            {
                Debug.LogWarning("[GuildTabPanelController] 길드 미션 목록이 비어 있습니다.");
                yield break;
            }

            // 3) DTO -> 클라이언트용 GuildMissionData 변환
            currentGuildMissionList = new List<GuildMissionData>();
            foreach (var dto in missionTitleData.GuildMissionList)
            {
                currentGuildMissionList.Add(new GuildMissionData
                {
                    guildMissionID = dto.id,
                    guildMissionReward = "미구현 보상",
                    missionContent = $"{dto.title}",
                    isCleared = false
                });
            }

            Debug.Log("[GuildTabPanelController] 최종 변환된 미션 개수: " + currentGuildMissionList.Count);

            // 미션 탭이 활성화되어 있다면 UI 갱신
            if (guildMissionPanel.activeSelf && guildMissionListCreator != null)
            {
                guildMissionListCreator.SetGuildMissionList(currentGuildMissionList);
            }
        }
    }

    public void RemoveMemberAndRefresh(GuildMemberData memberData)
    {
        if (currentGuildMemberList != null)
        {
            // userName으로 식별, 혹은 별도의 고유 ID가 있다면 그걸로 찾는 게 더 안전
            GuildMemberData found = currentGuildMemberList.Find(m => m.userName == memberData.userName);

            if (found != null)
            {
                currentGuildMemberList.Remove(found);
                Debug.Log($"[GuildTabPanelController] {found.userName} 를 리스트에서 제거");
            }
            else
            {
                Debug.LogWarning($"[GuildTabPanelController] {memberData.userName}가 리스트 내에 없습니다.");
            }
        }

        // 탭이 0 (길드 정보 탭)이 활성화 중이라면, 리스트 다시 표시
        // 혹은 그냥 OnClickTab(0)을 다시 호출해도 됨
        if (guildUserListCreator != null && currentGuildMemberList != null)
        {
            guildUserListCreator.SetGuildUserList(currentGuildMemberList, myIsMasterOrSub);
        }
    }

    /// <summary>
    /// 길드 탭 버튼 활성화/비활성 처리
    /// </summary>
    private void InitializeGuildTabs()
    {
        guildInfoTabButton.gameObject.SetActive(false);
        guildMissionTabButton.gameObject.SetActive(false);
        guildStatTabButton.gameObject.SetActive(false);
        guildRegisterTabButton.gameObject.SetActive(false);
        guildRankTabButton.gameObject.SetActive(false);

        if (isJoinedGuild)
        {
            guildInfoTabButton.gameObject.SetActive(true);
            guildMissionTabButton.gameObject.SetActive(true);
            guildStatTabButton.gameObject.SetActive(true);
            guildRankTabButton.gameObject.SetActive(true);

            OnClickTab(0);
        }
        else
        {
            // 길드 미가입
            guildRegisterTabButton.gameObject.SetActive(true);
            guildRankTabButton.gameObject.SetActive(true);

            OnClickTab(3);
        }
    }

    /// <summary>
    /// 탭 버튼을 클릭했을 때 호출되는 함수
    /// </summary>
    public void OnClickTab(int idx)
    {
        HideAllPanels();

        switch (idx)
        {
            case 0:
                // 길드 정보 패널
                if (guildInfoPanel) guildInfoPanel.SetActive(true);

                // 길드원 목록 표시
                if (guildUserListCreator != null && currentGuildMemberList != null)
                {
                    guildUserListCreator.SetGuildUserList(currentGuildMemberList, myIsMasterOrSub);
                }
                break;

            case 1:
                // 길드 미션 패널
                if (guildMissionPanel) guildMissionPanel.SetActive(true);

                // 아직 미션을 한 번도 불러오지 않았다면, 서버에서 가져옴
                if (currentGuildMissionList == null || currentGuildMissionList.Count == 0)
                {
                    StartCoroutine(GetGuildMissionCoroutine());
                }
                else
                {
                    // 이미 로드된 미션 목록이 있으면 바로 표시
                    if (guildMissionListCreator != null)
                    {
                        guildMissionListCreator.SetGuildMissionList(currentGuildMissionList);
                    }
                }
                break;

            case 2:
                // 길드 스탯 패널
                if (guildStatPanel) guildStatPanel.SetActive(true);
                break;

            case 3:
                // 길드 가입 패널
                if (guildRegisterPanel) guildRegisterPanel.SetActive(true);
                break;

            case 4:
                // 길드 랭크 패널
                if (guildRankPanel) guildRankPanel.SetActive(true);
                break;
        }
    }

    /// <summary>
    /// 모든 탭 패널을 비활성화
    /// </summary>
    private void HideAllPanels()
    {
        if (guildInfoPanel) guildInfoPanel.SetActive(false);
        if (guildMissionPanel) guildMissionPanel.SetActive(false);
        if (guildStatPanel) guildStatPanel.SetActive(false);
        if (guildRegisterPanel) guildRegisterPanel.SetActive(false);
        if (guildRankPanel) guildRankPanel.SetActive(false);
    }
}



// -----------------------------------------------------
// 서버 응답 DTO (길드 정보)
// -----------------------------------------------------
[System.Serializable]
public class GuildInfoResponse
{
    public bool isJoined;
    public string groupId;
    public string groupName;

    public int groupLevel;        // 레벨이 int라고 가정
    public string groupDesc;      // 길드 소개

    public List<ServerGuildMember> memberList;
}

[System.Serializable]
public class ServerGuildMember
{
    public string entityId;
    public string role;
    public string displayName;
    public int power;
    public bool isOnline;
}

// -----------------------------------------------------
// 서버 응답 DTO (길드 미션)
// -----------------------------------------------------
[System.Serializable]
public class GuildMissionResponse
{
    [JsonProperty("guildMissionData")]
    public string GuildMissionData { get; set; }
}

[System.Serializable]
public class GuildMissionDTO
{
    public string id;
    public string title;
    public string description;
    public int goal;
    public string resetType;
    // public object reward; // 필요 시
}

[System.Serializable]
public class GuildMissionTitleData
{
    [JsonProperty("GuildMissionList")]
    public List<GuildMissionDTO> GuildMissionList { get; set; }
}
