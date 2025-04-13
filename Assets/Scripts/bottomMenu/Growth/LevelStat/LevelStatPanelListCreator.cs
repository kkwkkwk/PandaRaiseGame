using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class LevelStatPanelListCreator : MonoBehaviour
{
    [Header("URL")]
    // 1) 전체 스탯 조회 URL
    [SerializeField] private string getLevelStatURL = "https://example.com/api/GetLevelStatData";
    // 2) 능력치 변경(증가/초기화) URL
    [SerializeField] private string postLevelStatChangeURL = "https://example.com/api/PostLevelStatChange";

    [Header("UI References")]
    public Transform contentParent;          // ScrollView의 Content
    public GameObject growthLevelStatPrefab; // GrowthLevelStat_Prefab
    public TextMeshProUGUI remainingStatText; // 상단에 남은 스탯 포인트 표시

    // 현재 서버에서 받은 데이터
    private LevelStatResponse currentStatData;
    // 동적으로 생성된 아이템 컨트롤러들을 추적
    private List<GrowthLevelStatItemController> itemControllers = new List<GrowthLevelStatItemController>();

    private void OnEnable()
    {
        // 탭 열릴 때 서버에서 최신 데이터 받아오기
        StartCoroutine(GetLevelStatDataFromServer());
    }

    /// <summary>
    /// 서버에서 전체 레벨 스탯 데이터를 받아와 UI를 구성
    /// </summary>
    private IEnumerator GetLevelStatDataFromServer()
    {
        // (퀘스트/길드 코드처럼) playFabId, entityToken 등 필요 정보
        var requestObject = new
        {
            playFabId = GlobalData.playFabId,
        };

        string jsonString = JsonConvert.SerializeObject(requestObject);

        using (UnityWebRequest request = new UnityWebRequest(getLevelStatURL, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonString);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[LevelStat] 서버 통신 실패: " + request.error);
                yield break;
            }

            string responseJson = request.downloadHandler.text;
            Debug.Log("[LevelStat] 서버 응답: " + responseJson);

            LevelStatResponse response = null;
            try
            {
                response = JsonConvert.DeserializeObject<LevelStatResponse>(responseJson);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[LevelStat] JSON 파싱 오류: " + ex.Message);
            }

            if (response == null || !response.IsSuccess)
            {
                // 서버에서 isSuccess=false or 응답 자체가 null일 경우
                Debug.LogWarning("[LevelStat] 서버 응답이 실패 상태입니다: "
                                  + (response?.ErrorMessage ?? "NULL"));
                yield break;
            }

            // 받아온 데이터로 UI 갱신
            currentStatData = response;
            RebuildStatUI(currentStatData);
        }
    }

    /// <summary>
    /// 능력치 버튼 클릭 시 서버에 '증가' 요청
    /// </summary>
    public void IncreaseStat(string abilityName)
    {
        StartCoroutine(PostLevelStatChangeCoroutine("increase", abilityName));
    }

    /// <summary>
    /// 능력치 버튼 클릭 시 서버에 '초기화' 요청
    /// </summary>
    public void ResetStat(string abilityName)
    {
        StartCoroutine(PostLevelStatChangeCoroutine("reset", abilityName));
    }

    /// <summary>
    /// 서버에 능력치 변경 요청('증가' or '초기화') → 응답 받아 UI 재구성
    /// </summary>
    private IEnumerator PostLevelStatChangeCoroutine(string action, string abilityName)
    {
        var requestDTO = new LevelStatChangeRequest
        {
            PlayFabId = GlobalData.playFabId,
            Action = action,
            AbilityName = abilityName
        };

        string jsonString = JsonConvert.SerializeObject(requestDTO);

        using (UnityWebRequest request = new UnityWebRequest(postLevelStatChangeURL, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonString);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[LevelStat] 능력치({action}) 서버 요청 실패: " + request.error);
                yield break;
            }

            string responseJson = request.downloadHandler.text;
            Debug.Log($"[LevelStat] 능력치({action}) 응답: " + responseJson);

            LevelStatResponse response = null;
            try
            {
                response = JsonConvert.DeserializeObject<LevelStatResponse>(responseJson);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LevelStat] {action} JSON 파싱 오류: " + ex.Message);
            }

            if (response == null || !response.IsSuccess)
            {
                Debug.LogWarning($"[LevelStat] 서버 응답 실패: {(response?.ErrorMessage ?? "NULL")}");
                yield break;
            }

            // 서버가 갱신 후의 최신 데이터를 내려줬으므로, UI 갱신
            currentStatData = response;
            RebuildStatUI(currentStatData);
        }
    }

    /// <summary>
    /// ScrollView의 기존 항목을 정리하고, 새 데이터로 Prefab 목록을 재구성
    /// </summary>
    private void RebuildStatUI(LevelStatResponse data)
    {
        // 1) 상단 표시 (잔여 스탯)
        if (remainingStatText != null)
        {
            remainingStatText.text = $"남은 스탯: {data.RemainingStatPoints}";
        }

        // 2) 기존 리스트 삭제
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }
        itemControllers.Clear();

        // 3) abilityStatList를 순회하며 Prefab 생성
        if (data.AbilityStatList == null || data.AbilityStatList.Count == 0)
        {
            Debug.LogWarning("[LevelStat] abilityStatList가 비어 있음");
            return;
        }

        foreach (var stat in data.AbilityStatList)
        {
            GameObject itemObj = Instantiate(growthLevelStatPrefab, contentParent);
            var controller = itemObj.GetComponent<GrowthLevelStatItemController>();
            if (controller != null)
            {
                controller.Initialize(stat, this);
                itemControllers.Add(controller);
            }
            else
            {
                Debug.LogWarning("[LevelStat] GrowthLevelStatItemController가 프리팹에 없음!");
            }
        }
    }
}
