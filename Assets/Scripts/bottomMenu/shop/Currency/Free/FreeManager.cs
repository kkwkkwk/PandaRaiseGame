using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;                // ScrollRect, LayoutRebuilder
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class FreeManager : MonoBehaviour
{
    public static FreeManager Instance { get; private set; }

    [Header("무료(Free) 탭 팝업 Canvas")]
    public GameObject freePopupCanvas;

    [Header("기본 컨텐츠 (헤더 미발견 시)")]
    public Transform defaultContentParent;

    [System.Serializable]
    public class HeaderConfig { public string headerName; public Transform contentParent; }
    [Header("Header → Content Parent 매핑")]
    public HeaderConfig[] headerConfigs;

    [Header("단일 아이템 Prefab")]
    public GameObject itemPrefab;

    private const string fetchFreeUrl =
        "https://pandaraisegame-shop.azurewebsites.net/api/GetFreeShopData?code=RSjJW7s3xyKlU3iJzYog0Fd50ylVsuIp-PdqRpG807e4AzFu0MBECg==";
    private const string purchaseFreeUrl =
        "https://pandaraisegame-shop.azurewebsites.net/api/BuyFreeShopItem";

    private List<FreeItemData> freeItems;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OpenFreePanel()
    {
        freePopupCanvas?.SetActive(true);
        StartCoroutine(FetchFreeDataFromServer());
    }

    public void CloseFreePanel()
    {
        freePopupCanvas?.SetActive(false);
    }

    private IEnumerator FetchFreeDataFromServer()
    {
        var body = System.Text.Encoding.UTF8.GetBytes("{}");
        using var req = new UnityWebRequest(fetchFreeUrl, "POST")
        {
            uploadHandler = new UploadHandlerRaw(body),
            downloadHandler = new DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[FreeManager] Fetch 실패: {req.error}");
            yield break;
        }

        var resp = JsonConvert.DeserializeObject<FreeResponseData>(req.downloadHandler.text);
        if (resp == null || !resp.IsSuccess)
        {
            Debug.LogWarning("[FreeManager] 서버 응답 이상");
            yield break;
        }

        LoadData(resp.FreeItemList);
    }

    public void LoadData(List<FreeItemData> items)
    {
        freeItems = items;
        PopulateFreeItems();
    }

    private void PopulateFreeItems()
    {
        foreach (var hc in headerConfigs) ClearContent(hc.contentParent);
        ClearContent(defaultContentParent);

        if (freeItems == null || freeItems.Count == 0) return;

        foreach (var item in freeItems)
        {
            var parent = GetContentParent(item.Header) ?? defaultContentParent;
            if (parent == null || itemPrefab == null) continue;

            var go = Instantiate(itemPrefab, parent);
            var ctrl = go.GetComponent<SmallItemController>();
            ctrl?.Setup(item.ItemName, null, item.Price, item.CurrencyType, item.ItemName, "Free");
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

    public void PurchaseFree(string itemName, string currencyType)
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
        var req = new UnityWebRequest(purchaseFreeUrl, "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonConvert.DeserializeObject<BuyCurrencyResponseData>(req.downloadHandler.text);
            if (resp != null && resp.IsSuccess)
                Debug.Log("[FreeManager] 무료 아이템 처리 성공");
            else
                Debug.LogWarning("[FreeManager] 서버 처리 실패 (isSuccess == false)");
        }
        else
        {
            Debug.LogError($"[FreeManager] 요청 실패: {req.error}");
        }
    }
}
