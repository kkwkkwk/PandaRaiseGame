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

    [Header("기본 컨텐츠 (헤더 미설정 시)")]
    public Transform defaultContentParent;

    [System.Serializable]
    public class HeaderConfig { public string headerName; public Transform contentParent; }
    [Header("Header → Content Parent 매핑")]
    public HeaderConfig[] headerConfigs;

    [Header("단일 아이템 Prefab")]
    public GameObject itemPrefab;

    private string fetchMileageUrl = "https://pandaraisegame-shop.azurewebsites.net/api/GetMileageShopData?code=7FEp57GIrRLmJG0-E3k5IuuksDTzUgpcSkJiNRzVM3H2AzFuWLTgiw==";
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
        Debug.Log("[MileageManager] ▶ 서버에서 받은 Header 목록:");
        foreach (var it in items)
            Debug.Log($"    • '{it.Header}'");

        mileageItems = items;
        PopulateMileageItems();
    }

    private void PopulateMileageItems()
    {
        // 기존 콘텐츠 모두 클리어
        foreach (var hc in headerConfigs)
            ClearContent(hc.contentParent);
        ClearContent(defaultContentParent);

        if (mileageItems == null || mileageItems.Count == 0) return;

        // 아이템 배치
        foreach (var item in mileageItems)
        {
            // 헤더 매칭 → 없으면 default
            var parent = GetContentParent(item.Header) ?? defaultContentParent;
            if (parent == null || itemPrefab == null) continue;

            var go = Instantiate(itemPrefab, parent);
            var ctrl = go.GetComponent<SmallItemController>();
            ctrl?.Setup(item.ItemName, null, item.Price, item.CurrencyType);
        }
    }

    private Transform GetContentParent(string header)
    {
        if (string.IsNullOrEmpty(header)) return null;
        foreach (var hc in headerConfigs)
            if (header.Contains(hc.headerName))
                return hc.contentParent;
        // 매칭 실패 시 null 반환 → defaultContentParent 로 배치
        return null;
    }

    private void ClearContent(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}
