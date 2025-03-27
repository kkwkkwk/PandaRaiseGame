using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Tooltip("소환 위치 기준 Transform (씬의 빈 오브젝트를 드래그)")]
    public Transform spawnPointTransform;

    [Header("Spawn Settings")]
    [Tooltip("동시에 유지할 적의 최대 수 (예: 3)")]
    public int maxEnemyCount = 3;
    [Tooltip("스폰 체크 간격 (초) (예: 2초)")]
    public float spawnCheckInterval = 2f;
    [Tooltip("spawnPointTransform 기준 x 좌표에 ±로 더할 범위 (예: 20)")]
    public float xOffsetRange = 20f;

    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private Coroutine spawnLoop;
    private GameObject currentEnemyPrefab;

    private void Start()
    {
        if (StageManager.Instance == null)
        {
            Debug.LogError("[EnemySpawner] StageManager.Instance가 존재하지 않습니다!");
            return;
        }

        StageData currentStage = StageManager.Instance.GetCurrentStageData();
        if (currentStage == null || currentStage.enemyPrefab == null)
        {
            Debug.LogError("[EnemySpawner] 현재 스테이지의 enemyPrefab이 설정되지 않았습니다!");
            return;
        }
        currentEnemyPrefab = currentStage.enemyPrefab;

        if (spawnPointTransform == null)
        {
            Debug.LogError("[EnemySpawner] spawnPointTransform이 할당되지 않았습니다!");
            return;
        }

        spawnLoop = StartCoroutine(SpawnLoop());
        Debug.Log("[EnemySpawner] Start() - 스폰 코루틴 시작");
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            spawnedEnemies.RemoveAll(e => e == null);
            Debug.Log($"[EnemySpawner] SpawnLoop 실행. 현재 소환된 적 수 = {spawnedEnemies.Count}");

            while (spawnedEnemies.Count < maxEnemyCount)
            {
                SpawnEnemy();
            }

            yield return new WaitForSeconds(spawnCheckInterval);
        }
    }

    private void SpawnEnemy()
    {
        if (currentEnemyPrefab == null || spawnPointTransform == null)
        {
            Debug.LogError("[EnemySpawner] SpawnEnemy() 실패: currentEnemyPrefab 또는 spawnPointTransform이 null입니다!");
            return;
        }

        Vector3 basePos = spawnPointTransform.position;
        float randomXOffset = Random.Range(-xOffsetRange, xOffsetRange);
        Vector3 spawnPos = new Vector3(basePos.x + randomXOffset, basePos.y, basePos.z);

        GameObject newEnemy = Instantiate(currentEnemyPrefab, spawnPos, Quaternion.identity);
        spawnedEnemies.Add(newEnemy);

        Debug.Log($"[EnemySpawner] 일반 몹 소환 완료. 최종 소환 위치={spawnPos}, 현재 소환된 적 수={spawnedEnemies.Count}");
    }

    private void OnDisable()
    {
        if (spawnLoop != null)
        {
            StopCoroutine(spawnLoop);
            Debug.Log("[EnemySpawner] OnDisable() - 스폰 코루틴 중지");
        }
    }
}
