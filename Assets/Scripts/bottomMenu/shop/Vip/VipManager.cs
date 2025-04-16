using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class VipManager : MonoBehaviour
{
    [Header("VIP 팝업 Panel")]
    public GameObject vipPopupCanvas;  // VIP 메뉴 전체 패널 (Canvas)

    // 서버 API URL – 실제 주소로 교체 필요
    private string fetchVipUrl = "https://YOUR_SERVER/api/GetVipData?code=YOUR_CODE_HERE";

    // 현재 로드된 VIP 아이템 목록
    private List<VipItemData> vipItems;

    /// <summary>
    /// VIP 메뉴(패널)을 열고 서버에서 VIP 상품 정보를 요청한다.
    /// </summary>
    public void OpenVipPanel()
    {
        if (vipPopupCanvas != null)
            vipPopupCanvas.SetActive(true);

        // 서버 데이터 요청 (또는 상위 매니저에서 VipItemData 리스트를 직접 LoadData()해도 됨)
        StartCoroutine(FetchVipDataFromServer());

        Debug.Log("[VipManager] OpenVipPanel 호출");
    }

    /// <summary>
    /// VIP 메뉴(패널)을 닫는다.
    /// </summary>
    public void CloseVipPanel()
    {
        if (vipPopupCanvas != null)
            vipPopupCanvas.SetActive(false);

        Debug.Log("[VipManager] CloseVipPanel 호출");
    }

    /// <summary>
    /// 서버에서 VIP 아이템 데이터를 가져오는 코루틴.
    /// (Title, Price, currencyType만 받아서 UI의 세부 이미지/설명 등은 앱 내부에서 처리)
    /// </summary>
    private IEnumerator FetchVipDataFromServer()
    {
        Debug.Log("[VipManager] FetchVipDataFromServer 시작");

        string requestBody = "{}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestBody);

        using (UnityWebRequest request = new UnityWebRequest(fetchVipUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[VipManager] VIP 데이터 조회 실패: " + request.error);
                yield break;
            }

            string respJson = request.downloadHandler.text;
            Debug.Log("[VipManager] 서버 응답: " + respJson);

            VipResponseData resp = null;
            try
            {
                resp = JsonConvert.DeserializeObject<VipResponseData>(respJson);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[VipManager] JSON 파싱 오류: " + ex.Message);
                yield break;
            }

            if (resp == null || !resp.IsSuccess)
            {
                Debug.LogError("[VipManager] 응답이 이상함");
                yield break;
            }

            // 서버에서 받은 VIP 아이템 리스트 저장
            LoadData(resp.VipItemList);

            Debug.Log("[VipManager] FetchVipDataFromServer 종료");
        }
    }

    /// <summary>
    /// 서버나 외부에서 받은 VIP 아이템 리스트를 내부에 보관.
    /// 앱 내부에서는 title, price, currencyType을 활용해 UI를 구성.
    /// </summary>
    public void LoadData(List<VipItemData> items)
    {
        vipItems = items;
        Debug.Log($"[VipManager] LoadData() 호출됨, 개수={vipItems?.Count ?? 0}");

        // 여기서 UI 배치를 자동으로 하지 않는다 가정(사용자 요청에 따라).
        // 필요하면 PopulateVipItems() 같은 메서드에서 제목, 가격 표시만 처리할 수 있음.
    }
}
