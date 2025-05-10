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

    [Header("로딩 패널")]
    [Tooltip("서버 요청 중 표시할 로딩 UI")] public GameObject loadingPanel;

    [Header("가챠 결과 창 컨트롤러")]
    [Tooltip("GachaResultController를 인스펙터에서 연결하세요.")]
    public GachaResultController gachaResultController;

    private const string fetchWeaponUrl =
        "https://pandaraisegame-shop.azurewebsites.net/api/GetWeaponGachaData?code=LbwpDyYAz2G1OE94wg-oUgI5VHYlmOY54oFUWFTUiJ7PAzFuafMI_g==";
    private const string purchaseWeaponUrl =
        "https://pandaraisegame-shop.azurewebsites.net/api/BuyGachaItem?code=lwFZwNzXVVbEbzFgjxRpfbaTeGN0fOkKEfq5K0_dRRWvAzFuxgjhXQ==";

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
        foreach (var hc in headerConfigs)
            ClearContent(hc.contentParent);

        if (weaponItems == null || weaponItems.Count == 0)
            return;

        foreach (var item in weaponItems)
        {
            var parent = GetParent(item.Header) ?? headerConfigs[0].contentParent;
            var go = Instantiate(itemPrefab, parent);
            var ctrl = go.GetComponent<SmallItemController>();

            // WeaponItemData 객체 자체를 넘기도록 Setup 호출
            ctrl?.Setup(item.ItemName,
                        null,
                        item.Price,
                        item.CurrencyType,
                        item,
                        "Weapon");
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
    public void PurchaseWeapon(WeaponItemData itemData, string currencyType)
    {
        if (itemData == null)
        {
            Debug.LogError("[WeaponManager] Purchase 요청 실패 – itemData is null");
            return;
        }

        // ▶ 로딩 시작
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        var requestData = new BuyGachaRequestData
        {
            PlayFabId = GlobalData.playFabId,
            CurrencyType = itemData.CurrencyType,
            ItemType = "Weapon",
            WeaponItemData = itemData
        };

        StartCoroutine(SendBuyRequest(requestData));
    }

    private IEnumerator SendBuyRequest(BuyGachaRequestData data)
    {
        string json = JsonConvert.SerializeObject(data);
        Debug.Log($"[WeaponManager] ▶ Purchase JSON\n{json}");

        var req = new UnityWebRequest(purchaseWeaponUrl, "POST")
        {
            uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        // ▶ 로딩 종료
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[WeaponManager] 요청 실패: {req.error}");
            yield break;
        }

        var resp = JsonConvert.DeserializeObject<BuyGachaResponseData>(req.downloadHandler.text);
        Debug.Log($"[WeaponManager] ◀ Response JSON\n{req.downloadHandler.text}");

        if (resp != null && resp.IsSuccess)
        {
            Debug.Log($"[WeaponManager] 구매 성공! 뽑힌 아이템 수: {resp.OwnedItemList.Count}");
            if (gachaResultController != null)
                gachaResultController.ShowResults(resp.OwnedItemList);
        }
        else
            Debug.LogWarning("[WeaponManager] 서버 처리 실패 (isSuccess == false)");
    }

    // 서버 한번에 로딩용
    public IEnumerator StartSequence()
    {
        // UI 표시는 하지 않고 데이터만 가져오길 원한다면 FetchWeaponDataCoroutine()만 호출
        yield return FetchWeaponDataCoroutine();
    }
    #endregion
}
