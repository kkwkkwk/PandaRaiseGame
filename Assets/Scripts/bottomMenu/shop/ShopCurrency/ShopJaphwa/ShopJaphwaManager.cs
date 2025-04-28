using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;                // ScrollRect, LayoutRebuilder
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ShopJaphwaManager : MonoBehaviour
{
    public static ShopJaphwaManager Instance { get; private set; }

    [Header("잡화 탭 팝업 Canvas")]
    public GameObject japhaPopupCanvas;

    [Header("ScrollView Content (Viewport→Content)")]
    public RectTransform scrollContent;

    [Header("기본 컨텐츠 (헤더 미발견 시)")]
    public Transform defaultContentParent;

    [System.Serializable]
    public class HeaderConfig { public string headerName; public Transform contentParent; }
    [Header("Header → Content Parent 매핑")]
    public HeaderConfig[] headerConfigs;

    [Header("단일 아이템 Prefab")]
    public GameObject itemPrefab;

    private const string fetchJaphaUrl =
        "https://pandaraisegame-shop.azurewebsites.net/api/GetJaphwaShopData?code=Qjq_KGQpLvoZjJKDCR76iOJE9EjQHoO2PvucK7Ea92-EAzFu8w6Mtg==";
    private const string purchaseJaphaUrl =
        "https://pandaraisegame-shop.azurewebsites.net/api/BuyCurrencyItem?code=LXn8o8WNXOWZhs85YUY8XgN4xyX87Oj8vrskr-3aDmMVAzFuRI30Fg==";

    private List<JaphwaItemData> japhaItems;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OpenJaphaPanel()
    {
        japhaPopupCanvas?.SetActive(true);
        scrollContent.gameObject.SetActive(false);
        StartCoroutine(FetchAndShow());
    }

    public void CloseJaphaPanel()
    {
        japhaPopupCanvas?.SetActive(false);
    }

    private IEnumerator FetchAndShow()
    {
        yield return FetchJaphaDataCoroutine();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);
        var sr = scrollContent.GetComponentInParent<ScrollRect>();
        if (sr != null) sr.verticalNormalizedPosition = 1f;
        scrollContent.gameObject.SetActive(true);
    }

    private IEnumerator FetchJaphaDataCoroutine()
    {
        var body = System.Text.Encoding.UTF8.GetBytes("{}");
        using var req = new UnityWebRequest(fetchJaphaUrl, "POST")
        {
            uploadHandler = new UploadHandlerRaw(body),
            downloadHandler = new DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[JaphaManager] Fetch 실패: {req.error}");
            yield break;
        }

        var resp = JsonConvert.DeserializeObject<JaphwaResponseData>(req.downloadHandler.text);
        if (resp == null || !resp.IsSuccess)
        {
            Debug.LogWarning("[JaphaManager] 서버 응답 이상");
            yield break;
        }

        japhaItems = resp.JaphaItemList;
        PopulateJaphaItems();
    }

    private void PopulateJaphaItems()
    {
        foreach (var hc in headerConfigs) ClearContent(hc.contentParent);
        ClearContent(defaultContentParent);

        if (japhaItems == null || japhaItems.Count == 0) return;

        foreach (var item in japhaItems)
        {
            var parent = GetContentParent(item.Header) ?? defaultContentParent;
            if (parent == null || itemPrefab == null) continue;

            var go = Instantiate(itemPrefab, parent);
            var ctrl = go.GetComponent<SmallItemController>();
            ctrl?.Setup(item.ItemName, null, item.Price, item.CurrencyType, item.ItemName, "Japha");
        }
    }

    private Transform GetContentParent(string header)
    {
        if (string.IsNullOrEmpty(header)) return null;
        foreach (var hc in headerConfigs)
            if (header.Contains(hc.headerName))
                return hc.contentParent;
        return null;
    }

    private void ClearContent(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    public void PurchaseJapha(string itemName, string currencyType)
    {
        var requestData = new BuyCurrencyRequestData
        {
            PlayFabId = PlayerPrefs.GetString("PlayFabId"),
            ItemName = itemName,
            CurrencyType = currencyType,
            ItemType = "Currency"
        };
        StartCoroutine(SendBuyCurrencyRequest(requestData));
    }

    private IEnumerator SendBuyCurrencyRequest(BuyCurrencyRequestData data)
    {
        string json = JsonConvert.SerializeObject(data);
        var req = new UnityWebRequest(purchaseJaphaUrl, "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonConvert.DeserializeObject<BuyCurrencyResponseData>(req.downloadHandler.text);
            if (resp != null && resp.IsSuccess)
                Debug.Log("[JaphaManager] 재화 구매 성공");
            else
                Debug.LogWarning("[JaphaManager] 서버 처리 실패 (isSuccess == false)");
        }
        else
        {
            Debug.LogError($"[JaphaManager] 요청 실패: {req.error}");
        }
    }
}