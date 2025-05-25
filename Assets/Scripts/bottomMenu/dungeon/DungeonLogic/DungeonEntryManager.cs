using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class DungeonEntryController : MonoBehaviour
{
    [Header("입장 버튼")]
    public Button enterButton;

    [Header("부족 팝업")]
    public CanvasGroup popupCanvasGroup;  // 팝업 전체를 감싸는 CanvasGroup (알파 0~1)
    public TextMeshProUGUI popupText;
    public float popupDuration = 2f;

    void Awake()
    {
        // 버튼 이벤트 연결
        if (enterButton != null)
            enterButton.onClick.AddListener(OnEnterButtonClicked);

        // 처음엔 팝업 숨기기
        if (popupCanvasGroup != null)
            popupCanvasGroup.alpha = 0f;
    }

    void OnEnterButtonClicked()
    {
        // DT(던전티켓) 잔량 가져오기
        int ticketCount = 0;
        if (GlobalData.VirtualCurrencyBalances != null)
            GlobalData.VirtualCurrencyBalances.TryGetValue("DT", out ticketCount);

        if (ticketCount > 0)
        {
            // 티켓 차감 (클라이언트 동기화용)
            GlobalData.VirtualCurrencyBalances["DT"] = ticketCount - 1;

            // TODO: 필요하다면 서버에도 SubtractUserVirtualCurrency 요청

            // 던전 씬 또는 스테이지 로드
            SceneManager.LoadScene("DungeonScene");  // 씬 이름을 본인의 것으로 바꿔 주세요
        }
        else
        {
            // 티켓 부족 팝업
            StartCoroutine(ShowNotEnoughTicketPopup());
        }
    }

    IEnumerator ShowNotEnoughTicketPopup()
    {
        if (popupCanvasGroup == null || popupText == null)
            yield break;

        popupText.text = "던전에 입장하기 위한 티켓이 부족합니다.";
        // 팝업 보이기
        popupCanvasGroup.alpha = 1f;
        popupCanvasGroup.blocksRaycasts = true;

        yield return new WaitForSeconds(popupDuration);

        // 팝업 숨기기
        popupCanvasGroup.alpha = 0f;
        popupCanvasGroup.blocksRaycasts = false;
    }
}
