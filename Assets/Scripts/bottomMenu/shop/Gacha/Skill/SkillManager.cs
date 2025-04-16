using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;

public class SkillManager : MonoBehaviour
{
    [Header("스킬(Skill) 팝업 Panel/Canvas")]
    public GameObject skillPopupCanvas;  // 스킬 메뉴 전체 패널

    // 서버 API URL
    private string fetchSkillUrl = "https://YOUR_SERVER/api/GetSkillData?code=YOUR_CODE_HERE";

    // 스킬 아이템 리스트
    private List<SkillItemData> skillItems;

    [System.Serializable]
    public class HeaderConfig
    {
        public string headerName;       // 예: "초급 스킬", "고급 스킬"
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
    /// 스킬 메뉴(패널) 열기
    /// </summary>
    public void OpenSkillPanel()
    {
        if (skillPopupCanvas != null)
            skillPopupCanvas.SetActive(true);

        // 서버 요청 or LoadData() 직접
        StartCoroutine(FetchSkillDataFromServer());

        Debug.Log("[SkillManager] OpenSkillPanel 호출");
    }

    /// <summary>
    /// 스킬 메뉴(패널) 닫기
    /// </summary>
    public void CloseSkillPanel()
    {
        if (skillPopupCanvas != null)
            skillPopupCanvas.SetActive(false);

        Debug.Log("[SkillManager] CloseSkillPanel 호출");
    }

    private IEnumerator FetchSkillDataFromServer()
    {
        Debug.Log("[SkillManager] FetchSkillDataFromServer 시작");

        string requestBody = "{}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestBody);

        using (UnityWebRequest request = new UnityWebRequest(fetchSkillUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[SkillManager] 데이터 조회 실패: " + request.error);
                yield break;
            }

            string respJson = request.downloadHandler.text;
            Debug.Log("[SkillManager] 서버 응답: " + respJson);

            SkillResponseData resp = null;
            try
            {
                resp = JsonConvert.DeserializeObject<SkillResponseData>(respJson);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[SkillManager] JSON 파싱 오류: " + ex.Message);
                yield break;
            }

            if (resp == null || !resp.IsSuccess)
            {
                Debug.LogError("[SkillManager] 응답이 이상함");
                yield break;
            }

            LoadData(resp.SkillItemList);

            Debug.Log("[SkillManager] FetchSkillDataFromServer 종료");
        }
    }

    public void LoadData(List<SkillItemData> items)
    {
        skillItems = items;
        PopulateSkillItems();
    }

    private void PopulateSkillItems()
    {
        // Clear all header parents
        foreach (var hc in headerConfigs)
        {
            ClearContent(hc.contentParent);
        }

        if (skillItems == null || skillItems.Count == 0)
        {
            Debug.LogWarning("[SkillManager] 스킬 아이템 없음");
            return;
        }

        foreach (var item in skillItems)
        {
            Transform parent = GetContentParentByHeader(item.Header);
            GameObject prefab = GetPrefabByCurrency(item.CurrencyType);

            if (parent == null || prefab == null) continue;

            GameObject cardObj = Instantiate(prefab, parent);
            var nameText = cardObj.transform.Find("ItemNameText")?.GetComponent<TextMeshProUGUI>();
            var priceText = cardObj.transform.Find("PriceText")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null) nameText.text = item.ItemName;
            if (priceText != null) priceText.text = $"{item.Price} ({item.CurrencyType})";
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
        Debug.LogWarning($"[SkillManager] '{header}'에 매칭되는 헤더 없음");
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
                Debug.LogWarning($"[SkillManager] 알 수 없는 CurrencyType: {currency}");
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
