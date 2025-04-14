using UnityEngine;
using System.Collections; // 코루틴
using UnityEngine.Networking; // UnityWebRequest
// using Newtonsoft.Json; // 만약 JsonUtility 대신 Newtonsoft를 쓴다면

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Shop Popup Panel")]
    public GameObject shopPopupPanel;

    [Header("Content Panels (ShopMain_Panel 내)")]
    public GameObject specialPanel;
    public GameObject currencyPanel;
    public GameObject gachaPanel;
    public GameObject vipPanel;

    private bool isShopOpen = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ─────────────────────────────────────────
    // ToggleShop, OpenShop, CloseShop,
    // ShowSpecialPanel, ShowCurrencyPanel, etc.
    // ─────────────────────────────────────────

    #region 기존함수예시(참고)
    public void ToggleShop()
    {
        if (isShopOpen) CloseShop();
        else OpenShop();
    }

    public void OpenShop()
    {
        if (shopPopupPanel != null)
        {
            shopPopupPanel.SetActive(true);
            isShopOpen = true;
            ShowSpecialPanel();
            Debug.Log("Shop Popup Opened");

            // 상점 열 때 서버에서 최신 데이터 받아오고 싶다면
            StartCoroutine(CallGetShopData());
        }
        else
        {
            Debug.LogWarning("ShopPopupPanel reference가 할당되어 있지 않습니다!");
        }
    }

    public void CloseShop()
    {
        if (shopPopupPanel != null)
        {
            shopPopupPanel.SetActive(false);
            isShopOpen = false;
            Debug.Log("Shop Popup Closed");
        }
    }

    private void ShowSpecialPanel()
    {
        HideAllContentPanels();
        if (specialPanel != null)
        {
            specialPanel.SetActive(true);
            Debug.Log("Special Panel Opened (기본)");
        }
    }

    private void HideAllContentPanels()
    {
        if (specialPanel) specialPanel.SetActive(false);
        if (currencyPanel) currencyPanel.SetActive(false);
        if (gachaPanel) gachaPanel.SetActive(false);
        if (vipPanel) vipPanel.SetActive(false);
    }
    #endregion

    // ─────────────────────────────────────────
    // 서버 연동을 위한 필드와 코루틴
    // ─────────────────────────────────────────

    // 1) 서버 URL (백엔드에서 넘겨준 HTTP endpoint)
    [Header("Server Settings")]
    [Tooltip("상점 데이터를 가져올 API URL")]
    [SerializeField]
    private string getShopUrl = "https://pandaraisegame-shop.azurewebsites.net/api/GetShopData"; // 건우오빠한테 받은 url이 없어서 임시로 넣어놨어용

    // 2) 서버에서 받아온 DTO를 저장할 변수
    private ShopServerData shopData;

    /// <summary>
    /// 서버에서 상점 데이터를 받아오는 코루틴 함수
    /// </summary>
    private IEnumerator CallGetShopData()
    {
        Debug.Log("[ShopManager] CallGetShopData() 코루틴 시작");

        // (1) 요청에 보낼 JSON 객체 (PlayFabID 필요시 GlobalData.playFabId 등)
        var requestObj = new
        {
            // 예) playFabId = GlobalData.playFabId,
            //     otherParam = "something"
        };
        string jsonBody = JsonUtility.ToJson(requestObj); // 또는 Newtonsoft.Json 사용

        // (2) UnityWebRequest (POST)
        using (UnityWebRequest request = new UnityWebRequest(getShopUrl, "POST"))
        {
            // (3) 업로드/다운로드 핸들러
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            // (4) 헤더 설정
            request.SetRequestHeader("Content-Type", "application/json");
            Debug.Log("[ShopManager] 서버에 GetShopData 요청 전송 중...");

            // (5) 요청 전송
            yield return request.SendWebRequest();

            // (6) 결과 확인
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ShopManager] CallGetShopData() 실패: {request.error}");
            }
            else
            {
                string responseJson = request.downloadHandler.text;
                Debug.Log($"[ShopManager] 서버 응답 수신: {responseJson}");

                // (7) JSON -> ShopServerData 파싱
                shopData = JsonUtility.FromJson<ShopServerData>(responseJson);

                if (shopData == null)
                {
                    Debug.LogError("[ShopManager] shopData가 null입니다. JSON 형식을 확인하세요.");
                }
                else
                {
                    // (8) UI 반영
                    Debug.Log("[ShopManager] ShopServerData 파싱 완료, UI 업데이트 로직 실행 예정...");
                    // 예) 다이아 표시 텍스트, 상품 목록 세팅 등
                    // UpdateShopUI();
                }
            }
        }

        Debug.Log("[ShopManager] CallGetShopData() 코루틴 종료");
    }

    /// <summary>
    /// 예: 코루틴 완료 후 UI에 반영하는 함수 (추가로 작성 가능)
    /// </summary>
    private void UpdateShopUI()
    {
        if (shopData == null) return;

        // shopData.diamond, shopData.products 등으로 텍스트/UI 갱신
        // e.g. diamondText.text = shopData.diamond.ToString();
        // Instantiate item cards for shopData.products ...
    }
}
