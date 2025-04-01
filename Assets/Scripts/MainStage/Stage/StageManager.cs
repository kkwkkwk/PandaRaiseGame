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

    [Header("Debug (인스펙터에서만 사용)")]
    public string debugStageString = "1,1";
    public bool debugApplyStage = false;

    [Header("Gold")]
    [Tooltip("현재 누적된 골드")]
    public int gold = 0;

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

    private void OnValidate()
    {
        if (debugApplyStage)
        {
            debugApplyStage = false;
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[StageManager] DebugSetStage는 Play Mode에서만 동작!");
                return;
            }
            if (string.IsNullOrEmpty(debugStageString))
            {
                Debug.LogWarning("[StageManager] debugStageString이 비어있음.");
                return;
            }
            string[] parts = debugStageString.Split(',');
            if (parts.Length != 2)
            {
                Debug.LogError($"[StageManager] debugStageString='{debugStageString}' 형식이 잘못됨. 예: \"1,5\"");
                return;
            }
            if (int.TryParse(parts[0], out int ch) && int.TryParse(parts[1], out int subSt))
            {
                Debug.Log($"[StageManager] OnValidate → DebugSetStage({ch}, {subSt})");
                DebugSetStage(ch, subSt);
            }
            else
            {
                Debug.LogError($"[StageManager] debugStageString='{debugStageString}' 파싱 실패");
            }
        }
    }

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
        if (IsBossStage(currentSubStage)) GoBossMode();
        else GoIdleMode();
        Debug.Log($"[StageManager] DebugSetStage({chapter},{subStage}) 완료. 현재 스테이지 = {currentStageData.chapter}-{currentSubStage}");
    }

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

        // 몹 처치 시 골드 추가 (currentBlock.enemyGoldReward)
        gold += currentBlock.enemyGoldReward;
        Debug.Log($"[StageManager] 몹 처치! +{currentBlock.enemyGoldReward} 골드 → 총 골드: {gold}");

        killCount++;
        Debug.Log($"[StageManager] OnEnemyKilled: {killCount}/{currentBlock.killGoal} (SubStage {currentSubStage}/{currentStageData.totalSubStages})");

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

        // 보스 처치 시 골드 추가 (currentBlock.bossGoldReward)
        gold += currentBlock.bossGoldReward;
        Debug.Log($"[StageManager] 보스 처치! +{currentBlock.bossGoldReward} 골드 → 총 골드: {gold}");

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
        Debug.Log("[StageManager] OnBossDefeat() - 보스 전투 실패, 해당 블록의 처음 서브스테이지로 리셋");
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
            if (es != null) es.ClearEnemies();
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
            stageInfoText.text = $"{currentStageData.chapter}-{currentSubStage}";
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
