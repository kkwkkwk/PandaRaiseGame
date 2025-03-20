using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Required Prefabs/Transforms")]
    [Tooltip("소환할 적(Enemy) 프리팹 (Inspector에서 드래그)")]
    public GameObject enemyPrefab;

    [Tooltip("소환 위치 기준 Transform (Inspector에서 EnemySpawnPoint를 드래그)")]
    public Transform spawnPointTransform;

    [Header("Spawn Settings")]
    [Tooltip("동시에 유지할 적의 최대 수 (예: 3)")]
    public int maxEnemyCount = 3;

    [Tooltip("스폰 체크 간격 (초) (예: 2초)")]
    public float spawnCheckInterval = 2f;

    [Header("X Offset Range")]
    [Tooltip("spawnPointTransform 기준 x 좌표에 ±로 더할 범위 (예: 20)")]
    public float xOffsetRange = 20f;

    // 현재 소환된 적을 추적
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private Coroutine spawnLoop;

    private void Start()
    {
        // 필수 필드 확인
        if (enemyPrefab == null)
        {
            Debug.LogError("[EnemySpawner] enemyPrefab이 할당되지 않았습니다!");
            return;
        }
        if (spawnPointTransform == null)
        {
            Debug.LogError("[EnemySpawner] spawnPointTransform이 할당되지 않았습니다!");
            return;
        }

        Debug.Log($"[EnemySpawner] enemyPrefab='{enemyPrefab.name}', spawnPoint='{spawnPointTransform.name}'가 정상 할당됨.");

        // 코루틴 시작
        spawnLoop = StartCoroutine(SpawnLoop());
        Debug.Log("[EnemySpawner] Start() - 스폰 코루틴 시작");
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            // 이미 파괴된(null) 오브젝트 제거
            spawnedEnemies.RemoveAll(e => e == null);

            Debug.Log($"[EnemySpawner] SpawnLoop 실행. 현재 소환된 적 수={spawnedEnemies.Count}");

            // 부족하면 새로 스폰
            while (spawnedEnemies.Count < maxEnemyCount)
            {
                SpawnEnemy();
            }

            yield return new WaitForSeconds(spawnCheckInterval);
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null || spawnPointTransform == null)
        {
            Debug.LogError("[EnemySpawner] SpawnEnemy() 실패: Prefab 또는 SpawnPoint가 null입니다!");
            return;
        }

        // spawnPointTransform 위치를 기준으로 xOffsetRange 범위 내에서만 X좌표 랜덤
        Vector3 basePos = spawnPointTransform.position;
        float randomXOffset = Random.Range(-xOffsetRange, xOffsetRange);
        Vector3 spawnPos = new Vector3(basePos.x + randomXOffset, basePos.y, basePos.z);

        // Instantiate (부모 없이) → Scene 루트(또는 EnemySpawner 오브젝트의 자식)로 놓일 수 있음
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        // 리스트 등록
        spawnedEnemies.Add(newEnemy);

        Debug.Log($"[EnemySpawner] 적 소환 완료. 최종 소환 위치={spawnPos}, 현재 소환된 적 수={spawnedEnemies.Count}");
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
