using System.Collections;
using UnityEngine;
using TMPro;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;

    [Header("Stage Data List")]
    public StageData[] stageDatas;

    private int currentChapterIndex = 0;
    private StageData currentStageData;
    private int currentSubStage = 1;
    private int killCount = 0;
    private SubStageBlock currentBlock;

    [Header("UI References")]
    public TextMeshProUGUI stageInfoText;

    [Header("Stage Panels")]
    public GameObject idleModePanel;
    public GameObject bossModePanel;

    [Header("Spawner Objects")]
    public GameObject mobSpawner;
    public GameObject bossSpawner;

    [Header("Boss Button")]
    public GameObject bossButton;

    // ─────────────────────────────────────────────
    // ↓↓↓ 디버그용 필드 ↓↓↓
    // ─────────────────────────────────────────────
    [Header("Debug (인스펙터에서만 사용)")]
    [Tooltip("예: \"1,5\" 입력 후 아래 체크박스를 켜면, 1-5로 이동")]
    public string debugStageString = "1,1";   // "챕터,서브스테이지" 형식
    [Tooltip("이 토글을 켜면, OnValidate()에서 debugStageString을 해석해 DebugSetStage를 실행")]
    public bool debugApplyStage = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (stageDatas == null || stageDatas.Length == 0)
        {
            Debug.LogError("[StageManager] stageDatas 배열이 비어있습니다!");
            return;
        }
        LoadChapter(0);
    }

    // ─────────────────────────────────────────────
    // ↓↓↓ 디버그용 OnValidate() ↓↓↓
    // ─────────────────────────────────────────────
    private void OnValidate()
    {
        // 유니티 에디터에서 Inspector 값이 변경될 때마다 호출됩니다.
        // Play Mode가 아닐 때 DebugSetStage를 호출해봐야 씬 플레이 상태가 아니므로 의미가 없지만,
        // "Play Mode 중"에도 Inspector 값을 바꾸면 OnValidate()가 호출되므로 사용할 수 있습니다.
        if (debugApplyStage)
        {
            debugApplyStage = false; // 다시 꺼줌

            if (!Application.isPlaying)
            {
                Debug.LogWarning("[StageManager] DebugSetStage는 Play Mode에서만 동작합니다!");
                return;
            }

            // "1,5" 같은 문자열을 파싱
            if (string.IsNullOrEmpty(debugStageString))
            {
                Debug.LogWarning("[StageManager] debugStageString이 비어있습니다.");
                return;
            }

            string[] parts = debugStageString.Split(',');
            if (parts.Length != 2)
            {
                Debug.LogError($"[StageManager] debugStageString='{debugStageString}' 형식이 잘못되었습니다. 예: \"1,5\"");
                return;
            }

            if (int.TryParse(parts[0], out int ch) && int.TryParse(parts[1], out int subSt))
            {
                Debug.Log($"[StageManager] OnValidate → DebugSetStage({ch}, {subSt}) 호출");
                DebugSetStage(ch, subSt);
            }
            else
            {
                Debug.LogError($"[StageManager] debugStageString='{debugStageString}' 파싱 실패");
            }
        }
    }
    // ─────────────────────────────────────────────

    // ─────────────────────────────────────────────
    // ↓↓↓ DebugSetStage 함수 ↓↓↓
    // ─────────────────────────────────────────────
    /// <summary>
    /// 디버그용: 지정한 챕터와 서브스테이지부터 시작하도록 설정합니다.
    /// 예: DebugSetStage(1, 5) -> 1-5부터 시작.
    /// </summary>
    public void DebugSetStage(int chapter, int subStage)
    {
        if (chapter < 1 || chapter > stageDatas.Length)
        {
            Debug.LogError("[StageManager] DebugSetStage: 유효하지 않은 챕터 번호");
            return;
        }

        currentChapterIndex = chapter - 1;
        currentStageData = stageDatas[currentChapterIndex];

        if (subStage < 1 || subStage > currentStageData.totalSubStages)
        {
            Debug.LogError("[StageManager] DebugSetStage: 유효하지 않은 서브스테이지 번호");
            return;
        }

        currentSubStage = subStage;
        killCount = 0;

        UpdateCurrentBlock();
        UpdateStageInfo();

        // 현재 스테이지가 보스 스테이지라면 보스 모드, 아니라면 일반 모드
        if (IsBossStage(currentSubStage))
        {
            GoBossMode();
        }
        else
        {
            GoIdleMode();
        }

        Debug.Log($"[StageManager] DebugSetStage({chapter},{subStage}) 완료. 현재 스테이지 = {currentStageData.chapter}-{currentSubStage}");
    }
    // ─────────────────────────────────────────────

    private void LoadChapter(int index)
    {
        currentChapterIndex = index;
        currentStageData = stageDatas[index];

        currentSubStage = 1;
        killCount = 0;

        UpdateCurrentBlock();
        UpdateStageInfo();

        if (bossButton != null) bossButton.SetActive(false);
        GoIdleMode();
    }

    public void OnEnemyKilled()
    {
        if (IsBossStage(currentSubStage)) return;

        killCount++;
        Debug.Log($"[StageManager] OnEnemyKilled: {killCount}/{currentBlock.killGoal} " +
                  $"(SubStage {currentSubStage}/{currentStageData.totalSubStages})");

        if (killCount >= currentBlock.killGoal)
        {
            killCount = 0;
            currentSubStage++;

            if (currentSubStage > currentStageData.totalSubStages)
            {
                GoNextChapter();
                return;
            }

            if (IsBossStage(currentSubStage))
            {
                if (bossButton != null) bossButton.SetActive(false);
                GoBossMode();
            }
            else
            {
                GoIdleMode();
            }

            UpdateCurrentBlock();
            UpdateStageInfo();
        }
    }

    public void OnBossVictory()
    {
        Debug.Log("[StageManager] OnBossVictory() - 보스 처치 성공!");

        currentSubStage++;
        if (currentSubStage > currentStageData.totalSubStages)
        {
            GoNextChapter();
            return;
        }

        killCount = 0;
        UpdateCurrentBlock();
        UpdateStageInfo();

        if (bossButton != null) bossButton.SetActive(false);
        GoIdleMode();
    }

    public void OnBossDefeat()
    {
        Debug.Log("[StageManager] OnBossDefeat() - 보스 전투 실패, 해당 블록의 처음 서브스테이지로 리셋합니다.");
        int blockIndex = (currentSubStage - 1) / 10;
        currentSubStage = blockIndex * 10 + 1;
        killCount = 0;
        UpdateCurrentBlock();
        UpdateStageInfo();

        if (bossButton != null) bossButton.SetActive(true);
        GoIdleMode();
    }

    public void OnGeneralDefeat()
    {
        Debug.Log("[StageManager] OnGeneralDefeat() - 일반 스테이지에서 플레이어 사망, 서브스테이지 재시작");
        killCount = 0;
        if (mobSpawner != null)
        {
            mobSpawner.SetActive(true);
            EnemySpawner es = mobSpawner.GetComponent<EnemySpawner>();
            if (es != null)
                es.ClearEnemies();
        }
        UpdateStageInfo();
        GoIdleMode();
    }

    public void OnPlayerDefeat()
    {
        if (IsBossStage(currentSubStage)) OnBossDefeat();
        else OnGeneralDefeat();
    }

    private void GoNextChapter()
    {
        currentChapterIndex++;
        if (currentChapterIndex >= stageDatas.Length)
        {
            Debug.Log("[StageManager] 모든 챕터 클리어! 다시 0번으로 순환");
            currentChapterIndex = 0;
        }
        LoadChapter(currentChapterIndex);
    }

    private void GoIdleMode()
    {
        Debug.Log("[StageManager] GoIdleMode()");
        if (idleModePanel) idleModePanel.SetActive(true);
        if (bossModePanel) bossModePanel.SetActive(false);

        if (mobSpawner) mobSpawner.SetActive(true);
        if (bossSpawner)
        {
            bossSpawner.SetActive(false);
            BossSpawner bs = bossSpawner.GetComponent<BossSpawner>();
            if (bs != null) bs.ClearBoss();
        }
    }

    public void GoBossMode()
    {
        Debug.Log("[StageManager] GoBossMode()");
        if (idleModePanel) idleModePanel.SetActive(false);
        if (bossModePanel) bossModePanel.SetActive(true);

        if (mobSpawner)
        {
            mobSpawner.SetActive(false);
            EnemySpawner es = mobSpawner.GetComponent<EnemySpawner>();
            if (es != null) es.ClearEnemies();
        }
        if (bossSpawner) bossSpawner.SetActive(true);

        if (bossButton != null) bossButton.SetActive(false);
    }

    public void ReEnterBossMode()
    {
        int blockIndex = (currentSubStage - 1) / 10;
        currentSubStage = blockIndex * 10 + 10;
        killCount = 0;
        UpdateCurrentBlock();
        UpdateStageInfo();
        GoBossMode();
    }

    private bool IsBossStage(int subStage)
    {
        return (subStage % 10 == 0);
    }

    private void UpdateCurrentBlock()
    {
        int blockIndex = (currentSubStage - 1) / 10;
        if (blockIndex < 0) blockIndex = 0;
        if (blockIndex >= currentStageData.subStageBlocks.Length)
            blockIndex = currentStageData.subStageBlocks.Length - 1;

        currentBlock = currentStageData.subStageBlocks[blockIndex];
    }

    private void UpdateStageInfo()
    {
        if (stageInfoText)
        {
            stageInfoText.text = $"{currentStageData.chapter}-{currentSubStage}";
        }
    }

    public GameObject GetEnemyPrefab()
    {
        if (currentBlock != null && currentBlock.enemyPrefabs != null && currentBlock.enemyPrefabs.Length > 0)
        {
            int randomIndex = Random.Range(0, currentBlock.enemyPrefabs.Length);
            return currentBlock.enemyPrefabs[randomIndex];
        }
        return null;
    }

    public GameObject GetBossPrefab()
    {
        return currentBlock != null ? currentBlock.bossPrefab : null;
    }

    public StageData GetCurrentStageData()
    {
        return currentStageData;
    }
}
