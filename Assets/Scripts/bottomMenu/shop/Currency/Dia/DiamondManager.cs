using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class DiamondManager : MonoBehaviour
{
    public static DiamondManager Instance { get; private set; }

    [Header("다이아 탭 팝업 Canvas")]
    public GameObject diamondPopupCanvas;

    [System.Serializable]
    public class HeaderConfig { public string headerName; public Transform contentParent; }
    [Header("Header → Content Parent 매핑")]
    public HeaderConfig[] headerConfigs;

    [Header("단일 아이템 Prefab")]
    public GameObject itemPrefab;

    private string fetchDiamondUrl = "https://YOUR_SERVER/api/GetDiamondData";
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
        if (req.result != UnityWebRequest.Result.Success) yield break;

        var resp = JsonConvert.DeserializeObject<DiamondResponseData>(req.downloadHandler.text);
        if (resp == null || !resp.IsSuccess) yield break;

        LoadData(resp.DiamondItemList);
    }

    public void LoadData(List<DiamondItemData> items)
    {
        diamondItems = items;
        PopulateDiamondItems();
    }

    private void PopulateDiamondItems()
    {
        foreach (var hc in headerConfigs)
            ClearContent(hc.contentParent);

        if (diamondItems == null || diamondItems.Count == 0) return;

        foreach (var item in diamondItems)
        {
            var parent = GetContentParent(item.Header);
            if (parent == null || itemPrefab == null) continue;

            var go = Instantiate(itemPrefab, parent);
            var ctrl = go.GetComponent<SmallItemController>();
            if (ctrl != null)
                ctrl.Setup(item.ItemName, null, item.Price, item.CurrencyType);
        }
    }

    private Transform GetContentParent(string header)
    {
        foreach (var hc in headerConfigs)
            if (header.Contains(hc.headerName))
                return hc.contentParent;
        Debug.LogWarning($"[DiamondManager] 헤더 미발견: {header}");
        return null;
    }

    private void ClearContent(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}
