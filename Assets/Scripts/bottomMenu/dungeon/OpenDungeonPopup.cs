using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenDungeonPopup : MonoBehaviour
{
    [Header("––– 팝업 패널 참조 –––")]
    [Tooltip("최상위 던전 팝업 Canvas")]
    [SerializeField] private GameObject dungeonPopupCanvas;
    [Tooltip("던전 입장 패널")]
    [SerializeField] private GameObject dungeonEntrancePanel;
    [Tooltip("던전 스테이지 선택 패널")]
    [SerializeField] private GameObject dungeonStagePanel;

    public void OnClickOpenPanel()
    {
        Debug.Log("▶▶▶ Dungeon Button Clicked");

        // 1) 혹시 열려있는 Chat 풀스크린 있으면 닫기
        if (ChatPopupManager.Instance != null && ChatPopupManager.Instance.isFullScreenActive)
            ChatPopupManager.Instance.CloseChat();

        // 2) 직접 할당한 패널 활성화
        if (dungeonPopupCanvas != null) dungeonPopupCanvas.SetActive(true);
        else Debug.LogWarning("dungeonPopupCanvas가 할당되지 않았습니다!");

        if (dungeonEntrancePanel != null) dungeonEntrancePanel.SetActive(true);
        else Debug.LogWarning("dungeonEntrancePanel이 할당되지 않았습니다!");

        if (dungeonStagePanel != null) dungeonStagePanel.SetActive(false);
        else Debug.LogWarning("dungeonStagePanel이 할당되지 않았습니다!");
    }
}
