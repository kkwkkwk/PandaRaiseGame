using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class SpecialManager : MonoBehaviour
{
    public static SpecialManager Instance { get; private set; }

    [Header("스페셜 패널 (옵션)")]
    public GameObject specialPanel;

    [Header("아이템 배치용 Content Parent")]
    public Transform contentParent;

    [Header("단일 아이템 Prefab")]
    public GameObject itemPrefab;

    private const string fetchSpecialUrl = "https://pandaraisegame-shop.azurewebsites.net/api/GetSpecialData?code=YOUR_CODE";
    private const string purchaseSpecialUrl = "https://pandaraisegame-shop.azurewebsites.net/api/BuySpecialItem";

    private List<SpecialItemData> specialItems;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OpenSpecialPanel()
    {
        specialPanel?.SetActive(true);
        ClearContent();
        StartCoroutine(FetchSpecialDataCoroutine());
    }

    public void CloseSpecialPanel()
    {
        specialPanel?.SetActive(false);
    }

    private IEnumerator FetchSpecialDataCoroutine()
    {
        byte[] body = System.Text.Encoding.UTF8.GetBytes("{}");
        using var req = new UnityWebRequest(fetchSpecialUrl, "POST")
        {
            uploadHandler = new UploadHandlerRaw(body),
            downloadHandler = new DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[SpecialManager] 데이터 조회 실패: {req.error}");
            yield break;
        }

        SpecialResponseWrapper wrapper = null;
        try
        {
            wrapper = JsonConvert.DeserializeObject<SpecialResponseWrapper>(req.downloadHandler.text);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SpecialManager] JSON 파싱 오류: {ex.Message}");
            yield break;
        }

        if (wrapper == null || !wrapper.IsSuccess)
        {
            Debug.LogWarning("[SpecialManager] 유효하지 않은 응답 또는 isSuccess=false");
            yield break;
        }

        specialItems = wrapper.SpecialItemList;
        PopulateSpecialItems();
    }

    private void PopulateSpecialItems()
    {
        if (specialItems == null || itemPrefab == null || contentParent == null)
            return;

        foreach (var item in specialItems)
        {
            var go = Instantiate(itemPrefab, contentParent);
            var ctrl = go.GetComponent<SmallItemController>();
            if (ctrl != null)
            {
                // 4번째 인자로 "WON"을 넘겨서 Setup; 마지막 인자로 "Special"
                ctrl.Setup(item.Title, null, item.Price, "WON", item.Title, "Special");
            }
        }
    }

    private void ClearContent()
    {
        if (contentParent == null) return;
        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);
    }

    // currencyType 기본값을 "WON"으로 주었습니다.
    public void PurchaseSpecial(string itemTitle, string currencyType = "WON")
    {
        var requestData = new BuySpecialRequestData
        {
            PlayFabId = PlayerPrefs.GetString("PlayFabId"),
            ItemTitle = itemTitle
        };
        StartCoroutine(SendBuySpecialRequest(requestData));
    }

    private IEnumerator SendBuySpecialRequest(BuySpecialRequestData data)
    {
        string json = JsonConvert.SerializeObject(data);
        using var req = new UnityWebRequest(purchaseSpecialUrl, "POST")
        {
            uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[SpecialManager] 구매 요청 실패: {req.error}");
            yield break;
        }

        BuySpecialResponseData resp = null;
        try
        {
            resp = JsonConvert.DeserializeObject<BuySpecialResponseData>(req.downloadHandler.text);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SpecialManager] 구매 응답 파싱 오류: {ex.Message}");
            yield break;
        }

        if (resp != null && resp.IsSuccess)
            Debug.Log("[SpecialManager] 스페셜 구매 성공");
        else
            Debug.LogWarning($"[SpecialManager] 구매 실패: {resp?.Message}");
    }

    private class SpecialResponseWrapper
    {
        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("specialItemList")]
        public List<SpecialItemData> SpecialItemList { get; set; }
    }
}
