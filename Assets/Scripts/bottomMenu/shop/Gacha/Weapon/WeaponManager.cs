using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;                // ScrollRect, LayoutRebuilder
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class WeaponManager : MonoBehaviour
{
    public static WeaponManager Instance { get; private set; }

    [Header("무기 패널 Canvas")]
    public GameObject weaponPopupCanvas;

    [Header("ScrollView Content (Viewport→Content)")]
    public RectTransform scrollContent;

    [System.Serializable]
    public class HeaderConfig { public string headerName; public Transform contentParent; }
    [Header("헤더 + ContentParent 매핑")]
    public HeaderConfig[] headerConfigs;

    [Header("단일 아이템 Prefab")]
    public GameObject itemPrefab;

    private const string fetchWeaponUrl =
        "https://pandaraisegame-shop.azurewebsites.net/api/GetWeaponGachaData?code=LbwpDyYAz2G1OE94wg-oUgI5VHYlmOY54oFUWFTUiJ7PAzFuafMI_g==";
    private const string purchaseWeaponUrl =
        "https://pandaraisegame-shop.azurewebsites.net/api/BuyWeaponGachaItem";

    private List<WeaponItemData> weaponItems;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OpenWeaponPanel()
    {
        weaponPopupCanvas?.SetActive(true);
        scrollContent.gameObject.SetActive(false);
        StartCoroutine(FetchThenShow());
    }

    public void CloseWeaponPanel()
    {
        weaponPopupCanvas?.SetActive(false);
    }

    private IEnumerator FetchThenShow()
    {
        yield return FetchWeaponDataCoroutine();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);
        var sr = scrollContent.GetComponentInParent<ScrollRect>();
        if (sr != null) sr.verticalNormalizedPosition = 1f;
        scrollContent.gameObject.SetActive(true);
    }

    private IEnumerator FetchWeaponDataCoroutine()
    {
        var req = new UnityWebRequest(fetchWeaponUrl, "POST");
        byte[] body = System.Text.Encoding.UTF8.GetBytes("{}");
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[WeaponManager] Fetch 실패: {req.error}");
            yield break;
        }

        WeaponResponseData resp = null;
        try { resp = JsonConvert.DeserializeObject<WeaponResponseData>(req.downloadHandler.text); }
        catch (System.Exception ex)
        {
            Debug.LogError($"[WeaponManager] JSON 파싱 오류: {ex.Message}");
            yield break;
        }

        if (resp == null || !resp.IsSuccess)
        {
            Debug.LogWarning("[WeaponManager] 서버 응답 이상");
            yield break;
        }

        LoadData(resp.WeaponItemList);
    }

    public void LoadData(List<WeaponItemData> items)
    {
        weaponItems = items;
        Populate();
    }

    private void Populate()
    {
        foreach (var hc in headerConfigs)
            ClearContent(hc.contentParent);

        if (weaponItems == null || weaponItems.Count == 0) return;

        foreach (var item in weaponItems)
        {
            var parent = GetParent(item.Header);
            if (parent == null) continue;

            var go = Instantiate(itemPrefab, parent);
            var ctrl = go.GetComponent<SmallItemController>();
            // 6인자 오버로드 호출
            ctrl?.Setup(item.ItemName, null, item.Price, item.CurrencyType, item.ItemName, "Weapon");
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

    // 구매 기능
    public void PurchaseWeapon(string itemName, string currencyType)
    {
        var requestData = new BuyGachaRequestData
        {
            PlayFabId = PlayerPrefs.GetString("PlayFabId"),
            ItemName = itemName,
            CurrencyType = currencyType,
            ItemType = "Weapon"
        };
        StartCoroutine(SendBuyRequest(requestData));
    }

    private IEnumerator SendBuyRequest(BuyGachaRequestData data)
    {
        string json = JsonConvert.SerializeObject(data);
        var req = new UnityWebRequest(purchaseWeaponUrl, "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonConvert.DeserializeObject<BuyGachaResponseData>(req.downloadHandler.text);
            if (resp != null && resp.IsSuccess)
                Debug.Log($"[WeaponManager] 구매 성공! 뽑힌 아이템 수: {resp.OwnedItemList.Count}");
            else
                Debug.LogWarning("[WeaponManager] 서버 처리 실패 (isSuccess == false)");
        }
        else
        {
            Debug.LogError($"[WeaponManager] 요청 실패: {req.error}");
        }
    }
}
