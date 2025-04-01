using UnityEngine;

[CreateAssetMenu(fileName = "NewStageData", menuName = "Stage/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("Chapter Info")]
    [Tooltip("챕터 번호 (예: 1이면 1-1~1-100, 2이면 2-1~2-100)")]
    public int chapter;

    [Header("Total SubStages")]
    [Tooltip("이 챕터 안에서 진행할 서브스테이지 수 (기본 100)")]
    public int totalSubStages = 100;

    [Header("Player Settings")]
    [Tooltip("이 챕터에서 사용할 플레이어 프리팹")]
    public GameObject playerPrefab;

    [Header("Player Spawn Positions")]
    [Tooltip("이 챕터에서 사용할 플레이어 스폰 위치들 (월드 좌표)")]
    public Vector3[] playerSpawnPositions;

    [Header("Enemy Spawn Positions")]
    [Tooltip("이 챕터에서 사용할 에너미 스폰 위치들 (월드 좌표)")]
    public Vector3[] enemySpawnPositions;

    [Header("Boss Spawn Positions")]
    [Tooltip("이 챕터에서 사용할 보스 스폰 위치들 (월드 좌표)")]
    public Vector3[] bossSpawnPositions;

    [Header("10단위 블록 설정 (배열 길이=10)")]
    [Tooltip("각 블록이 10스테이지를 담당. 예: 0번=1~10, 1번=11~20, ... 9번=91~100")]
    public SubStageBlock[] subStageBlocks = new SubStageBlock[10];
}
