using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;

public class FreeManager : MonoBehaviour
{
    [Header("무료(Free) 탭 패널(Canvas)")]
    public GameObject freePopupCanvas;

    [System.Serializable]
    public class HeaderConfig
    {
        public string headerName;
        public Transform contentParent;
    }
    [Header("headerName-ContentParent 매핑 배열")]
    public HeaderConfig[] headerConfigs;

    [Header("아이템 프리팹(결제수단별)")]
    public GameObject freeItemCardPrefab;
    public GameObject diamondItemCardPrefab;
    public GameObject goldItemCardPrefab;
    public GameObject wonItemCardPrefab;

    private string fetchFreeUrl = "https://YOUR_SERVER/api/GetFreeData"; // 서버 URL
    private List<FreeItemData> freeItems;

    public void OpenFreePanel()
    {
        if (freePopupCanvas != null)
            freePopupCanvas.SetActive(true);

        StartCoroutine(FetchFreeDataFromServer());
        Debug.Log("[FreeManager] OpenFreePanel");
    }

    public void CloseFreePanel()
    {
        if (freePopupCanvas != null)
            freePopupCanvas.SetActive(false);

        Debug.Log("[FreeManager] CloseFreePanel");
    }

    private IEnumerator FetchFreeDataFromServer()
    {
        Debug.Log("[FreeManager] FetchFreeDataFromServer 시작");

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes("{}");
        using (UnityWebRequest request = new UnityWebRequest(fetchFreeUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[FreeManager] 서버 조회 실패: " + request.error);
                yield break;
            }

            string respJson = request.downloadHandler.text;
            Debug.Log("[FreeManager] 서버 응답: " + respJson);

            FreeResponseData resp = null;
            try
            {
                resp = JsonConvert.DeserializeObject<FreeResponseData>(respJson);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[FreeManager] JSON 파싱 오류: " + ex.Message);
                yield break;
            }

            if (resp == null || !resp.IsSuccess)
            {
                Debug.LogError("[FreeManager] 응답이상");
                yield break;
            }

            LoadData(resp.FreeItemList);
        }
    }

    public void LoadData(List<FreeItemData> items)
    {
        freeItems = items;
        Debug.Log($"[FreeManager] LoadData - count={freeItems?.Count ?? 0}");
        PopulateFreeItems();
    }

    private void PopulateFreeItems()
    {
        // 1) 모든 headerConfigs에 대해 Clear
        foreach (var hc in headerConfigs)
        {
            if (hc.contentParent != null)
                ClearContent(hc.contentParent);
        }

        if (freeItems == null || freeItems.Count == 0)
        {
            Debug.LogWarning("[FreeManager] 무료 아이템 없음");
            return;
        }

        // 2) item.Header에 따라 contentParent 찾고, currencyType 따라 프리팹 결정
        foreach (var item in freeItems)
        {
            Transform parent = GetContentParentByHeader(item.Header);
            if (parent == null) continue;

            GameObject prefab = GetPrefabByCurrency(item.CurrencyType);
            if (prefab == null) continue;

            var cardObj = Instantiate(prefab, parent);
            var nameText = cardObj.transform.Find("ItemNameText")?.GetComponent<TextMeshProUGUI>();
            var priceText = cardObj.transform.Find("PriceText")?.GetComponent<TextMeshProUGUI>();

            if (nameText != null)
                nameText.text = item.ItemName;

            if (priceText != null)
            {
                // FREE → "무료"
                // DIAMOND → "n 다이아", etc...
                if (item.CurrencyType == "FREE") priceText.text = "무료";
                else priceText.text = $"{item.Price} ({item.CurrencyType})";
            }
        }
    }

    private Transform GetContentParentByHeader(string header)
    {
        if (string.IsNullOrEmpty(header)) return null;
        foreach (var hc in headerConfigs)
        {
            if (!string.IsNullOrEmpty(hc.headerName) && header.Contains(hc.headerName))
                return hc.contentParent;
        }
        Debug.LogWarning($"[FreeManager] '{header}' 헤더 미발견");
        return null;
    }

    private GameObject GetPrefabByCurrency(string currencyType)
    {
        switch (currencyType)
        {
            case "FREE": return freeItemCardPrefab;
            case "DIAMOND": return diamondItemCardPrefab;
            case "GC": return goldItemCardPrefab;
            case "WON": return wonItemCardPrefab;
            default:
                Debug.LogWarning("[FreeManager] 알수없는 currency=" + currencyType);
                return null;
        }
    }

    private void ClearContent(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
}
