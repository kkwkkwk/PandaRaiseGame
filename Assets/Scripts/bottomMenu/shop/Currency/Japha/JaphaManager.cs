using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;                 // ScrollRect, LayoutRebuilder
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class JaphaManager : MonoBehaviour
{
    public static JaphaManager Instance { get; private set; }

    [Header("잡화 탭 팝업 Canvas")]
    public GameObject japhaPopupCanvas;

    [Header("ScrollView Content (Viewport → Content)")]
    public RectTransform scrollContent;   // 반드시 Inspector에서 Content로 연결하세요

    [Header("기본 컨텐츠 (헤더 미발견 시)")]
    public Transform defaultContentParent;

    [System.Serializable]
    public class HeaderConfig { public string headerName; public Transform contentParent; }
    [Header("Header → Content Parent 매핑")]
    public HeaderConfig[] headerConfigs;

    [Header("단일 아이템 Prefab")]
    public GameObject itemPrefab;

    private const string fetchJaphaUrl =
        "https://pandaraisegame-shop.azurewebsites.net/api/GetJaphwaShopData?code=Qjq_KGQpLvoZjJKDCR76iOJE9EjQHoO2PvucK7Ea92-EAzFu8w6Mtg==";

    private List<JaphaItemData> japhaItems;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 잡화 탭 열기:
    ///   1) 팝업 Canvas 켜고
    ///   2) 스크롤 Content 숨김
    ///   3) 데이터 Fetch → Populate → 레이아웃 재빌드 → Content 보임
    /// </summary>
    public void OpenJaphaPanel()
    {
        japhaPopupCanvas?.SetActive(true);
        // 1) Content 먼저 숨기기
        scrollContent.gameObject.SetActive(false);
        StartCoroutine(FetchAndShow());
    }

    public void CloseJaphaPanel()
    {
        japhaPopupCanvas?.SetActive(false);
    }

    private IEnumerator FetchAndShow()
    {
        // 데이터 가져오고 Populate까지 끝냄
        yield return FetchJaphaDataCoroutine();

        // 레이아웃 강제 재빌드 (겹침 방지)
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);

        // 스크롤 맨 위로 리셋
        var sr = scrollContent.GetComponentInParent<ScrollRect>();
        if (sr != null) sr.verticalNormalizedPosition = 1f;

        // Content 다시 보이기
        scrollContent.gameObject.SetActive(true);
    }

    private IEnumerator FetchJaphaDataCoroutine()
    {
        var body = System.Text.Encoding.UTF8.GetBytes("{}");
        using var req = new UnityWebRequest(fetchJaphaUrl, "POST")
        {
            uploadHandler = new UploadHandlerRaw(body),
            downloadHandler = new DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[JaphaManager] Fetch 실패: {req.error}");
            yield break;
        }

        // Raw JSON 로그 (필요 없으면 지워도 됩니다)
        string raw = req.downloadHandler.text;
        Debug.Log($"[JaphaManager] ▶ Raw JSON: {raw}");

        var resp = JsonConvert.DeserializeObject<JaphaResponseData>(raw);
        if (resp == null || !resp.IsSuccess)
        {
            Debug.LogError("[JaphaManager] 서버 응답 이상");
            yield break;
        }

        // 리스트 저장 & 화면 갱신
        japhaItems = resp.JaphaItemList;
        PopulateJaphaItems();
    }

    private void PopulateJaphaItems()
    {
        // 1) 기존에 뿌려진 모든 자식 지우기
        foreach (var hc in headerConfigs)
            ClearContent(hc.contentParent);
        ClearContent(defaultContentParent);

        if (japhaItems == null || japhaItems.Count == 0) return;

        // 2) 헤더 매칭 → Prefab Instantiate
        foreach (var item in japhaItems)
        {
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
        return null;
    }

    private void ClearContent(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}
