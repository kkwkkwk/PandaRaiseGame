using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용
using System.Collections;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json; // JSON 직렬화 라이브러리
using PlayFab;
using PlayFab.ClientModels;

public class PurchaseItemByGold : MonoBehaviour
{

    public Button purchaseButton;           // 유니티 버튼 참조
    public TextMeshProUGUI goldText;        // 보유 골드 표시용
    public TextMeshProUGUI itemCountText;   // 아이템 개수 표시용

    private string PlayFabId; // // PlayFab 사용자 ID
    private string itemId = "itemtest_001"; // 구매할 아이템의 ID
    private string azureFunctionUrl = "https://pandaraisegame-shop.azurewebsites.net/api/PurchaseWithGold?code=6RKJD-VDGDnuvxg5v39tW_SR4XeIf24FJvbg9qau3-WaAzFubcVozg=="; // Azure Function URL

    private int currentGold = 0;
    private int itemCount = 0;

    // Start is called before the first frame update
    void Start()
    {
        if (!string.IsNullOrEmpty(GlobalData.playFabId)) // 플레이팹 id 가져오기
        {
            PlayFabId = GlobalData.playFabId;
            Debug.Log("PlayFab ID 가져오기 성공: " + PlayFabId);

            // 현재 보유 골드 및 아이템 개수 가져오기
            StartCoroutine(GetPlayerCurrency());
            StartCoroutine(GetPlayerInventory());
        }
        else
        {
            Debug.LogError("PlayFab ID가 설정되지 않았습니다.");
        }

        // 버튼 클릭 이벤트 등록
        purchaseButton.onClick.AddListener(() => BuyItem());
    }

    /// <summary>
    /// PlayFab에서 현재 보유 골드 조회
    /// </summary>
    private IEnumerator GetPlayerCurrency()
    {
        var request = new GetUserInventoryRequest();
        PlayFabClientAPI.GetUserInventory(request, result =>
        {
            currentGold = result.VirtualCurrency.ContainsKey("GC") ? result.VirtualCurrency["GC"] : 0;
            UpdateGoldUI();
        },
        error =>
        {
            Debug.LogError("골드 조회 실패: " + error.GenerateErrorReport());
        });

        yield return null;
    }

    /// <summary>
    /// PlayFab에서 현재 아이템 개수 조회
    /// </summary>
    private IEnumerator GetPlayerInventory()
    {
        var request = new GetUserInventoryRequest();
        PlayFabClientAPI.GetUserInventory(request, result =>
        {
            itemCount = 0;
            foreach (var item in result.Inventory)
            {
                if (item.ItemId == itemId)
                {
                    itemCount += item.RemainingUses ?? 1; // 아이템 개수 업데이트
                }
            }
            UpdateItemUI();
        },
        error =>
        {
            Debug.LogError("아이템 개수 조회 실패: " + error.GenerateErrorReport());
        });

        yield return null;
    }


    /// <summary>
    /// 아이템 구매 요청을 Azure Function으로 보내는 함수
    /// </summary>
    async void BuyItem()
    {
        // PlayFab ID가 없는 경우, 로그 출력 후 요청하지 않음
        if (string.IsNullOrEmpty(PlayFabId))
        {
            Debug.LogError("PlayFab ID가 설정되지 않았습니다. 로그인 후 다시 시도하세요.");
            return;
        }

        using (HttpClient client = new HttpClient())
        {
            // 요청 데이터 생성 (PlayFab ID와 아이템 ID 포함)
            var requestData = new
            {
                playFabId = PlayFabId,
                itemId = itemId
            };

            // JSON 데이터 직렬화
            string json = JsonConvert.SerializeObject(requestData);
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                // Azure Function에 비동기 HTTP POST 요청 전송
                HttpResponseMessage response = await client.PostAsync(azureFunctionUrl, content);
                string responseText = await response.Content.ReadAsStringAsync();

                // 응답 결과 확인
                if (response.IsSuccessStatusCode)
                {
                    Debug.Log($"아이템 구매 성공: {responseText}");

                    // 구매 성공 후 PlayFab에서 골드 및 아이템 개수 업데이트

                    // 0.3초 정도 대기 후 아이템 및 재화 조회
                    StartCoroutine(UpdateInventoryAfterDelay(0.3f));
                }
                else
                {
                    Debug.LogError($"아이템 구매 실패: {responseText}");
                }
            }
            catch (HttpRequestException e)
            {
                Debug.LogError($"HTTP 요청 실패: {e.Message}");
            }
        }
    }
    /// <summary>
    /// 아이템 구매시 잠시 대기
    /// </summary>
    private IEnumerator UpdateInventoryAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(GetPlayerCurrency());
        StartCoroutine(GetPlayerInventory());
    }
    /// <summary>
    /// UI에 현재 보유 골드 업데이트
    /// </summary>
    private void UpdateGoldUI()
    {
        if (goldText != null)
        {
            goldText.text = $"골드: {currentGold}";
        }
    }

    /// <summary>
    /// UI에 현재 아이템 개수 업데이트
    /// </summary>
    private void UpdateItemUI()
    {
        if (itemCountText != null)
        {
            itemCountText.text = $"아이템 개수: {itemCount}";
        }
    }
}
