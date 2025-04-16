using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;

/// <summary>
/// 무기(뽑기 하위메뉴) 전용 매니저.
/// 여러 헤더(TitleBar) + 그 아래 아이템 Content 가 한 쌍씩 있는 구조를
/// 배열(headerConfigs[])로 관리하여, 서버에서 받은 item.Header에 맞게 배치.
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("무기(Weapon) 팝업 Panel/Canvas")]
    public GameObject weaponPopupCanvas; // 무기 메뉴 전체 패널

    // (옵션) 서버 API URL
    private string fetchWeaponUrl = "https://YOUR_SERVER/api/GetWeaponData?code=YOUR_CODE_HERE";

    // 무기 아이템 리스트 (header, itemName, price, currencyType 등)
    private List<WeaponItemData> weaponItems;

    /// <summary>
    /// Inspector에서 헤더 이름과 배치될 Content Transform을 등록하기 위한 구조체
    /// 예: Header("일일 광고") → contentParent(일일 광고용 영역)
    /// </summary>
    [System.Serializable]
    public class HeaderConfig
    {
        public string headerName;       // 서버에서 item.Header와 동일한 문자열
        public Transform contentParent; // 해당 헤더 아래 아이템들이 배치될 ScrollView Content
        // (옵션) public GameObject titleBar; 
        //        Inspector에 등록된 TitleBar 오브젝트를 활성화/비활성화 할 수도 있음
    }

    [Header("헤더 + 컨텐츠 Parent 매핑 배열")]
    public HeaderConfig[] headerConfigs;

    [Header("아이템 프리팹(결제 수단별)")]
    public GameObject freeItemCardPrefab;     // currencyType=FREE
    public GameObject diamondItemCardPrefab;  // currencyType=DIAMOND
    public GameObject goldItemCardPrefab;     // currencyType=GC
    public GameObject wonItemCardPrefab;      // currencyType=WON (현금 결제 등)

    /// <summary>
    /// 무기 메뉴(팝업) 열고, 서버에서 데이터 가져오기(또는 상위에서 LoadData 직접).
    /// </summary>
    public void OpenWeaponPanel()
    {
        if (weaponPopupCanvas != null)
            weaponPopupCanvas.SetActive(true);

        StartCoroutine(FetchWeaponDataFromServer());
        Debug.Log("[WeaponManager] OpenWeaponPanel 호출");
    }

    /// <summary>
    /// 무기 메뉴(팝업) 닫기
    /// </summary>
    public void CloseWeaponPanel()
    {
        if (weaponPopupCanvas != null)
            weaponPopupCanvas.SetActive(false);

        Debug.Log("[WeaponManager] CloseWeaponPanel 호출");
    }

    /// <summary>
    /// 서버에서 무기 아이템 목록(여러 헤더 포함)을 받아오는 코루틴.
    /// </summary>
    private IEnumerator FetchWeaponDataFromServer()
    {
        Debug.Log("[WeaponManager] FetchWeaponDataFromServer 시작");

        string requestBody = "{}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestBody);

        using (UnityWebRequest request = new UnityWebRequest(fetchWeaponUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[WeaponManager] 무기 데이터 조회 실패: " + request.error);
                yield break;
            }

            string respJson = request.downloadHandler.text;
            Debug.Log("[WeaponManager] 서버 응답: " + respJson);

            WeaponResponseData resp = null;
            try
            {
                resp = JsonConvert.DeserializeObject<WeaponResponseData>(respJson);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[WeaponManager] JSON 파싱 오류: " + ex.Message);
                yield break;
            }

            if (resp == null || !resp.IsSuccess)
            {
                Debug.LogError("[WeaponManager] 응답이 이상함");
                yield break;
            }

            // UI 배치를 위해 데이터 저장
            LoadData(resp.WeaponItemList);

            Debug.Log("[WeaponManager] FetchWeaponDataFromServer 종료");
        }
    }

    /// <summary>
    /// 외부(또는 서버)에서 받은 무기 아이템 리스트(여러 헤더)를 내부에 저장 후, UI 배치
    /// </summary>
    public void LoadData(List<WeaponItemData> items)
    {
        weaponItems = items;
        PopulateWeaponItems();
    }

    /// <summary>
    /// 기존 UI(Content Parent들)를 Clear한 뒤, item.Header에 따라 각각 배치
    /// </summary>
    private void PopulateWeaponItems()
    {
        // 1) 헤더별 컨텐츠를 전부 Clear
        foreach (var hc in headerConfigs)
        {
            if (hc.contentParent != null)
                ClearContent(hc.contentParent);
        }

        if (weaponItems == null || weaponItems.Count == 0)
        {
            Debug.LogWarning("[WeaponManager] 무기 아이템 목록이 비어있습니다.");
            return;
        }

        // 2) 각 아이템을 header에 맞춰 Instantiate
        foreach (var item in weaponItems)
        {
            // 헤더 매핑
            Transform targetParent = GetContentParentByHeader(item.Header);

            // 결제수단별 프리팹 결정
            GameObject prefab = GetPrefabByCurrency(item.CurrencyType);

            if (targetParent == null || prefab == null)
                continue;

            // Instantiate 프리팹
            GameObject cardObj = Instantiate(prefab, targetParent);
            // Layout Group에 의해 자동 정렬
            // option: cardObj.GetComponent<RectTransform>().localScale = Vector3.one;

            // 간단히 "ItemNameText", "PriceText"에만 값을 적용
            var nameText = cardObj.transform.Find("ItemNameText")?.GetComponent<TMPro.TextMeshProUGUI>();
            var priceText = cardObj.transform.Find("PriceText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (nameText != null) nameText.text = item.ItemName;
            if (priceText != null) priceText.text = $"{item.Price} ({item.CurrencyType})";
        }
    }

    /// <summary>
    /// item.Header 문자열에 대응하는 headerConfigs 배열 내 contentParent를 찾는다.
    /// 못 찾으면 null 반환(또는 default Content 사용).
    /// </summary>
    private Transform GetContentParentByHeader(string header)
    {
        if (string.IsNullOrEmpty(header)) return null;
        // 필요하다면 default parent etc.

        // headerConfigs에 headerName이 일치하는 항목 찾아 반환
        foreach (var hc in headerConfigs)
        {
            if (!string.IsNullOrEmpty(hc.headerName) && header.Contains(hc.headerName))
            {
                return hc.contentParent;
            }
        }

        Debug.LogWarning($"[WeaponManager] '{header}'에 해당하는 헤더를 찾지 못했습니다.");
        return null;
    }

    /// <summary>
    /// 결제수단(currencyType)에 맞는 프리팹
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
                Debug.LogWarning($"[WeaponManager] 알 수 없는 CurrencyType: {currency}");
                return null;
        }
    }

    /// <summary>
    /// 특정 Transform 아래의 자식 오브젝트들을 모두 제거
    /// </summary>
    private void ClearContent(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
}
