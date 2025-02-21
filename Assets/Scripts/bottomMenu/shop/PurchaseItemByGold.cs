using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json; // JSON 직렬화 라이브러리
using PlayFab;
using PlayFab.ClientModels;

public class PurchaseItemByGold : MonoBehaviour
{

    public Button purchaseButton; // 유니티 버튼 참조
    private string PlayFabId; // // PlayFab 사용자 ID
    private string itemId = "itemtest_001"; // 구매할 아이템의 ID
    private string azureFunctionUrl = "https://pandaraisegame-shop.azurewebsites.net/api/PurchaseWithGold?code=9WFQxHRVtEk1XPtR1BWCqIw84ySflr7ba1LsiEMH-TC7AzFumhpR4A=="; // Azure Function URL

    // Start is called before the first frame update
    void Start()
    {
        if (!string.IsNullOrEmpty(GlobalData.playFabId)) // 플레이팹 id 가져오기
        {
            PlayFabId = GlobalData.playFabId;
            Debug.Log("PlayFab ID 가져오기 성공: " + PlayFabId);
        }
        else
        {
            Debug.LogError("PlayFab ID가 설정되지 않았습니다.");
        }

        // 버튼 클릭 이벤트 등록
        purchaseButton.onClick.AddListener(() => BuyItem());
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
}
