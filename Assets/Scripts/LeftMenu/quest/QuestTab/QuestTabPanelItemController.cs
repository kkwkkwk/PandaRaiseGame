using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestTabPanelItemController : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI infoText; // 퀘스트 내용 표시에 사용
    public Button actionButton;      // (옵션) 버튼이 필요하다면 연결

    /// <summary>
    /// 외부에서 QuestData를 받아 이 프리팹의 UI를 업데이트한다.
    /// </summary>
    /// <param name="data">표시할 퀘스트 정보(문자열)</param>
    public void SetData(QuestData data)
    {
        // questInfo를 UI에 반영
        if (infoText != null)
        {
            infoText.text = data.questInfo;
        }
        else
        {
            Debug.LogWarning($"[QuestTabPanelItemController] infoText가 할당되지 않았습니다. (info: {data.questInfo})");
        }

        // 버튼이 연결되어 있다면, 클릭 이벤트 설정
        if (actionButton != null)
        {
            // 기존 이벤트 리스너 제거(중복 방지)
            actionButton.onClick.RemoveAllListeners();

            // 클릭 시 실제 동작
            actionButton.onClick.AddListener(() => OnClickQuestAction(data));
        }
        else
        {
            Debug.LogWarning("[QuestTabPanelItemController] actionButton이 할당되지 않았습니다. 버튼 기능 없음.");
        }
    }

    /// <summary>
    /// 버튼 클릭 시 호출되는 메서드.
    /// </summary>
    /// <param name="data">현재 퀘스트 정보</param>
    private void OnClickQuestAction(QuestData data)
    {
        // 실제로는 퀘스트 완료, 상세 팝업 열기 등 원하는 로직을 넣으면 된다.
        Debug.Log($"[QuestTabPanelItemController] '{data.questInfo}' 버튼이 클릭되었습니다.");
    }
}
