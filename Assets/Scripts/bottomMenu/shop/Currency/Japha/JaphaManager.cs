using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;

public class JaphaManager : MonoBehaviour
{
    [Header("잡화 탭 패널(Canvas)")]
    public GameObject japhaPopupCanvas;

    [System.Serializable]
    public class HeaderConfig
    {
        public string headerName;
        public Transform contentParent;
    }
    [Header("headerName-ContentParent 매핑 배열")]
    public HeaderConfig[] headerConfigs;

    [Header("아이템 프리팹(결제수단별)")]
    public GameObject freeItemPrefab;
    public GameObject diamondItemPrefab;
    public GameObject goldItemPrefab;
    public GameObject wonItemPrefab;

    private string fetchJaphaUrl = "https://YOUR_SERVER/api/GetJaphaData";
    private List<JaphaItemData> currentJaphaItems;

    public void OpenJaphaPanel()
    {
        if (japhaPopupCanvas != null)
            japhaPopupCanvas.SetActive(true);

        StartCoroutine(FetchJaphaDataFromServer());
        Debug.Log("[JaphaManager] OpenJaphaPanel");
    }

    public void CloseJaphaPanel()
    {
        if (japhaPopupCanvas != null)
            japhaPopupCanvas.SetActive(false);

        Debug.Log("[JaphaManager] CloseJaphaPanel");
    }

    private IEnumerator FetchJaphaDataFromServer()
    {
        Debug.Log("[JaphaManager] FetchJaphaDataFromServer 시작");

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes("{}");
        using (UnityWebRequest request = new UnityWebRequest(fetchJaphaUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[JaphaManager] 조회 실패: " + request.error);
                yield break;
            }

            string respJson = request.downloadHandler.text;
            Debug.Log("[JaphaManager] 서버 응답: " + respJson);

            JaphaResponseData resp = null;
            try
            {
                resp = JsonConvert.DeserializeObject<JaphaResponseData>(respJson);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[JaphaManager] JSON 파싱 오류: " + ex.Message);
                yield break;
            }

            if (resp == null || !resp.IsSuccess)
            {
                Debug.LogError("[JaphaManager] 응답 이상");
                yield break;
            }

            LoadData(resp.JaphaItemList);
        }
        Debug.Log("[JaphaManager] FetchJaphaDataFromServer 종료");
    }

    public void LoadData(List<JaphaItemData> items)
    {
        currentJaphaItems = items;
        Debug.Log($"[JaphaManager] LoadData: count={currentJaphaItems?.Count ?? 0}");
        PopulateJaphaItems();
    }

    private void PopulateJaphaItems()
    {
        // 1) 헤더별 Clear
        foreach (var hc in headerConfigs)
        {
            if (hc.contentParent != null)
                ClearContent(hc.contentParent);
        }

        if (currentJaphaItems == null || currentJaphaItems.Count == 0)
        {
            Debug.LogWarning("[JaphaManager] 잡화 아이템 없음");
            return;
        }

        // 2) header → contentParent, currencyType → prefab
        foreach (var item in currentJaphaItems)
        {
            var parent = GetContentByHeader(item.Header);
            if (parent == null) continue;

            var prefab = GetPrefabByCurrency(item.CurrencyType);
            if (prefab == null) continue;

            var cardObj = Instantiate(prefab, parent);
            var nameText = cardObj.transform.Find("ItemNameText")?.GetComponent<TextMeshProUGUI>();
            var priceText = cardObj.transform.Find("PriceText")?.GetComponent<TextMeshProUGUI>();

            if (nameText != null) nameText.text = item.ItemName;
            if (priceText != null) priceText.text = $"{item.Price} ({item.CurrencyType})";
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
        Debug.LogWarning("[JaphaManager] 헤더 못찾음: " + header);
        return null;
    }

    private GameObject GetPrefabByCurrency(string currency)
    {
        switch (currency)
        {
            case "FREE": return freeItemPrefab;
            case "DIAMOND": return diamondItemPrefab;
            case "GC": return goldItemPrefab;
            case "WON": return wonItemPrefab;
            default:
                Debug.LogWarning("[JaphaManager] 알수없는통화: " + currency);
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
