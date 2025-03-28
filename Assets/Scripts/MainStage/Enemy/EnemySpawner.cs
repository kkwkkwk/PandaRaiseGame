using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Tooltip("소환 위치 기준 Transform (씬의 빈 오브젝트를 드래그)")]
    public Transform spawnPointTransform;

    [Header("Spawn Settings")]
    public int maxEnemyCount = 3;
    public float spawnCheckInterval = 2f;
    public float xOffsetRange = 20f;

    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private Coroutine spawnLoop;

    // 여러 몹 프리팹(배열) 중에서 골라 소환
    private GameObject[] currentEnemyPrefabs;

    private void Start()
    {
        // StageManager에서 현재 스테이지 가져옴
        if (StageManager.Instance == null)
        {
            Debug.LogError("[EnemySpawner] StageManager.Instance가 존재하지 않습니다!");
            return;
        }

        // StageData에서 enemyPrefabs 배열을 가져옴
        StageData currentStage = StageManager.Instance.GetCurrentStageData();
        if (currentStage == null || currentStage.enemyPrefabs == null || currentStage.enemyPrefabs.Length == 0)
        {
            Debug.LogError("[EnemySpawner] 현재 스테이지의 enemyPrefabs가 설정되지 않았거나 비어 있습니다!");
            return;
        }
        currentEnemyPrefabs = currentStage.enemyPrefabs;

        if (spawnPointTransform == null)
        {
            Debug.LogError("[EnemySpawner] spawnPointTransform이 할당되지 않았습니다!");
            return;
        }

        // 스폰 코루틴 시작
        spawnLoop = StartCoroutine(SpawnLoop());
        Debug.Log("[EnemySpawner] Start() - 스폰 코루틴 시작");
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            // 이미 파괴된(null) 오브젝트 제거
            spawnedEnemies.RemoveAll(e => e == null);

            Debug.Log($"[EnemySpawner] SpawnLoop 실행. 현재 소환된 적 수 = {spawnedEnemies.Count}");

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
        if (currentEnemyPrefabs == null || currentEnemyPrefabs.Length == 0 || spawnPointTransform == null)
        {
            Debug.LogError("[EnemySpawner] SpawnEnemy() 실패: currentEnemyPrefabs가 없거나 spawnPointTransform이 null입니다!");
            return;
        }

        // 위치 계산
        Vector3 basePos = spawnPointTransform.position;
        float randomXOffset = Random.Range(-xOffsetRange, xOffsetRange);
        Vector3 spawnPos = new Vector3(basePos.x + randomXOffset, basePos.y, basePos.z);

        // 무작위 인덱스 선택
        int randomIndex = Random.Range(0, currentEnemyPrefabs.Length);
        GameObject chosenPrefab = currentEnemyPrefabs[randomIndex];

        // 소환. 네 번째 매개변수로 this.transform 지정 → EnemySpawner 아래 자식으로 생성
        GameObject newEnemy = Instantiate(chosenPrefab, spawnPos, Quaternion.identity, this.transform);

        spawnedEnemies.Add(newEnemy);

        Debug.Log($"[EnemySpawner] '{chosenPrefab.name}' 몹 소환 완료(부모={this.name}). 위치={spawnPos}, 현재 소환된 적 수={spawnedEnemies.Count}");
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
