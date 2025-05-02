using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;                // ScrollRect, LayoutRebuilder
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ShopMileageManager : MonoBehaviour
{
    public static ShopMileageManager Instance { get; private set; }

    [Header("마일리지 탭 팝업 Canvas")]
    public GameObject mileagePopupCanvas;

    [Header("기본 컨텐츠 (헤더 미발견 시)")]
    public Transform defaultContentParent;

    [System.Serializable]
    public class HeaderConfig { public string headerName; public Transform contentParent; }
    [Header("Header → Content Parent 매핑")]
    public HeaderConfig[] headerConfigs;

    [Header("단일 아이템 Prefab")]
    public GameObject itemPrefab;

    private const string fetchMileageUrl =
        "https://pandaraisegame-shop.azurewebsites.net/api/GetMileageShopData?code=7FEp57GIrRLmJG0-E3k5IuuksDTzUgpcSkJiNRzVM3H2AzFuWLTgiw==";
    private const string purchaseMileageUrl =
        "https://pandaraisegame-shop.azurewebsites.net/api/BuyCurrencyItem?code=LXn8o8WNXOWZhs85YUY8XgN4xyX87Oj8vrskr-3aDmMVAzFuRI30Fg==";

    private List<MileageItemData> mileageItems;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OpenMileagePanel()
    {
        mileagePopupCanvas?.SetActive(true);
        StartCoroutine(FetchMileageDataFromServer());
    }

    public void CloseMileagePanel()
    {
        mileagePopupCanvas?.SetActive(false);
    }

    private IEnumerator FetchMileageDataFromServer()
    {
        var body = System.Text.Encoding.UTF8.GetBytes("{}");
        using var req = new UnityWebRequest(fetchMileageUrl, "POST")
        {
            uploadHandler = new UploadHandlerRaw(body),
            downloadHandler = new DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[MileageManager] Fetch 실패: {req.error}");
            yield break;
        }

        var resp = JsonConvert.DeserializeObject<MileageResponseData>(req.downloadHandler.text);
        if (resp == null || !resp.IsSuccess)
        {
            Debug.LogWarning("[MileageManager] 서버 응답 이상");
            yield break;
        }

        LoadData(resp.MileageItemList);
    }

    public void LoadData(List<MileageItemData> items)
    {
        mileageItems = items;
        PopulateMileageItems();
    }

    private void PopulateMileageItems()
    {
        foreach (var hc in headerConfigs) ClearContent(hc.contentParent);
        ClearContent(defaultContentParent);

        if (mileageItems == null || mileageItems.Count == 0) return;

        foreach (var item in mileageItems)
        {
            var parent = GetContentParent(item.Header) ?? defaultContentParent;
            if (parent == null || itemPrefab == null) continue;

            var go = Instantiate(itemPrefab, parent);
            var ctrl = go.GetComponent<SmallItemController>();
            ctrl?.Setup(item.ItemName, null, item.Price, item.CurrencyType, item.ItemName, "Mileage");
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
    public void PurchaseMileage(string itemName, string currencyType)
    {
        // 1) 선택된 마일리지 아이템 찾기
        var itemData = mileageItems.Find(i => i.ItemName == itemName);
        if (itemData == null)
        {
            Debug.LogError($"[MileageManager] Purchase 요청 실패 – '{itemName}' 항목 미존재");
            return;
        }

        var requestData = new BuyCurrencyRequestData
        {
            PlayFabId = GlobalData.playFabId,
            CurrencyType = currencyType,
            GoodsType = itemData.GoodsType,  
            ItemType = "Mileage",
            MileageItemData = itemData
        };
        StartCoroutine(SendBuyCurrencyRequest(requestData));
    }


    // ShopMileageManager.cs
    private IEnumerator SendBuyCurrencyRequest(BuyCurrencyRequestData data)
    {
        // ▶ Purchase JSON
        string json = JsonConvert.SerializeObject(data);
        Debug.Log($"[MileageManager] ▶ Purchase JSON\n{json}");

        // 요청 전송
        var req = new UnityWebRequest(purchaseMileageUrl, "POST")
        {
            uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        // HTTP 실패
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[MileageManager] 요청 실패: {req.error}");
            yield break;
        }

        // ◀ Response JSON
        Debug.Log($"[MileageManager] ◀ Response JSON\n{req.downloadHandler.text}");

        // 성공/실패 로직 (message 없이)
        var resp = JsonConvert.DeserializeObject<BuyCurrencyResponseData>(req.downloadHandler.text);
        if (resp != null && resp.IsSuccess)
        {
            Debug.Log($"[MileageManager] 구매 성공! {data.GoodsType}");
        }
        else
        {
            Debug.LogWarning($"[MileageManager] 서버 처리 실패 (isSuccess == false)");
        }
    }

}
