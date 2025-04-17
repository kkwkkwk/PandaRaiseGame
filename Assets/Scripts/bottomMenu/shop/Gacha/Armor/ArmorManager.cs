using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ArmorManager : MonoBehaviour
{
    // 1) 싱글톤 인스턴스
    public static ArmorManager Instance { get; private set; }

    [Header("방어구 팝업 Panel/Canvas")]
    public GameObject armorPopupCanvas;

    [System.Serializable]
    public class HeaderConfig
    {
        public string headerName;
        public Transform contentParent;
    }
    [Header("헤더 + Content Parent 매핑")]
    public HeaderConfig[] headerConfigs;

    [Header("단일 아이템 프리팹")]
    public GameObject itemPrefab;

    private const string fetchArmorUrl = "https://pandaraisegame-shop.azurewebsites.net/api/GetArmorGachaData?code=oRjQ3RDt8YXXaItejzasxC8IpWXG9VY5nxWfjTqy3xB6AzFugcy0Ww==";

    private List<ArmorItemData> armorItems;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OpenArmorPanel()
    {
        if (armorPopupCanvas != null)
            armorPopupCanvas.SetActive(true);
        StartCoroutine(FetchArmorDataCoroutine());
    }

    public void CloseArmorPanel()
    {
        if (armorPopupCanvas != null)
            armorPopupCanvas.SetActive(false);
    }

    private IEnumerator FetchArmorDataCoroutine()
    {
        var req = new UnityWebRequest(fetchArmorUrl, "POST");
        byte[] body = System.Text.Encoding.UTF8.GetBytes("{}");
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[ArmorManager] 데이터 조회 실패: {req.error}");
            yield break;
        }

        ArmorResponseData resp = null;
        try
        {
            resp = JsonConvert.DeserializeObject<ArmorResponseData>(req.downloadHandler.text);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ArmorManager] JSON 파싱 오류: {ex.Message}");
            yield break;
        }

        if (resp == null || !resp.IsSuccess)
        {
            Debug.LogError("[ArmorManager] 응답 이상");
            yield break;
        }

        LoadData(resp.ArmorItemList);
    }

    public void LoadData(List<ArmorItemData> items)
    {
        armorItems = items;
        PopulateArmorItems();
    }

    private void PopulateArmorItems()
    {
        foreach (var hc in headerConfigs)
            ClearContent(hc.contentParent);

        if (armorItems == null || armorItems.Count == 0)
        {
            Debug.LogWarning("[ArmorManager] 아이템 목록이 없습니다.");
            return;
        }

        foreach (var item in armorItems)
        {
            Transform parent = GetContentParent(item.Header);
            if (parent == null || itemPrefab == null) continue;

            GameObject go = Instantiate(itemPrefab, parent);
            var ctrl = go.GetComponent<SmallItemController>();
            if (ctrl != null)
            {
                Sprite sprite = null;
                ctrl.Setup(item.ItemName, sprite, item.Price, item.CurrencyType);
            }
        }
    }

    private Transform GetContentParent(string header)
    {
        if (string.IsNullOrEmpty(header)) return null;
        foreach (var hc in headerConfigs)
            if (!string.IsNullOrEmpty(hc.headerName) && header.Contains(hc.headerName))
                return hc.contentParent;
        Debug.LogWarning($"[ArmorManager] 헤더 매칭 실패: {header}");
        return null;
    }

    private void ClearContent(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}
