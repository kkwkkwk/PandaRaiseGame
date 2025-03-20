using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class GuildTabPanelController : MonoBehaviour
{
    [Header("Server Info")]
    private string getGuildInfoURL = "https://pandaraisegame-guild.azurewebsites.net/api/GetGuildInfo?code=rF0MNAlNGQAzW_jLfF3A5QBKvRCGmR92yd3CTvzb5NopAzFuLTJeAg==";

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

    private bool isJoinedGuild = false;

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
        // Guild Info 조회
        StartCoroutine(GetGuildInfoCoroutine());
    }

    private IEnumerator GetGuildInfoCoroutine()
    {
        // 예: { "playFabId": GlobalData.playFabId }
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
                isJoinedGuild = false;
                currentGuildMemberList = null;
            }
            else
            {
                isJoinedGuild = serverResponse.isJoined;
                if (isJoinedGuild)
                {
                    // ===== 여기서 서버 멤버 구조 -> GuildMemberData로 변환 =====
                    currentGuildMemberList = new List<GuildMemberData>();
                    if (serverResponse.memberList != null)
                    {
                        foreach (var m in serverResponse.memberList)
                        {
                            // 서버에서 받은 displayName -> userName
                            // 서버에서 받은 role -> userClass
                            // 서버에서 받은 power -> userPower
                            // isOnline은 임의로 false (또는 다른 로직)
                            var localData = new GuildMemberData
                            {
                                userName = m.displayName,
                                userClass = m.role,
                                userPower = m.power,
                                isOnline = false
                            };
                            currentGuildMemberList.Add(localData);
                        }
                    }
                }
                else
                {
                    // 길드 미가입
                    currentGuildMemberList = null;
                }
            }

            InitializeGuildTabs();
        }
    }

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
            guildRegisterTabButton.gameObject.SetActive(true);
            guildRankTabButton.gameObject.SetActive(true);

            OnClickTab(3);
        }
    }

    public void OnClickTab(int idx)
    {
        HideAllPanels();

        switch (idx)
        {
            case 0:
                if (guildInfoPanel) guildInfoPanel.SetActive(true);

                // 길드원 목록 표시
                if (guildUserListCreator != null && currentGuildMemberList != null)
                {
                    guildUserListCreator.SetGuildUserList(currentGuildMemberList);
                }
                break;
            case 1:
                if (guildMissionPanel) guildMissionPanel.SetActive(true);
                break;
            case 2:
                if (guildStatPanel) guildStatPanel.SetActive(true);
                break;
            case 3:
                if (guildRegisterPanel) guildRegisterPanel.SetActive(true);
                break;
            case 4:
                if (guildRankPanel) guildRankPanel.SetActive(true);
                break;
        }
    }

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
// 서버 응답 DTO
// -----------------------------------------------------
[System.Serializable]
public class GuildInfoResponse
{
    public bool isJoined;
    public string groupId;
    public string groupName;
    public List<ServerGuildMember> memberList;
}

// -----------------------------------------------------
// 서버 측 멤버 구조
// -----------------------------------------------------
[System.Serializable]
public class ServerGuildMember
{
    public string entityId;
    public string role;
    public string displayName;
    public int power;
}
