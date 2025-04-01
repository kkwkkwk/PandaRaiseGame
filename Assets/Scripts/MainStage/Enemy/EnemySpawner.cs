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

    // 소환된 몹들을 추적 (필요 시 전체 제거할 때 사용)
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private Coroutine spawnLoop;

    // 순서대로 사용할 스폰 위치의 현재 인덱스 (StageData의 enemySpawnPositions 배열 사용)
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
        {
            Debug.LogWarning("[EnemySpawner] enemyPrefab is null!");
            return;
        }

        // StageData에서 지정한 에너미 스폰 위치 배열을 사용합니다.
        Vector3[] spawnPositions = StageManager.Instance.GetCurrentStageData().enemySpawnPositions;
        if (spawnPositions == null || spawnPositions.Length == 0)
        {
            Debug.LogWarning("[EnemySpawner] enemySpawnPositions 배열이 비어있습니다!");
            return;
        }

        // 순서대로 스폰 위치를 선택합니다.
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
    /// 기존에 소환된 모든 몹을 제거합니다.
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
