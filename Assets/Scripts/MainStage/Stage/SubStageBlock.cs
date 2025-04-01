using UnityEngine;

/// <summary>
/// "10스테이지 단위"를 한 블록으로 묶어,
/// 해당 구간(1~10, 11~20, …)에서 사용할 enemyPrefabs, bossPrefab, killGoal을 저장
/// </summary>
[System.Serializable]
public class SubStageBlock
{
    [Header("Enemy (보스 전 9스테이지)")]
    [Tooltip("해당 블록에서 사용할 일반 몹 프리팹들의 배열")]
    public GameObject[] enemyPrefabs;

    [Header("Boss (10번째 스테이지)")]
    public GameObject bossPrefab;

    [Header("Kill Goal (보스 등장 전까지)")]
    public int killGoal = 9;
}
