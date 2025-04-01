using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("소환 체크 주기 (초)")]
    public float spawnInterval = 1f; // 1초 간격

    [Tooltip("소환 위치에 적용할 X 방향 랜덤 오프셋")]
    public float xOffsetRange = 2f;

    // 소환된 몹들을 추적 (필요 시 전체 제거)
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private Coroutine spawnLoop;

    // StageData의 enemySpawnPositions 배열을 순서대로 사용할 인덱스
    private int currentSpawnPositionIndex = 0;

    private void OnEnable()
    {
        spawnLoop = StartCoroutine(SpawnRoutine());
    }

    private void OnDisable()
    {
        if (spawnLoop != null)
            StopCoroutine(spawnLoop);
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            SpawnOneEnemy();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnOneEnemy()
    {
        if (StageManager.Instance == null)
            return;

        GameObject enemyPrefab = StageManager.Instance.GetEnemyPrefab();
        if (enemyPrefab == null)
            return;

        // StageData에서 지정한 에너미 스폰 위치 배열 사용
        Vector3[] spawnPositions = StageManager.Instance.GetCurrentStageData().enemySpawnPositions;
        if (spawnPositions == null || spawnPositions.Length == 0)
            return;

        // 순서대로 스폰 위치 선택
        Vector3 basePos = spawnPositions[currentSpawnPositionIndex];
        currentSpawnPositionIndex = (currentSpawnPositionIndex + 1) % spawnPositions.Length;

        // X 방향 랜덤 오프셋 적용
        float randomX = Random.Range(-xOffsetRange, xOffsetRange);
        Vector3 spawnPos = new Vector3(basePos.x + randomX, basePos.y, basePos.z);

        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity, transform);
        spawnedEnemies.Add(newEnemy);

        Debug.Log($"[EnemySpawner] Spawned Enemy: {newEnemy.name}, total spawned count = {spawnedEnemies.Count}");
    }

    /// <summary>
    /// 기존에 소환된 모든 몹 제거
    /// </summary>
    public void ClearEnemies()
    {
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }
        spawnedEnemies.Clear();
    }
}
