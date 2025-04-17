using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class FreeManager : MonoBehaviour
{
    public static FreeManager Instance { get; private set; }

    [Header("무료(Free) 탭 팝업 Canvas")]
    public GameObject freePopupCanvas;

    [System.Serializable]
    public class HeaderConfig { public string headerName; public Transform contentParent; }
    [Header("Header → Content Parent 매핑")]
    public HeaderConfig[] headerConfigs;

    [Header("단일 아이템 Prefab")]
    public GameObject itemPrefab;           

    private string fetchFreeUrl = "https://pandaraisegame-shop.azurewebsites.net/api/GetFreeShopData?code=RSjJW7s3xyKlU3iJzYog0Fd50ylVsuIp-PdqRpG807e4AzFu0MBECg==";
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
        if (req.result != UnityWebRequest.Result.Success) yield break;

        var resp = JsonConvert.DeserializeObject<FreeResponseData>(req.downloadHandler.text);
        if (resp == null || !resp.IsSuccess) yield break;

        LoadData(resp.FreeItemList);
    }

    public void LoadData(List<FreeItemData> items)
    {
        freeItems = items;
        PopulateFreeItems();
    }

    private void PopulateFreeItems()
    {
        // 1) 모든 헤더 영역 비우기
        foreach (var hc in headerConfigs)
            ClearContent(hc.contentParent);

        if (freeItems == null || freeItems.Count == 0) return;

        // 2) 단일 Prefab으로 Instantiate + Setup
        foreach (var item in freeItems)
        {
            var parent = GetContentParent(item.Header);
            if (parent == null || itemPrefab == null) continue;

            var go = Instantiate(itemPrefab, parent);
            var ctrl = go.GetComponent<SmallItemController>();
            if (ctrl != null)
            {
                // 이름, null(sprite), 가격, 통화타입 전달
                ctrl.Setup(item.ItemName, null, item.Price, item.CurrencyType);
            }
        }
    }

    private Transform GetContentParent(string header)
    {
        if (string.IsNullOrEmpty(header)) return null;
        foreach (var hc in headerConfigs)
            if (header.Contains(hc.headerName))
                return hc.contentParent;
        Debug.LogWarning($"[FreeManager] 헤더 미발견: {header}");
        return null;
    }

    private void ClearContent(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}
