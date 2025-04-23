using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;                // ScrollRect, LayoutRebuilder
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ShopWeaponManager : MonoBehaviour
{
    public static ShopWeaponManager Instance { get; private set; }

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

    /** 서버에서 내려온 아이템 캐시 */
    private List<WeaponItemData> weaponItems;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    #region 패널 오픈/데이터 로드 --------------------------------------------------
    public void OpenWeaponPanel()
    {
        weaponPopupCanvas?.SetActive(true);
        scrollContent.gameObject.SetActive(false);
        StartCoroutine(FetchThenShow());
    }

    public void CloseWeaponPanel() => weaponPopupCanvas?.SetActive(false);

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
    #endregion

    #region 리스트 → UI  ----------------------------------------------------------
    public void LoadData(List<WeaponItemData> items)
    {
        weaponItems = items;
        Populate();
    }

    private void Populate()
    {
        foreach (var hc in headerConfigs) ClearContent(hc.contentParent);

        if (weaponItems == null || weaponItems.Count == 0) return;

        foreach (var item in weaponItems)
        {
            var parent = GetParent(item.Header) ?? headerConfigs[0].contentParent;
            var go = Instantiate(itemPrefab, parent);
            var ctrl = go.GetComponent<SmallItemController>();

            // 6-인자 Setup : 버튼 클릭 시 PurchaseWeapon 호출
            ctrl?.Setup(item.ItemName, null, item.Price, item.CurrencyType,
                        item.ItemName, "Weapon");
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
    #endregion

    #region 구매 ------------------------------------------------------------------
    /** SmallItemController 에서 itemName 으로 호출 */
    public void PurchaseWeapon(string itemName, string currencyType)
    {
        var itemData = weaponItems?.Find(i => i.ItemName == itemName);
        if (itemData == null)
        {
            Debug.LogError($"[WeaponManager] Purchase 요청 실패 – '{itemName}' 캐시 미존재");
            return;
        }

        var requestData = new BuyGachaRequestData
        {
            PlayFabId = PlayerPrefs.GetString("PlayFabId"),
            CurrencyType = currencyType,
            ItemType = "Weapon",
            WeaponItemData = itemData          // ← 전체 ItemData 전송
        };

        StartCoroutine(SendBuyRequest(requestData));
    }

    private IEnumerator SendBuyRequest(BuyGachaRequestData data)
    {
        string json = JsonConvert.SerializeObject(data);
        Debug.Log($"[WeaponManager] ▶ Purchase JSON\n{json}");   // **요청 로그 추가**

        var req = new UnityWebRequest(purchaseWeaponUrl, "POST")
        {
            uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[WeaponManager] 요청 실패: {req.error}");
            yield break;
        }

        var resp = JsonConvert.DeserializeObject<BuyGachaResponseData>(req.downloadHandler.text);
        Debug.Log($"[WeaponManager] ◀ Response JSON\n{req.downloadHandler.text}"); // **응답 로그**

        if (resp != null && resp.IsSuccess)
            Debug.Log($"[WeaponManager] 구매 성공! 뽑힌 아이템 수: {resp.OwnedItemList.Count}");
        else
            Debug.LogWarning("[WeaponManager] 서버 처리 실패 (isSuccess == false)");
    }
    #endregion
}
