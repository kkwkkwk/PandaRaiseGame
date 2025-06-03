using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonPopupManager : MonoBehaviour
{
    // -------------------------------------------------
    // 1) 싱글턴 인스턴스
    // -------------------------------------------------
    public static DungeonPopupManager Instance { get; private set; }

    // -------------------------------------------------
    // 2) Inspector에서 연결할 UI 오브젝트 참조
    // -------------------------------------------------
    [Header("팝업 전체 Canvas (최상위)")]
    [Tooltip("던전 팝업의 최상위 Canvas/GameObject 입니다.\n예: DungeonPopupScreen_Panel")]
    public GameObject dungeonPopupCanvas;

    [Header("던전 입장(Entrance) 패널")]
    [Tooltip("던전 입구 UI가 들어있는 패널 (예: DungeonEntrance_Panel)")]
    public GameObject dungeonEntrancePanel;

    [Header("던전 스테이지(Stage) 패널")]
    [Tooltip("실제 던전 스테이지가 표시되는 패널 (예: DungeonStage_Panel)")]
    public GameObject dungeonStagePanel;

    // -------------------------------------------------
    // 3) 싱글턴 세팅
    // -------------------------------------------------
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 다른 씬으로 넘어가도 삭제되지 않게 하려면 아래 주석 해제
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // 시작 시에는 팝업 전체를 비활성화
        if (dungeonPopupCanvas != null)
            dungeonPopupCanvas.SetActive(false);

        // 입장/스테이지 패널도 모두 꺼 두기 (혹시 켜져 있으면 숨김)
        if (dungeonEntrancePanel != null)
            dungeonEntrancePanel.SetActive(false);
        if (dungeonStagePanel != null)
            dungeonStagePanel.SetActive(false);
    }

    // -------------------------------------------------
    // 4) “던전 버튼(메인 화면)” → 던전 팝업 열기
    // -------------------------------------------------
    /// <summary>
    /// 메인 화면의 “던전 버튼”을 눌렀을 때 호출합니다.
    /// </summary>
    public void OpenDungeonPanel()
    {
        if (dungeonPopupCanvas == null) return;

        // 1) 팝업 전체 켜기
        dungeonPopupCanvas.SetActive(true);

        // 2) “입장 패널” (Entrance)만 활성화
        if (dungeonEntrancePanel != null)
            dungeonEntrancePanel.SetActive(true);

        // 3) “스테이지 패널”은 꺼 두기
        if (dungeonStagePanel != null)
            dungeonStagePanel.SetActive(false);

        // 기존에 하던 ChatPopupManager, 다른 팝업 닫는 로직 그대로 유지
        if (WeaponPopupManager.Instance != null)
            WeaponPopupManager.Instance.CloseWeaponPanel();
        if (SkillPopupManager.Instance != null)
            SkillPopupManager.Instance.CloseSkillPanel();
        if (ArmorPopupManager.Instance != null)
            ArmorPopupManager.Instance.CloseArmorPanel();

        if (ChatPopupManager.Instance != null)
        {
            ChatPopupManager.Instance.OpenMiddleChatPreview();
            ChatPopupManager.Instance.CloseBottomChatPreview();
            ChatPopupManager.Instance.middlePreview = true;
        }
    }

    /// <summary>
    /// 던전 팝업 전체를 닫고, 내부의 모든 서브 패널도 비활성화합니다.
    /// </summary>
    public void CloseDungeonPanel()
    {
        if (dungeonPopupCanvas != null)
            dungeonPopupCanvas.SetActive(false);

        if (dungeonEntrancePanel != null)
            dungeonEntrancePanel.SetActive(false);

        if (dungeonStagePanel != null)
            dungeonStagePanel.SetActive(false);

        // ChatPopup 복원 로직
        if (ChatPopupManager.Instance != null)
        {
            ChatPopupManager.Instance.CloseMiddleChatPreview();
            ChatPopupManager.Instance.OpenBottomChatPreview();
            ChatPopupManager.Instance.middlePreview = false;
        }
    }

    // -------------------------------------------------
    // 5) “던전 입장” 버튼을 눌렀을 때 (Entrance → Stage 전환)
    // -------------------------------------------------
    /// <summary>
    /// 던전 입장 패널 안의 “던전 입장” 버튼을 눌렀을 때, 
    /// Entrance 패널은 숨기고 Stage 패널을 켜며 실제 Load 로직을 호출합니다.
    /// (DungeonManager.TryEnterDungeon() 를 호출해서 내부 티켓 차감 & 스테이지 로드)
    /// </summary>
    public void OnClickEnterDungeonButton()
    {
        // 1) DungeonManager 쪽 로직 수행 (티켓 차감, 데이터 갱신, 스테이지 씬 이동 등)
        if (DungeonManager.Instance != null)
        {
            DungeonManager.Instance.TryEnterDungeon();
        }
        else
        {
            Debug.LogWarning("DungeonManager 인스턴스를 찾을 수 없습니다!");
        }

        // 2) Entrance 패널 숨기기
        if (dungeonEntrancePanel != null)
            dungeonEntrancePanel.SetActive(false);

        // 3) Stage 패널 활성화
        if (dungeonStagePanel != null)
            dungeonStagePanel.SetActive(true);
    }
}
