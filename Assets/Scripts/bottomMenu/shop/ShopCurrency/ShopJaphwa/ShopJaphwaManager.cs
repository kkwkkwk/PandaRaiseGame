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
    public GameObject japhwaPopupCanvas;

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

    private const string fetchJaphwaUrl =
        "https://pandaraisegame-shop.azurewebsites.net/api/GetJaphwaShopData?code=Qjq_KGQpLvoZjJKDCR76iOJE9EjQHoO2PvucK7Ea92-EAzFu8w6Mtg==";
    private const string purchaseJaphwaUrl =
        "https://pandaraisegame-shop.azurewebsites.net/api/BuyCurrencyItem?code=LXn8o8WNXOWZhs85YUY8XgN4xyX87Oj8vrskr-3aDmMVAzFuRI30Fg==";

    private List<JaphwaItemData> japhwaItems;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OpenJaphwaPanel()
    {
        japhwaPopupCanvas?.SetActive(true);
        scrollContent.gameObject.SetActive(false);
        StartCoroutine(FetchAndShow());
    }

    public void CloseJaphwaPanel()
    {
        japhwaPopupCanvas?.SetActive(false);
    }

    private IEnumerator FetchAndShow()
    {
        yield return FetchJaphwaDataCoroutine();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);
        var sr = scrollContent.GetComponentInParent<ScrollRect>();
        if (sr != null) sr.verticalNormalizedPosition = 1f;
        scrollContent.gameObject.SetActive(true);
    }

    private IEnumerator FetchJaphwaDataCoroutine()
    {
        var body = System.Text.Encoding.UTF8.GetBytes("{}");
        using var req = new UnityWebRequest(fetchJaphwaUrl, "POST")
        {
            uploadHandler = new UploadHandlerRaw(body),
            downloadHandler = new DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[JaphwaManager] Fetch 실패: {req.error}");
            yield break;
        }

        var resp = JsonConvert.DeserializeObject<JaphwaResponseData>(req.downloadHandler.text);
        if (resp == null || !resp.IsSuccess)
        {
            Debug.LogWarning("[JaphwaManager] 서버 응답 이상");
            yield break;
        }

        japhwaItems = resp.JaphwaItemList;
        PopulateJaphwaItems();
    }

    private void PopulateJaphwaItems()
    {
        foreach (var hc in headerConfigs) ClearContent(hc.contentParent);
        ClearContent(defaultContentParent);

        if (japhwaItems == null || japhwaItems.Count == 0) return;

        foreach (var item in japhwaItems)
        {
            var parent = GetContentParent(item.Header) ?? defaultContentParent;
            if (parent == null || itemPrefab == null) continue;

            var go = Instantiate(itemPrefab, parent);
            var ctrl = go.GetComponent<SmallItemController>();
            ctrl?.Setup(item.ItemName, null, item.Price, item.CurrencyType, item.ItemName, "Japhwa");
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

    public void PurchaseJaphwa(string itemName, string currencyType)
    {
        // 1) 선택된 잡화 아이템 찾기
        var itemData = japhwaItems.Find(i => i.ItemName == itemName);
        if (itemData == null)
        {
            Debug.LogError($"[JaphwaManager] Purchase 요청 실패 – '{itemName}' 항목 미존재");
            return;
        }

        var requestData = new BuyCurrencyRequestData
        {
            PlayFabId = GlobalData.playFabId,
            CurrencyType = currencyType,
            GoodsType = itemData.GoodsType,   
            ItemType = "Japhwa",
            JaphwaItemData = itemData
        };

        StartCoroutine(SendBuyCurrencyRequest(requestData));
    }


    // ShopJaphwaManager.cs
    private IEnumerator SendBuyCurrencyRequest(BuyCurrencyRequestData data)
    {
        // ▶ Purchase JSON
        string json = JsonConvert.SerializeObject(data);
        Debug.Log($"[JaphwaManager] ▶ Purchase JSON\n{json}");

        // 요청 전송
        var req = new UnityWebRequest(purchaseJaphwaUrl, "POST")
        {
            uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        // HTTP 실패
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[JaphwaManager] 요청 실패: {req.error}");
            yield break;
        }

        // ◀ Response JSON
        Debug.Log($"[JaphwaManager] ◀ Response JSON\n{req.downloadHandler.text}");

        // 성공/실패 로직 (message 없이)
        var resp = JsonConvert.DeserializeObject<BuyCurrencyResponseData>(req.downloadHandler.text);
        if (resp != null && resp.IsSuccess)
        {
            Debug.Log($"[JaphwaManager] 구매 성공! {data.GoodsType}");
        }
        else
        {
            Debug.LogWarning($"[JaphwaManager] 서버 처리 실패 (isSuccess == false)");
        }
    }
    public IEnumerator StartSequence()
    {
        yield return FetchJaphwaDataCoroutine();
    }


}