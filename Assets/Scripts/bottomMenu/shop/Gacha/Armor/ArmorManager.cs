using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;                // ScrollRect, LayoutRebuilder
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ArmorManager : MonoBehaviour
{
    public static ArmorManager Instance { get; private set; }

    [Header("방어구 패널 Canvas")]
    public GameObject armorPopupCanvas;

    [Header("ScrollView Content (Viewport→Content)")]
    public RectTransform scrollContent;   // ← 추가

    [System.Serializable]
    public class HeaderConfig { public string headerName; public Transform contentParent; }
    [Header("헤더 + ContentParent 매핑")]
    public HeaderConfig[] headerConfigs;

    [Header("단일 아이템 Prefab")]
    public GameObject itemPrefab;

    private const string fetchArmorUrl =
        "https://pandaraisegame-shop.azurewebsites.net/api/GetArmorGachaData?code=oRjQ3RDt8YXXaItejzasxC8IpWXG9VY5nxWfjTqy3xB6AzFugcy0Ww==";

    private List<ArmorItemData> armorItems;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 방어구 탭 열기: 패널 켜고, 스크롤 콘텐츠 숨긴 뒤 데이터 로드
    /// </summary>
    public void OpenArmorPanel()
    {
        armorPopupCanvas?.SetActive(true);
        scrollContent.gameObject.SetActive(false);
        StartCoroutine(FetchThenShow());
    }

    public void CloseArmorPanel()
    {
        armorPopupCanvas?.SetActive(false);
    }

    private IEnumerator FetchThenShow()
    {
        // 데이터 Fetch → LoadData → Populate
        yield return FetchArmorDataCoroutine();

        // 레이아웃 강제 재빌드
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);

        // 스크롤 맨 위로 초기화
        var sr = scrollContent.GetComponentInParent<ScrollRect>();
        if (sr != null) sr.verticalNormalizedPosition = 1f;

        // 콘텐츠 보이기
        scrollContent.gameObject.SetActive(true);
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
            Debug.LogError($"[ArmorManager] Fetch 실패: {req.error}");
            yield break;
        }

        ArmorResponseData resp = null;
        try { resp = JsonConvert.DeserializeObject<ArmorResponseData>(req.downloadHandler.text); }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ArmorManager] JSON 파싱 오류: {ex.Message}");
            yield break;
        }

        if (resp == null || !resp.IsSuccess)
        {
            Debug.LogWarning("[ArmorManager] 서버 응답 이상");
            yield break;
        }

        LoadData(resp.ArmorItemList);
    }

    public void LoadData(List<ArmorItemData> items)
    {
        armorItems = items;
        Populate();
    }

    private void Populate()
    {
        // 기존 콘텐츠 비우기
        foreach (var hc in headerConfigs)
            ClearContent(hc.contentParent);

        if (armorItems == null || armorItems.Count == 0) return;

        // 프리팹 생성
        foreach (var item in armorItems)
        {
            var parent = GetParent(item.Header);
            if (parent == null) continue;

            var go = Instantiate(itemPrefab, parent);
            var ctrl = go.GetComponent<SmallItemController>();
            ctrl?.Setup(item.ItemName, null, item.Price, item.CurrencyType);
        }
    }

    private Transform GetParent(string header)
    {
        foreach (var hc in headerConfigs)
            if (!string.IsNullOrEmpty(hc.headerName) && header.Contains(hc.headerName))
                return hc.contentParent;
        return null;
    }

    private void ClearContent(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}
