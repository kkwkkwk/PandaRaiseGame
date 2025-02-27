using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestTabPanelItemController : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI questInfoText;  // 퀘스트 제목, 설명, 목표, 타입을 표시할 텍스트
    public Button actionButton;           // 액션 버튼 (예: 퀘스트 상세보기)

    public void SetData(QuestData quest)
    {
        if (questInfoText != null)
        {
            questInfoText.text = $"{quest.title}";
                //\n{quest.description}\n목표: {quest.goal}\n타입: {quest.resetType}";
        }
        else
        {
            Debug.LogWarning("[QuestTabPanelItemController] questInfoText가 할당되지 않았습니다.");
        }

        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() => OnQuestAction(quest));
        }
        else
        {
            Debug.LogWarning("[QuestTabPanelItemController] actionButton이 할당되지 않았습니다.");
        }
    }

    private void OnQuestAction(QuestData quest)
    {
        Debug.Log($"[QuestTabPanelItemController] '{quest.title}' 퀘스트 액션 버튼 클릭됨.");
    }
}
