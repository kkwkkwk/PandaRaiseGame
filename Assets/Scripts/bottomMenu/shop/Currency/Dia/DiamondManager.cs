using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;

public class DiamondManager : MonoBehaviour
{
    [Header("다이아(Diamond) 패널(Canvas)")]
    public GameObject diamondPopupCanvas;

    [System.Serializable]
    public class HeaderConfig
    {
        public string headerName;
        public Transform contentParent;
    }
    [Header("headerName-ContentParent 매핑 배열")]
    public HeaderConfig[] headerConfigs;

    [Header("다이아 아이템 Prefab(결제수단별)")]
    public GameObject freeItemCardPrefab;
    public GameObject diamondItemCardPrefab;
    public GameObject goldItemCardPrefab;
    public GameObject wonItemCardPrefab;

    private string fetchDiamondUrl = "https://YOUR_SERVER/api/GetDiamondData";
    private List<DiamondItemData> diamondItems;

    public void OpenDiamondPanel()
    {
        if (diamondPopupCanvas != null)
            diamondPopupCanvas.SetActive(true);

        StartCoroutine(FetchDiamondDataFromServer());
        Debug.Log("[DiamondManager] OpenDiamondPanel");
    }

    public void CloseDiamondPanel()
    {
        if (diamondPopupCanvas != null)
            diamondPopupCanvas.SetActive(false);

        Debug.Log("[DiamondManager] CloseDiamondPanel");
    }

    private IEnumerator FetchDiamondDataFromServer()
    {
        Debug.Log("[DiamondManager] FetchDiamondDataFromServer 시작");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes("{}");
        using (UnityWebRequest request = new UnityWebRequest(fetchDiamondUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[DiamondManager] 조회 실패: " + request.error);
                yield break;
            }

            string respJson = request.downloadHandler.text;
            Debug.Log("[DiamondManager] 응답: " + respJson);

            DiamondResponseData resp = null;
            try
            {
                resp = JsonConvert.DeserializeObject<DiamondResponseData>(respJson);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[DiamondManager] JSON 파싱 오류: " + ex.Message);
                yield break;
            }

            if (resp == null || !resp.IsSuccess)
            {
                Debug.LogError("[DiamondManager] 응답 이상함");
                yield break;
            }

            LoadData(resp.DiamondItemList);
        }
    }

    public void LoadData(List<DiamondItemData> items)
    {
        diamondItems = items;
        Debug.Log($"[DiamondManager] LoadData count={diamondItems?.Count ?? 0}");
        PopulateDiamondItems();
    }

    private void PopulateDiamondItems()
    {
        // Clear
        foreach (var hc in headerConfigs)
        {
            if (hc.contentParent != null)
                ClearContent(hc.contentParent);
        }

        if (diamondItems == null || diamondItems.Count == 0)
        {
            Debug.LogWarning("[DiamondManager] 다이아 아이템 없음");
            return;
        }

        foreach (var item in diamondItems)
        {
            var parent = GetContentByHeader(item.Header);
            if (parent == null) continue;

            var prefab = GetPrefabByCurrency(item.CurrencyType);
            if (prefab == null) continue;

            var cardObj = Instantiate(prefab, parent);
            var nameText = cardObj.transform.Find("ItemNameText")?.GetComponent<TextMeshProUGUI>();
            var priceText = cardObj.transform.Find("PriceText")?.GetComponent<TextMeshProUGUI>();

            if (nameText != null)
                nameText.text = item.ItemName;
            if (priceText != null)
                priceText.text = $"{item.Price}원";
        }
    }

    private Transform GetContentByHeader(string header)
    {
        if (string.IsNullOrEmpty(header)) return null;
        foreach (var hc in headerConfigs)
        {
            if (!string.IsNullOrEmpty(hc.headerName) && header.Contains(hc.headerName))
                return hc.contentParent;
        }
        Debug.LogWarning("[DiamondManager] 헤더매칭실패: " + header);
        return null;
    }

    private GameObject GetPrefabByCurrency(string currency)
    {
        switch (currency)
        {
            case "FREE": return freeItemCardPrefab;
            case "DIAMOND": return diamondItemCardPrefab;
            case "GC": return goldItemCardPrefab;
            case "WON": return wonItemCardPrefab;
            default:
                Debug.LogWarning("[DiamondManager] 알수없는통화:" + currency);
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
