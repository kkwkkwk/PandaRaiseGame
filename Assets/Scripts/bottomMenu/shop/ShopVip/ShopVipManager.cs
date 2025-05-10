using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ShopVipManager : MonoBehaviour
{
    // 싱글턴 인스턴스 추가
    public static ShopVipManager Instance { get; private set; }

    [Header("VIP 패널 (옵션)")]
    public GameObject vipPanel;          // VIP 메뉴 전체 패널

    [Header("아이템 배치용 Content Parent")]
    public Transform contentParent;      // ScrollView Content 등

    [Header("단일 아이템 Prefab")]
    public GameObject itemPrefab;        // SmallItemController가 붙어 있어야 함

    // 서버 API URL (코드에서 직접 관리)
    private const string fetchVipUrl = "https://YOUR_SERVER/api/GetVipData?code=YOUR_CODE_HERE";

    private List<VipItemData> vipItems;

    private void Awake()
    {
        // 싱글턴 초기화
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// VIP 패널 열기 + 서버 데이터 요청
    /// </summary>
    public void OpenVipPanel()
    {
        if (vipPanel != null)
            vipPanel.SetActive(true);

        // 기존 UI 초기화
        ClearContent();
        // 데이터 요청
        StartCoroutine(FetchVipDataCoroutine());
    }

    /// <summary>
    /// VIP 패널 닫기
    /// </summary>
    public void CloseVipPanel()
    {
        if (vipPanel != null)
            vipPanel.SetActive(false);
    }

    /// <summary>
    /// 서버에서 VIP 아이템 데이터를 가져오는 코루틴
    /// </summary>
    private IEnumerator FetchVipDataCoroutine()
    {
        byte[] body = System.Text.Encoding.UTF8.GetBytes("{}");
        using var req = new UnityWebRequest(fetchVipUrl, "POST")
        {
            uploadHandler = new UploadHandlerRaw(body),
            downloadHandler = new DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[VipManager] 데이터 조회 실패: {req.error}");
            yield break;
        }

        VipResponseData resp = null;
        try
        {
            resp = JsonConvert.DeserializeObject<VipResponseData>(req.downloadHandler.text);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[VipManager] JSON 파싱 오류: {ex.Message}");
            yield break;
        }

        if (resp == null || !resp.IsSuccess)
        {
            Debug.LogWarning("[VipManager] 유효하지 않은 응답");
            yield break;
        }

        // 데이터 저장 및 UI 구성
        vipItems = resp.VipItemList;
        PopulateVipItems();
    }

    /// <summary>
    /// 받아온 데이터를 기반으로 아이템 Prefab을 Instantiate하여 UI 배치
    /// </summary>
    private void PopulateVipItems()
    {
        if (vipItems == null || vipItems.Count == 0)
            return;

        foreach (var item in vipItems)
        {
            if (itemPrefab == null || contentParent == null)
                break;

            var go = Instantiate(itemPrefab, contentParent);
            var ctrl = go.GetComponent<SmallItemController>();
            if (ctrl != null)
            {
                // Title, null(sprite), Price, currencyType 전달
                ctrl.Setup(item.Title, null, item.Price, item.CurrencyType);
            }
        }
    }

    /// <summary>
    /// Content Parent의 자식 모두 제거
    /// </summary>
    private void ClearContent()
    {
        if (contentParent == null)
            return;

        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// LoadingScreenManager를 위한 StartSequence
    /// </summary>
    public IEnumerator StartSequence()
    {
        // 내부의 FetchVipDataCoroutine 을 그대로 호출
        yield return FetchVipDataCoroutine();
    }
}
