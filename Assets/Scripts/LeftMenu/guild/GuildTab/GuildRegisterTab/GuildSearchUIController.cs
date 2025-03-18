using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Text;
using TMPro;          // 혹은 InputField가 TMP가 아니라면 TMP는 제외

public class GuildSearchUIController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField searchInputField;     // 검색어 입력란
    public Button searchButton;                 // 검색 버튼

    [Header("Search Result")]
    public GuildListPanelListCreator searchResultListCreator;
    // 검색 결과를 표시할 ScrollView (GuildListPanelListCreator 스크립트)

    [Header("Server Info")]
    // 길드 검색을 수행하기 위한 서버 URL 
    public string guildSearchUrl = "https://your-azure-function-url/GetGuildSearch";

    private void Awake()
    {
        if (searchButton != null)
        {
            searchButton.onClick.AddListener(OnSearchButtonClicked);
        }
    }

    /// <summary>
    /// 검색 버튼을 클릭했을 때 호출되는 함수
    /// </summary>
    private void OnSearchButtonClicked()
    {
        if (searchInputField == null) return;

        string query = searchInputField.text.Trim();  // 검색어
        if (string.IsNullOrEmpty(query))
        {
            Debug.LogWarning("검색어가 비어 있습니다.");
            return;
        }
        Debug.Log($"[GuildSearchUIController] 검색어: {query}");

        // 서버로 검색 요청
        StartCoroutine(SearchGuildCoroutine(query));
    }

    /// <summary>
    /// 서버로 검색어를 전송, 부분 일치하는 길드 리스트를 받아온 뒤 ScrollView에 표시
    /// </summary>
    private IEnumerator SearchGuildCoroutine(string query)
    {
        // 예시로 { "searchQuery": "안녕" } 형태의 JSON을 보낸다고 가정
        string jsonData = $"{{\"searchQuery\":\"{query}\"}}";
        Debug.Log("[SearchGuildCoroutine] 요청 JSON: " + jsonData);

        // UnityWebRequest 준비 (POST)
        UnityWebRequest request = new UnityWebRequest(guildSearchUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[SearchGuildCoroutine] 검색 실패: {request.error}");
            // 필요하다면 UI에 “검색 실패” 알림 표시
        }
        else
        {
            string responseJson = request.downloadHandler.text;
            Debug.Log("[SearchGuildCoroutine] 검색 성공, 응답: " + responseJson);

            // 서버 응답을 GuildSearchResponse 등으로 파싱 (아래 예시 DTO를 참고)
            GuildSearchResponse response = null;
            try
            {
                response = Newtonsoft.Json.JsonConvert.DeserializeObject<GuildSearchResponse>(responseJson);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SearchGuildCoroutine] JSON 파싱 오류: {ex.Message}");
                yield break;
            }

            if (response == null || response.guildList == null)
            {
                Debug.LogWarning("[SearchGuildCoroutine] guildList가 없습니다.");
                // 필요하다면 “검색 결과 없음” 처리
            }
            else
            {
                // 스크롤뷰에 표시
                if (searchResultListCreator != null)
                {
                    searchResultListCreator.SetGuildList(response.guildList);
                }
                else
                {
                    Debug.LogWarning("[SearchGuildCoroutine] searchResultListCreator가 null입니다.");
                }
            }
        }
    }
}

/// <summary>
/// 서버에서 반환할 검색 결과 DTO 예시
/// </summary>
[System.Serializable]
public class GuildSearchResponse
{
    // 길드 데이터 목록 
    public List<GuildData> guildList;
    // 필요하면 검색 개수, 페이지 정보 등 추가 가능
}
