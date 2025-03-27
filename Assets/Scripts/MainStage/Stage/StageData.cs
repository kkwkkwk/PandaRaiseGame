using UnityEngine;

[CreateAssetMenu(fileName = "NewStageData", menuName = "Stage/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("Stage Info")]
    public int chapter;       // 큰 스테이지 번호 (예: 1 ~ 10)
    public int subStageCount; // 서브 스테이지 수 (보통 10; 1~9: 일반, 10: 보스)

    [Header("Player Settings")]
    [Tooltip("이 스테이지에서 사용할 플레이어 프리팹")]
    public GameObject playerPrefab;

    [Header("Enemy Settings")]
    [Tooltip("이 스테이지에서 사용할 일반 몹 프리팹 (1~9 스테이지)")]
    public GameObject enemyPrefab;
    [Tooltip("일반 몹 처치 목표 (예: 9마리)")]
    public int killGoal;

    [Header("Boss Settings")]
    [Tooltip("이 스테이지에서 사용할 보스 프리팹 (10 스테이지)")]
    public GameObject bossPrefab;
}
