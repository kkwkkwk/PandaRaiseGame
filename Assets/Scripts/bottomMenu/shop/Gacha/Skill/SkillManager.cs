using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI; // ScrollRect, LayoutRebuilder
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    [Header("스킬 패널 Canvas")]
    public GameObject skillPopupCanvas;

    [Header("ScrollView Content (Viewport→Content)")]
    public RectTransform scrollContent;

    [System.Serializable]
    public class HeaderConfig { public string headerName; public Transform contentParent; }
    [Header("헤더 + ContentParent 매핑")]
    public HeaderConfig[] headerConfigs;

    [Header("단일 아이템 Prefab")]
    public GameObject itemPrefab;

    private const string fetchSkillUrl =
        "https://pandaraisegame-shop.azurewebsites.net/api/GetSkillGachaData?code=5TkJ9Ck9okV81RdtuQA8jUEEEUDm37fq5owAnfIE-hBPAzFurDM8bA==";
    private const string purchaseSkillUrl =
        "https://pandaraisegame-shop.azurewebsites.net/api/BuySkillGachaItem";

    private List<SkillItemData> skillItems;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OpenSkillPanel()
    {
        skillPopupCanvas?.SetActive(true);
        scrollContent.gameObject.SetActive(false);
        StartCoroutine(FetchThenShow());
    }

    public void CloseSkillPanel()
    {
        skillPopupCanvas?.SetActive(false);
    }

    private IEnumerator FetchThenShow()
    {
        yield return FetchSkillDataCoroutine();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);
        var sr = scrollContent.GetComponentInParent<ScrollRect>();
        if (sr != null) sr.verticalNormalizedPosition = 1f;
        scrollContent.gameObject.SetActive(true);
    }

    private IEnumerator FetchSkillDataCoroutine()
    {
        var req = new UnityWebRequest(fetchSkillUrl, "POST");
        byte[] body = System.Text.Encoding.UTF8.GetBytes("{}");
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[SkillManager] Fetch 실패: {req.error}");
            yield break;
        }

        SkillResponseData resp = null;
        try { resp = JsonConvert.DeserializeObject<SkillResponseData>(req.downloadHandler.text); }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SkillManager] JSON 파싱 오류: {ex.Message}");
            yield break;
        }

        if (resp == null || !resp.IsSuccess)
        {
            Debug.LogWarning("[SkillManager] 서버 응답 이상");
            yield break;
        }

        LoadData(resp.SkillItemList);
    }

    public void LoadData(List<SkillItemData> items)
    {
        skillItems = items;
        Populate();
    }

    private void Populate()
    {
        foreach (var hc in headerConfigs)
            ClearContent(hc.contentParent);

        if (skillItems == null || skillItems.Count == 0) return;

        foreach (var item in skillItems)
        {
            var parent = GetParent(item.Header);
            if (parent == null) continue;

            var go = Instantiate(itemPrefab, parent);
            var ctrl = go.GetComponent<SmallItemController>();
            ctrl?.Setup(item.ItemName, null, item.Price, item.CurrencyType, item.ItemName, "Skill");
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

    public void PurchaseSkill(string itemName, string currencyType)
    {
        var requestData = new BuyGachaRequestData
        {
            PlayFabId = PlayerPrefs.GetString("PlayFabId"),
            ItemName = itemName,
            CurrencyType = currencyType,
            ItemType = "Skill"
        };
        StartCoroutine(SendBuyRequest(requestData));
    }

    private IEnumerator SendBuyRequest(BuyGachaRequestData data)
    {
        string json = JsonConvert.SerializeObject(data);
        var req = new UnityWebRequest(purchaseSkillUrl, "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonConvert.DeserializeObject<BuyGachaResponseData>(req.downloadHandler.text);
            if (resp != null && resp.IsSuccess)
                Debug.Log($"[SkillManager] 구매 성공! 뽑힌 아이템 수: {resp.OwnedItemList.Count}");
            else
                Debug.LogWarning("[SkillManager] 서버 처리 실패 (isSuccess == false)");
        }
        else
        {
            Debug.LogError($"[SkillManager] 요청 실패: {req.error}");
        }
    }
}
