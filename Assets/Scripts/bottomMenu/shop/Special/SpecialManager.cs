using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class SpecialManager : MonoBehaviour
{
    [Header("스페셜 패널 (옵션)")]
    public GameObject specialPanel;       // 스페셜 메뉴 전체 패널

    [Header("아이템 배치용 Content Parent")]
    public Transform contentParent;       // ScrollView Content 등

    [Header("단일 아이템 Prefab")]
    public GameObject itemPrefab;         // SmallItemController가 붙어 있어야 함

    // 서버 API URL (코드에서 직접 관리)
    private const string fetchSpecialUrl = "https://YOUR_SERVER/api/GetSpecialData?code=YOUR_CODE_HERE";

    private List<SpecialItemData> specialItems;

    /// <summary>
    /// 스페셜 패널 열기 + 서버 데이터 요청
    /// </summary>
    public void OpenSpecialPanel()
    {
        if (specialPanel != null)
            specialPanel.SetActive(true);

        // UI 초기화 후 데이터 로드
        ClearContent();
        StartCoroutine(FetchSpecialDataCoroutine());
    }

    /// <summary>
    /// 스페셜 패널 닫기
    /// </summary>
    public void CloseSpecialPanel()
    {
        if (specialPanel != null)
            specialPanel.SetActive(false);
    }

    /// <summary>
    /// 서버에서 스페셜 아이템 데이터를 가져오는 코루틴
    /// </summary>
    private IEnumerator FetchSpecialDataCoroutine()
    {
        byte[] body = System.Text.Encoding.UTF8.GetBytes("{}");
        using var req = new UnityWebRequest(fetchSpecialUrl, "POST")
        {
            uploadHandler = new UploadHandlerRaw(body),
            downloadHandler = new DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[SpecialManager] 데이터 조회 실패: {req.error}");
            yield break;
        }

        SpecialResponseData resp = null;
        try
        {
            resp = JsonConvert.DeserializeObject<SpecialResponseData>(req.downloadHandler.text);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SpecialManager] JSON 파싱 오류: {ex.Message}");
            yield break;
        }

        if (resp == null || !resp.IsSuccess)
        {
            Debug.LogWarning("[SpecialManager] 유효하지 않은 응답");
            yield break;
        }

        // 데이터 저장 및 UI 구성
        specialItems = resp.SpecialItemList;
        PopulateSpecialItems();
    }

    /// <summary>
    /// 받아온 데이터를 기반으로 아이템 Prefab을 Instantiate하여 UI 배치
    /// </summary>
    private void PopulateSpecialItems()
    {
        if (specialItems == null || specialItems.Count == 0)
            return;

        foreach (var item in specialItems)
        {
            if (itemPrefab == null || contentParent == null)
                break;

            var go = Instantiate(itemPrefab, contentParent);
            var ctrl = go.GetComponent<SmallItemController>();
            if (ctrl != null)
            {
                // Title, null(sprite), Price, 통화 타입 미사용(빈 문자열)
                ctrl.Setup(item.Title, null, item.Price, string.Empty);
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
}
