using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class SkillManager : MonoBehaviour
{
    // 1) 싱글톤 인스턴스
    public static SkillManager Instance { get; private set; }

    [Header("스킬 팝업 Panel/Canvas")]
    public GameObject skillPopupCanvas;

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

    private const string fetchSkillUrl = "https://pandaraisegame-shop.azurewebsites.net/api/GetSkillGachaData?code=5TkJ9Ck9okV81RdtuQA8jUEEEUDm37fq5owAnfIE-hBPAzFurDM8bA==";

    private List<SkillItemData> skillItems;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OpenSkillPanel()
    {
        if (skillPopupCanvas != null)
            skillPopupCanvas.SetActive(true);
        StartCoroutine(FetchSkillDataCoroutine());
    }

    public void CloseSkillPanel()
    {
        if (skillPopupCanvas != null)
            skillPopupCanvas.SetActive(false);
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
            Debug.LogError($"[SkillManager] 데이터 조회 실패: {req.error}");
            yield break;
        }

        SkillResponseData resp = null;
        try
        {
            resp = JsonConvert.DeserializeObject<SkillResponseData>(req.downloadHandler.text);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SkillManager] JSON 파싱 오류: {ex.Message}");
            yield break;
        }

        if (resp == null || !resp.IsSuccess)
        {
            Debug.LogError("[SkillManager] 응답 이상");
            yield break;
        }

        LoadData(resp.SkillItemList);
    }

    public void LoadData(List<SkillItemData> items)
    {
        skillItems = items;
        PopulateSkillItems();
    }

    private void PopulateSkillItems()
    {
        foreach (var hc in headerConfigs)
            ClearContent(hc.contentParent);

        if (skillItems == null || skillItems.Count == 0)
        {
            Debug.LogWarning("[SkillManager] 아이템 목록이 없습니다.");
            return;
        }

        foreach (var item in skillItems)
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
        Debug.LogWarning($"[SkillManager] 헤더 매칭 실패: {header}");
        return null;
    }

    private void ClearContent(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}
