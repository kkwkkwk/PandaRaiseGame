using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenQuestPopup : MonoBehaviour
{
    public void OnClickOpenQuestPanel() // 퀘스트 버튼 클릭
    {
        QuestPopupManager.Instance.OpenQuestPopup();
    }
}
