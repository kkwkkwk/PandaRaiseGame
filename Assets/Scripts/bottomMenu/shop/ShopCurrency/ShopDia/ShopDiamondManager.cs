using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;                // ScrollRect, LayoutRebuilder
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ShopDiamondManager : MonoBehaviour
{
    public static ShopDiamondManager Instance { get; private set; }

    [Header("다이아 탭 팝업 Canvas")]
    public GameObject diamondPopupCanvas;

    [Header("기본 컨텐츠 (헤더 미발견 시)")]
    public Transform defaultContentParent;

    [System.Serializable]
    public class HeaderConfig { public string headerName; public Transform contentParent; }
    [Header("Header → Content Parent 매핑")]
    public HeaderConfig[] headerConfigs;

    [Header("단일 아이템 Prefab")]
    public GameObject itemPrefab;

    private const string fetchDiamondUrl =
        "https://pandaraisegame-shop.azurewebsites.net/api/GetDiamondShopData?code=NFo5vc5QTYyNUxJBokgDiMDi5F_NgRP6JuvKWpz4R6RBAzFuKOBC_A==";
    private const string purchaseDiamondUrl =
        "https://pandaraisegame-shop.azurewebsites.net/api/BuyDiamondShopItem";

    private List<DiamondItemData> diamondItems;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OpenDiamondPanel()
    {
        diamondPopupCanvas?.SetActive(true);
        StartCoroutine(FetchDiamondDataFromServer());
    }

    public void CloseDiamondPanel()
    {
        diamondPopupCanvas?.SetActive(false);
    }

    private IEnumerator FetchDiamondDataFromServer()
    {
        var body = System.Text.Encoding.UTF8.GetBytes("{}");
        using var req = new UnityWebRequest(fetchDiamondUrl, "POST")
        {
            uploadHandler = new UploadHandlerRaw(body),
            downloadHandler = new DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[DiamondManager] Fetch 실패: {req.error}");
            yield break;
        }

        var resp = JsonConvert.DeserializeObject<DiamondResponseData>(req.downloadHandler.text);
        if (resp == null || !resp.IsSuccess)
        {
            Debug.LogWarning("[DiamondManager] 서버 응답 이상");
            yield break;
        }

        LoadData(resp.DiamondItemList);
    }

    public void LoadData(List<DiamondItemData> items)
    {
        diamondItems = items;
        PopulateDiamondItems();
    }

    private void PopulateDiamondItems()
    {
        foreach (var hc in headerConfigs) ClearContent(hc.contentParent);
        ClearContent(defaultContentParent);

        if (diamondItems == null || diamondItems.Count == 0) return;

        foreach (var item in diamondItems)
        {
            var parent = GetContentParent(item.Header) ?? defaultContentParent;
            if (parent == null || itemPrefab == null) continue;

            var go = Instantiate(itemPrefab, parent);
            var ctrl = go.GetComponent<SmallItemController>();
            ctrl?.Setup(item.ItemName, null, item.Price, item.CurrencyType, item.ItemName, "Diamond");
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

    public void PurchaseDiamond(string itemName, string currencyType)
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
        var req = new UnityWebRequest(purchaseDiamondUrl, "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonConvert.DeserializeObject<BuyCurrencyResponseData>(req.downloadHandler.text);
            if (resp != null && resp.IsSuccess)
                Debug.Log("[DiamondManager] 재화 구매 성공");
            else
                Debug.LogWarning("[DiamondManager] 서버 처리 실패 (isSuccess == false)");
        }
        else
        {
            Debug.LogError($"[DiamondManager] 요청 실패: {req.error}");
        }
    }
}
