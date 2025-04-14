using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class GoldGrowthPanelListCreator : MonoBehaviour
{
    private string getGoldGrowthURL = "https://pandaraisegame-growth.azurewebsites.net/api/GetGoldGrowthData?code=GXgBu_JBlSCXFBa_VoMde1-abBC5TLbA96hE8vdvNQyTAzFuhdAH4Q==";
    private string postGoldGrowthChangeURL = "https://pandaraisegame-growth.azurewebsites.net/api/PostGoldGrowthChange?code=Zap-_-wFxh3Tv-7Gqjxlzob12njD9uWs3UN1e5HVKCt9AzFuTve2Lw==";

    [Header("UI References")]
    public Transform contentParent;            // ScrollView의 Content
    public GameObject growthGoldPrefab;        // GrowthGold_Prefab
    public TextMeshProUGUI playerGoldText;     // 상단에 플레이어 골드를 표시

    // 서버에서 받은 데이터
    private GoldGrowthResponse currentData;
    // 생성된 아이템을 보관
    private List<GrowthGoldItemController> itemControllers = new List<GrowthGoldItemController>();

    private void OnEnable()
    {
        // 탭이 활성화될 때마다 서버에서 최신 데이터 받아오기
        StartCoroutine(GetGoldGrowthDataFromServer());
    }

    /// <summary>
    /// 1) 서버로부터 골드 강화 정보를 조회
    /// </summary>
    private IEnumerator GetGoldGrowthDataFromServer()
    {
        var requestObj = new
        {
            playFabId = GlobalData.playFabId
        };
        string jsonString = JsonConvert.SerializeObject(requestObj);

        using (UnityWebRequest request = new UnityWebRequest(getGoldGrowthURL, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonString);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[GoldGrowth] 데이터 조회 실패: " + request.error);
                yield break;
            }

            string responseJson = request.downloadHandler.text;
            Debug.Log("[GoldGrowth] 서버 응답: " + responseJson);

            GoldGrowthResponse response = null;
            try
            {
                response = JsonConvert.DeserializeObject<GoldGrowthResponse>(responseJson);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[GoldGrowth] JSON 파싱 오류: " + ex.Message);
            }

            if (response == null || !response.IsSuccess)
            {
                Debug.LogWarning("[GoldGrowth] 서버 응답 실패: " + (response?.ErrorMessage ?? "NULL"));
                yield break;
            }

            // 성공적으로 받아온 데이터로 UI 갱신
            currentData = response;
            RebuildUI(currentData);
        }
    }

    /// <summary>
    /// 2) “골드 강화” 버튼을 누를 때 서버에 요청 → 최신 데이터로 UI 갱신
    /// </summary>
    public void RequestGoldEnhance(string abilityName)
    {
        StartCoroutine(PostGoldEnhanceCoroutine(abilityName));
    }

    private IEnumerator PostGoldEnhanceCoroutine(string abilityName)
    {
        // body: { "playFabId": "...", "abilityName": "공격력" }
        var requestDTO = new GoldGrowthChangeRequest
        {
            PlayFabId = GlobalData.playFabId,
            AbilityName = abilityName
        };
        string jsonString = JsonConvert.SerializeObject(requestDTO);

        using (UnityWebRequest request = new UnityWebRequest(postGoldGrowthChangeURL, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonString);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[GoldGrowth] 골드 강화 요청 실패: " + request.error);
                yield break;
            }

            string responseJson = request.downloadHandler.text;
            Debug.Log("[GoldGrowth] 골드 강화 응답: " + responseJson);

            GoldGrowthResponse response = null;
            try
            {
                response = JsonConvert.DeserializeObject<GoldGrowthResponse>(responseJson);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[GoldGrowth] JSON 파싱 오류(강화 응답): " + ex.Message);
            }

            if (response == null || !response.IsSuccess)
            {
                Debug.LogWarning("[GoldGrowth] 골드 강화 실패: " + (response?.ErrorMessage ?? "NULL"));
                yield break;
            }

            // 서버가 갱신된 최신 데이터를 내려줌 -> 다시 UI 갱신
            currentData = response;
            RebuildUI(currentData);
        }
    }

    /// <summary>
    /// 기존 UI(프리팹) 정리 후 새로운 데이터로 재구성
    /// </summary>
    private void RebuildUI(GoldGrowthResponse data)
    {
        // 상단 골드 표시
        if (playerGoldText != null)
        {
            string formattedGold = NumberFormatter.FormatBigNumber(data.PlayerGold);
            playerGoldText.text = $"보유 골드: {formattedGold}";
        }

        // 기존 아이템 제거
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }
        itemControllers.Clear();

        // goldGrowthList에 따라 Prefab 생성
        if (data.GoldGrowthList == null || data.GoldGrowthList.Count == 0)
        {
            Debug.LogWarning("[GoldGrowth] GoldGrowthList가 비어있음.");
            return;
        }

        foreach (var itemData in data.GoldGrowthList)
        {
            GameObject itemObj = Instantiate(growthGoldPrefab, contentParent);
            var controller = itemObj.GetComponent<GrowthGoldItemController>();
            if (controller != null)
            {
                controller.Initialize(itemData, this);
                itemControllers.Add(controller);
            }
        }
    }
}
