using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class MileageManager : MonoBehaviour
{
    public static MileageManager Instance { get; private set; }

    [Header("마일리지 탭 팝업 Canvas")]
    public GameObject mileagePopupCanvas;

    [System.Serializable]
    public class HeaderConfig { public string headerName; public Transform contentParent; }
    [Header("Header → Content Parent 매핑")]
    public HeaderConfig[] headerConfigs;

    [Header("단일 아이템 Prefab")]
    public GameObject itemPrefab;

    private string fetchMileageUrl = "https://YOUR_SERVER/api/GetMileageData";
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
        if (req.result != UnityWebRequest.Result.Success) yield break;

        var resp = JsonConvert.DeserializeObject<MileageResponseData>(req.downloadHandler.text);
        if (resp == null || !resp.IsSuccess) yield break;

        LoadData(resp.MileageItemList);
    }

    public void LoadData(List<MileageItemData> items)
    {
        mileageItems = items;
        PopulateMileageItems();
    }

    private void PopulateMileageItems()
    {
        foreach (var hc in headerConfigs)
            ClearContent(hc.contentParent);

        if (mileageItems == null || mileageItems.Count == 0) return;

        foreach (var item in mileageItems)
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
        Debug.LogWarning($"[MileageManager] 헤더 미발견: {header}");
        return null;
    }

    private void ClearContent(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}
