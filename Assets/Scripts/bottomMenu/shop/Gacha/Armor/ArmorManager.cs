using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;

public class ArmorManager : MonoBehaviour
{
    [Header("방어구(Armor) 팝업 Panel/Canvas")]
    public GameObject armorPopupCanvas;  // 방어구 메뉴 전체 패널

    // (옵션) 서버 API URL
    private string fetchArmorUrl = "https://YOUR_SERVER/api/GetArmorData?code=YOUR_CODE_HERE";

    // 방어구 아이템 리스트
    private List<ArmorItemData> armorItems;

    /// <summary>
    /// 헤더(TitleBar)와 그 아래 Content를 매핑할 구조체
    /// </summary>
    [System.Serializable]
    public class HeaderConfig
    {
        public string headerName;       // item.Header와 매칭
        public Transform contentParent; // 해당 헤더에 속한 아이템들 배치할 ScrollView Content
    }

    [Header("헤더 + Content Parent 매핑")]
    public HeaderConfig[] headerConfigs;

    [Header("아이템 프리팹(결제수단별)")]
    public GameObject freeItemCardPrefab;
    public GameObject diamondItemCardPrefab;
    public GameObject goldItemCardPrefab;
    public GameObject wonItemCardPrefab;

    /// <summary>
    /// 방어구 메뉴(패널) 열기
    /// </summary>
    public void OpenArmorPanel()
    {
        if (armorPopupCanvas != null)
            armorPopupCanvas.SetActive(true);

        // 서버 요청 or 상위 매니저에서 LoadData() 직접 호출 가능
        StartCoroutine(FetchArmorDataFromServer());

        Debug.Log("[ArmorManager] OpenArmorPanel 호출");
    }

    /// <summary>
    /// 방어구 메뉴(패널) 닫기
    /// </summary>
    public void CloseArmorPanel()
    {
        if (armorPopupCanvas != null)
            armorPopupCanvas.SetActive(false);

        Debug.Log("[ArmorManager] CloseArmorPanel 호출");
    }

    /// <summary>
    /// 서버에서 방어구 데이터를 가져오는 코루틴
    /// </summary>
    private IEnumerator FetchArmorDataFromServer()
    {
        Debug.Log("[ArmorManager] FetchArmorDataFromServer 시작");

        string requestBody = "{}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestBody);

        using (UnityWebRequest request = new UnityWebRequest(fetchArmorUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[ArmorManager] 데이터 조회 실패: " + request.error);
                yield break;
            }

            string respJson = request.downloadHandler.text;
            Debug.Log("[ArmorManager] 서버 응답: " + respJson);

            ArmorResponseData resp = null;
            try
            {
                resp = JsonConvert.DeserializeObject<ArmorResponseData>(respJson);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[ArmorManager] JSON 파싱 오류: " + ex.Message);
                yield break;
            }

            if (resp == null || !resp.IsSuccess)
            {
                Debug.LogError("[ArmorManager] 응답이 이상함");
                yield break;
            }

            LoadData(resp.ArmorItemList);

            Debug.Log("[ArmorManager] FetchArmorDataFromServer 종료");
        }
    }

    /// <summary>
    /// 서버나 외부에서 받은 방어구 아이템 리스트를 저장 후 UI 배치
    /// </summary>
    public void LoadData(List<ArmorItemData> items)
    {
        armorItems = items;
        PopulateArmorItems();
    }

    /// <summary>
    /// 기존 UI를 Clear하고, item.Header에 따라 아이템 배치
    /// </summary>
    private void PopulateArmorItems()
    {
        // 모든 헤더의 Content Clear
        foreach (var hc in headerConfigs)
        {
            ClearContent(hc.contentParent);
        }

        if (armorItems == null || armorItems.Count == 0)
        {
            Debug.LogWarning("[ArmorManager] 방어구 아이템 없음");
            return;
        }

        foreach (var item in armorItems)
        {
            Transform targetParent = GetContentParentByHeader(item.Header);
            GameObject prefab = GetPrefabByCurrency(item.CurrencyType);

            if (targetParent == null || prefab == null) continue;

            GameObject cardObj = Instantiate(prefab, targetParent);
            // Layout Group 자동 정렬

            // 예: "ItemNameText", "PriceText"만 설정
            var nameText = cardObj.transform.Find("ItemNameText")?.GetComponent<TextMeshProUGUI>();
            var priceText = cardObj.transform.Find("PriceText")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null) nameText.text = item.ItemName;
            if (priceText != null) priceText.text = $"{item.Price} ({item.CurrencyType})";
        }
    }

    /// <summary>
    /// headerConfigs[]에서 headerName이 item.Header에 해당하는 contentParent를 찾는다
    /// </summary>
    private Transform GetContentParentByHeader(string header)
    {
        if (string.IsNullOrEmpty(header)) return null;
        foreach (var hc in headerConfigs)
        {
            if (!string.IsNullOrEmpty(hc.headerName) && header.Contains(hc.headerName))
                return hc.contentParent;
        }
        Debug.LogWarning($"[ArmorManager] '{header}'에 매칭되는 헤더 없음");
        return null;
    }

    /// <summary>
    /// 결제수단 별 프리팹 반환
    /// </summary>
    private GameObject GetPrefabByCurrency(string currency)
    {
        switch (currency)
        {
            case "FREE": return freeItemCardPrefab;
            case "DIAMOND": return diamondItemCardPrefab;
            case "GC": return goldItemCardPrefab;
            case "WON": return wonItemCardPrefab;
            default:
                Debug.LogWarning($"[ArmorManager] 알 수 없는 CurrencyType: {currency}");
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
