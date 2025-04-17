using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class WeaponManager : MonoBehaviour
{
    [Header("무기 팝업 Panel/Canvas")]
    public GameObject weaponPopupCanvas;

    [System.Serializable]
    public class HeaderConfig
    {
        public string headerName;       // 서버 DTO의 item.Header와 매칭
        public Transform contentParent; // 해당 헤더 아래에 Item 프리팹을 배치
    }
    [Header("헤더 + Content Parent 매핑")]
    public HeaderConfig[] headerConfigs;

    [Header("단일 아이템 프리팹")]
    public GameObject itemPrefab;      // SmallItemController 가 붙어 있는 프리팹

    // 서버 API URL (직접 코드에서 관리)
    private const string fetchWeaponUrl = "https://pandaraisegame-shop.azurewebsites.net/api/GetWeaponGachaData?code=LbwpDyYAz2G1OE94wg-oUgI5VHYlmOY54oFUWFTUiJ7PAzFuafMI_g==";

    private List<WeaponItemData> weaponItems;

    /// <summary> 무기 패널 열기 + 서버 데이터 요청 </summary>
    public void OpenWeaponPanel()
    {
        if (weaponPopupCanvas != null)
            weaponPopupCanvas.SetActive(true);
        StartCoroutine(FetchWeaponDataCoroutine());
    }

    /// <summary> 무기 패널 닫기 </summary>
    public void CloseWeaponPanel()
    {
        if (weaponPopupCanvas != null)
            weaponPopupCanvas.SetActive(false);
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
            Debug.LogError($"[WeaponManager] 데이터 조회 실패: {req.error}");
            yield break;
        }

        WeaponResponseData resp = null;
        try
        {
            resp = JsonConvert.DeserializeObject<WeaponResponseData>(req.downloadHandler.text);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[WeaponManager] JSON 파싱 오류: {ex.Message}");
            yield break;
        }

        if (resp == null || !resp.IsSuccess)
        {
            Debug.LogError("[WeaponManager] 응답 이상");
            yield break;
        }

        LoadData(resp.WeaponItemList);
    }

    /// <summary> DTO 리스트를 저장하고 UI 갱신 </summary>
    public void LoadData(List<WeaponItemData> items)
    {
        weaponItems = items;
        PopulateWeaponItems();
    }

    private void PopulateWeaponItems()
    {
        // 1) 기존 콘텐츠 비우기
        foreach (var hc in headerConfigs)
            ClearContent(hc.contentParent);

        if (weaponItems == null || weaponItems.Count == 0)
        {
            Debug.LogWarning("[WeaponManager] 아이템 목록이 없습니다.");
            return;
        }

        // 2) 헤더 매칭 후 단일 프리팹 Instantiate + Setup 호출
        foreach (var item in weaponItems)
        {
            Transform parent = GetContentParent(item.Header);
            if (parent == null || itemPrefab == null) continue;

            GameObject go = Instantiate(itemPrefab, parent);
            var ctrl = go.GetComponent<SmallItemController>();
            if (ctrl != null)
            {
                // TODO: item.ImageUrl이 있으면 여기에 로드해서 넘겨주세요.
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
        Debug.LogWarning($"[WeaponManager] 헤더 매칭 실패: {header}");
        return null;
    }

    private void ClearContent(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}
