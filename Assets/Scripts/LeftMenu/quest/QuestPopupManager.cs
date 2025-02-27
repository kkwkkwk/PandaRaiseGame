using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;
using System;

[System.Serializable]
public class QuestDataResponse
{
    [JsonProperty("QuestData")]
    public string QuestData { get; set; }
}
[Serializable]
public class QuestData
{
    [JsonProperty("id")]
    public string id { get; set; }

    [JsonProperty("title")]
    public string title { get; set; }

    [JsonProperty("description")]
    public string description { get; set; }

    [JsonProperty("goal")]
    public int goal { get; set; }

    [JsonProperty("resetType")]
    public string resetType { get; set; }
}


public class QuestPopupManager : MonoBehaviour
{
    public static QuestPopupManager Instance { get; private set; }

    public GameObject QuestCanvas;         // 퀘스트 팝업 Canvas
    public QuestTabPanelListCreator questListCreator; // 씬 내 QuestTabPanelListCreator 참조
    public TextMeshProUGUI questPopupText;   // 테스트용: 응답 문자열 출력

    private string getQuestInfoUrl = "https://pandaraisegame-quest.azurewebsites.net/api/GetQuestInfo?code=wcLCEUa2BEKrTQQNM_Rv106yU1CzZVAbiZJLle86yLsZAzFuDmLu1Q==";

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (QuestCanvas != null)
            QuestCanvas.SetActive(false);
    }

    public void OpenQuestPopup()
    {
        if (QuestCanvas != null)
        {
            QuestCanvas.SetActive(true);
            Debug.Log("퀘스트 팝업 열기");
            StartCoroutine(CallGetQuestData());
        }
    }

    public void CloseQuestPopup()
    {
        if (QuestCanvas != null)
        {
            QuestCanvas.SetActive(false);
            Debug.Log("퀘스트 팝업 닫기");
        }
    }

    /// <summary>
    /// Azure Function (GetQuestInfo) 호출하여 퀘스트 데이터를 가져오는 코루틴 (단순 문자열 반환 방식)
    /// </summary>
    private IEnumerator CallGetQuestData()
    {
        Debug.Log("[QuestPopupManager] 퀘스트 데이터 조회 시작...");

        using (UnityWebRequest request = new UnityWebRequest(getQuestInfoUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes("{}");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[QuestPopupManager] 조회 실패: " + request.error);
                if (questPopupText != null)
                    questPopupText.text = "퀘스트 데이터 조회 실패\n" + request.error;
                yield break;
            }

            string jsonResponse = request.downloadHandler.text;
            Debug.Log("[QuestPopupManager] 조회 성공. 응답: " + jsonResponse);

            QuestDataResponse response = null;
            try
            {
                response = JsonConvert.DeserializeObject<QuestDataResponse>(jsonResponse);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[QuestPopupManager] JSON 파싱 오류: " + ex.Message);
                if (questPopupText != null)
                    questPopupText.text = "퀘스트 JSON 파싱 오류";
                yield break;
            }

            if (response == null || string.IsNullOrEmpty(response.QuestData))
            {
                Debug.LogWarning("[QuestPopupManager] 퀘스트 데이터가 없습니다.");
                if (questPopupText != null)
                    questPopupText.text = "퀘스트 데이터가 없습니다.";
            }
            else
            {
                // (옵션) 전체 응답 문자열을 출력 (디버깅 용)
                if (questPopupText != null)
                {
                    questPopupText.text = response.QuestData;
                }
                // QuestTabPanelListCreator에 데이터 전달 (문자열 그대로 전달)
                if (questListCreator != null)
                {
                    questListCreator.SetQuestDataFromString(response.QuestData);
                }
                else
                {
                    Debug.LogWarning("QuestTabPanelListCreator 참조가 설정되지 않았습니다.");
                }
            }
        }
    }
}
