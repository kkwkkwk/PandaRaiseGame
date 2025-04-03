using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;  // HTTP 요청용
using TMPro;                  // TextMeshPro를 사용한다면 필요

[System.Serializable]
public class LeaderboardEntryData
{
    // Azure Function(또는 PlayFab)에서 반환되는 DTO 형태에 맞게 구성
    public int Rank;
    public string PlayFabId;    // null 가능하면 string?로
    public string DisplayName;  // null 가능하면 string?로
    public int StatValue;       // 여기서는 로그인 횟수 등 정수 값
}

public class LeaderboardPopupManager : MonoBehaviour
{

    public static LeaderboardPopupManager Instance { get; private set; }

    public GameObject LeaderboardCanvas;     // 리더보드 canvas
    public TextMeshProUGUI leaderboardText;      // 리더보드 결과 표시용 Text (TMPro)

    private string getLeaderboardUrl = "https://pandaraisegame-leaderboard.azurewebsites.net/api/GetLeaderBoard?code=Wpj4FIqgFxix697XXeysbU6M67cO_R-ekFq-jq5prpfsAzFuoeb33w==";

    private void Awake() // Unity의 Awake() 메서드에서 싱글톤 설정
    {
        // 만약 Instance가 비어 있으면, 이 스크립트를 싱글톤 인스턴스로 설정
        if (Instance == null)
        {
            Instance = this; // 현재 객체를 싱글톤으로 지정
        }
        else
        {
            // 이미 싱글톤 인스턴스가 존재하면, 중복된 객체를 제거
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    public IEnumerator StartSequence()
    {
        // 1) 먼저 리더보드를 조회
        yield return StartCoroutine(CallGetLeaderboard());

        // 2) 코루틴 완료 후 비활성화
        LeaderboardCanvas.SetActive(false);
        Debug.Log("[LeaderboardPopupManager] StartSequence() 완료 후 Canvas 비활성화");
    }


    public void OpenLeaderboardPopup()
    {   // 팝업 활성화 
        if (LeaderboardCanvas != null)
        {
            LeaderboardCanvas.SetActive(true);
            UnityEngine.Debug.Log("리더보드 열기");
            // 리더보드 가져오기
            StartCoroutine(CallGetLeaderboard());
        }
    }
    public void CloseLeaderboardPopup()
    {   // 팝업 비활성화 
        if (LeaderboardCanvas != null)
        {
            LeaderboardCanvas.SetActive(false);
            UnityEngine.Debug.Log("리더보드 닫기");
        }
    }
    public void RefreshLeaderboardData()
    {
        Debug.Log("[LeaderboardPopupManager] RefreshLeaderboardData() - 다시 GetLeaderboard 호출합니다.");
        StartCoroutine(CallGetLeaderboard());
    }

    /// <summary>
    /// Azure Function(GetLeaderBoard) 호출하여 리더보드를 가져오는 코루틴
    /// </summary>
    private IEnumerator CallGetLeaderboard()
    {
        Debug.Log("[Leaderboard] 리더보드 조회 시작...");

        // 1. 요청 생성 (POST 방식)
        using UnityWebRequest request = new UnityWebRequest(getLeaderboardUrl, "POST");

        // 요청 본문이 필요 없다면 빈 바디를 전송
        // 만약 startPosition, maxResultsCount 등의 파라미터를 JSON으로 보내고 싶다면,
        // 아래 JSON을 구성해서 전송할 수 있음. (지금은 생략)
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes("{}");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // 2. 요청 전송
        yield return request.SendWebRequest();

        // 3. 에러 체크
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[Leaderboard] 조회 실패: " + request.error);
            if (leaderboardText != null)
            {
                leaderboardText.text = "리더보드 조회 실패\n" + request.error;
            }
            yield break;
        }

        // 4. 성공 시 응답(JSON)을 문자열로 받음
        string jsonResponse = request.downloadHandler.text;
        Debug.Log("[Leaderboard] 조회 성공. 응답: " + jsonResponse);

        // 5. JSON → C# 리스트로 역직렬화
        //    Unity 2022+라면 UnityEngine.JsonUtility 사용 가능하지만,
        //    List 역직렬화가 번거로울 수 있어, Newtonsoft.Json을 사용 권장
        List<LeaderboardEntryData> leaderboardList;
        try
        {
            leaderboardList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<LeaderboardEntryData>>(jsonResponse);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Leaderboard] JSON 파싱 오류: " + e.Message);
            if (leaderboardText != null)
            {
                leaderboardText.text = "JSON 파싱 오류";
            }
            yield break;
        }

        // 6. UI에 표시
        if (leaderboardText != null)
        {
            if (leaderboardList == null || leaderboardList.Count == 0)
            {
                leaderboardText.text = "리더보드 데이터가 없습니다.";
            }
            else
            {
                leaderboardText.text = "=== 리더보드 (LoginCount 기준) ===\n";
                foreach (var entry in leaderboardList)
                {
                    string nameToShow = !string.IsNullOrEmpty(entry.DisplayName) ? entry.DisplayName : "NoName";
                    leaderboardText.text += $"{entry.Rank}등: {nameToShow} (PFID: {entry.PlayFabId}) - {entry.StatValue}\n";
                }
            }
        }
    }
}
