using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class SpecialManager : MonoBehaviour
{
    [Header("스페셜 패널 (옵션)")]
    public GameObject specialPopupCanvas;  // 스페셜 메뉴 전체 패널

    // (옵션) 서버 API URL
    private string fetchSpecialUrl = "https://YOUR_SERVER/api/GetSpecialData?code=YOUR_CODE_HERE";

    // 현재 로드된 스페셜 아이템 리스트 (Title, Price)
    private List<SpecialItemData> specialItems;

    /// <summary>
    /// 스페셜 메뉴(패널)를 열고, 서버에서 스페셜 상품 정보를 요청한다.
    /// </summary>
    public void OpenSpecialPanel()
    {
        if (specialPopupCanvas != null)
            specialPopupCanvas.SetActive(true);

        // 서버 데이터 가져오기 (혹은 상위 매니저에서 LoadData() 직접 호출 가능)
        StartCoroutine(FetchSpecialDataFromServer());

        Debug.Log("[SpecialManager] OpenSpecialPanel 호출");
    }

    /// <summary>
    /// 스페셜 메뉴(패널)를 닫는다.
    /// </summary>
    public void CloseSpecialPanel()
    {
        if (specialPopupCanvas != null)
            specialPopupCanvas.SetActive(false);

        Debug.Log("[SpecialManager] CloseSpecialPanel 호출");
    }

    /// <summary>
    /// 서버에서 스페셜 아이템 데이터를 가져오는 코루틴.
    /// (제목, 가격만 받아서 앱 내에서 이미지·세부내용 3개는 별도로 처리한다고 가정)
    /// </summary>
    private IEnumerator FetchSpecialDataFromServer()
    {
        Debug.Log("[SpecialManager] FetchSpecialDataFromServer 시작");

        string requestBody = "{}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestBody);

        using (UnityWebRequest request = new UnityWebRequest(fetchSpecialUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[SpecialManager] 스페셜 데이터 조회 실패: " + request.error);
                yield break;
            }

            string respJson = request.downloadHandler.text;
            Debug.Log("[SpecialManager] 서버 응답: " + respJson);

            SpecialResponseData resp = null;
            try
            {
                resp = JsonConvert.DeserializeObject<SpecialResponseData>(respJson);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[SpecialManager] JSON 파싱 오류: " + ex.Message);
                yield break;
            }

            if (resp == null || !resp.IsSuccess)
            {
                Debug.LogError("[SpecialManager] 응답이 이상함");
                yield break;
            }

            // 서버에서 받은 스페셜 아이템 리스트 저장
            LoadData(resp.SpecialItemList);

            Debug.Log("[SpecialManager] FetchSpecialDataFromServer 종료");
        }
    }

    /// <summary>
    /// 서버나 외부에서 받은 스페셜 아이템 목록(제목, 가격)을 내부에 저장.
    /// UI 배치는 앱 내부에서 이미지, 3개 라인 등 별도로 처리하므로 여기서는 보관만 수행.
    /// </summary>
    public void LoadData(List<SpecialItemData> items)
    {
        specialItems = items;
        Debug.Log($"[SpecialManager] LoadData() 호출됨, 개수={specialItems?.Count ?? 0}");

        // 여기서 UI를 자동으로 구성하지 않는다면, 
        // 앱 내부 다른 코드에서 specialItems를 참조해 필요한 부분만 표시할 수 있습니다.
        // 예: PopulateSpecialItems() 등으로 직접 배치 가능
    }
}
