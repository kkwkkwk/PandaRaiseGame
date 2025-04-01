using UnityEngine;
using System.Collections;

public class BossSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("보스 소환 체크 주기 (초)")]
    public float spawnCheckInterval = 2f;

    private GameObject currentBoss;
    private Coroutine spawnLoop;
    // StageData의 bossSpawnPositions 배열을 순서대로 사용할 인덱스
    private int currentSpawnIndex = 0;

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
            if (currentBoss == null)
            {
                SpawnBoss();
            }
            yield return new WaitForSeconds(spawnCheckInterval);
        }
    }

    private void SpawnBoss()
    {
        if (StageManager.Instance == null)
            return;
        GameObject bossPrefab = StageManager.Instance.GetBossPrefab();
        if (bossPrefab == null)
            return;

        StageData data = StageManager.Instance.GetCurrentStageData();
        if (data == null)
            return;
        Vector3[] spawnPositions = data.bossSpawnPositions;
        if (spawnPositions == null || spawnPositions.Length == 0)
            return;

        Vector3 spawnPos = spawnPositions[currentSpawnIndex];
        currentSpawnIndex = (currentSpawnIndex + 1) % spawnPositions.Length;

        currentBoss = Instantiate(bossPrefab, spawnPos, Quaternion.identity, transform);
        Debug.Log($"[BossSpawner] Spawned Boss: {currentBoss.name}");
    }

    /// <summary>
    /// 현재 소환된 보스 오브젝트 제거
    /// </summary>
    public void ClearBoss()
    {
        if (currentBoss != null)
        {
            Destroy(currentBoss);
            currentBoss = null;
        }
    }
}
