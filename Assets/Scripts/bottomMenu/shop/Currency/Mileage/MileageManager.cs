using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;

public class MileageManager : MonoBehaviour
{
    [Header("마일리지(Mileage) 패널(Canvas)")]
    public GameObject mileagePopupCanvas;

    [System.Serializable]
    public class HeaderConfig
    {
        public string headerName;
        public Transform contentParent;
    }
    [Header("headerName-ContentParent 매핑 배열")]
    public HeaderConfig[] headerConfigs;

    [Header("프리팹(마일리지)")]
    public GameObject mileageItemCardPrefab;

    private string fetchMileageUrl = "https://YOUR_SERVER/api/GetMileageData";
    private List<MileageItemData> mileageItems;

    public void OpenMileagePanel()
    {
        if (mileagePopupCanvas != null)
            mileagePopupCanvas.SetActive(true);

        StartCoroutine(FetchMileageDataFromServer());
        Debug.Log("[MileageManager] OpenMileagePanel");
    }

    public void CloseMileagePanel()
    {
        if (mileagePopupCanvas != null)
            mileagePopupCanvas.SetActive(false);

        Debug.Log("[MileageManager] CloseMileagePanel");
    }

    private IEnumerator FetchMileageDataFromServer()
    {
        Debug.Log("[MileageManager] FetchMileageDataFromServer 시작");

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes("{}");
        using (UnityWebRequest request = new UnityWebRequest(fetchMileageUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[MileageManager] 조회 실패: " + request.error);
                yield break;
            }

            string respJson = request.downloadHandler.text;
            Debug.Log("[MileageManager] 서버 응답: " + respJson);

            MileageResponseData resp = null;
            try
            {
                resp = JsonConvert.DeserializeObject<MileageResponseData>(respJson);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[MileageManager] JSON 파싱 오류: " + ex.Message);
                yield break;
            }

            if (resp == null || !resp.IsSuccess)
            {
                Debug.LogError("[MileageManager] 응답 이상함");
                yield break;
            }

            LoadData(resp.MileageItemList);
        }
        Debug.Log("[MileageManager] FetchMileageDataFromServer 종료");
    }

    public void LoadData(List<MileageItemData> items)
    {
        mileageItems = items;
        Debug.Log($"[MileageManager] LoadData - count={mileageItems?.Count ?? 0}");
        PopulateMileageItems();
    }

    private void PopulateMileageItems()
    {
        // 1) Clear headerConfigs
        foreach (var hc in headerConfigs)
        {
            if (hc.contentParent != null)
                ClearContent(hc.contentParent);
        }

        if (mileageItems == null || mileageItems.Count == 0)
        {
            Debug.LogWarning("[MileageManager] 마일리지 아이템 없음");
            return;
        }

        foreach (var item in mileageItems)
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
        Debug.LogWarning("[MileageManager] 헤더 못찾음: " + header);
        return null;
    }

    private GameObject GetPrefabByCurrency(string currency)
    {
        switch (currency)
        {
            case "FREE": return mileageItemCardPrefab;
            default:
                Debug.LogWarning("[MileageManager] 알수없는통화: " + currency);
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
