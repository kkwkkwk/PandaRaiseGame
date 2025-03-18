using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;  // 스폰할 몬스터 프리팹
    public Transform spawnPoint;    // 몬스터가 생성될 위치
    public int maxEnemyCount = 3;   // 동시에 존재할 수 있는 최대 몬스터 수

    private List<GameObject> enemies = new List<GameObject>();

    void Start()
    {
        // 스테이지에 필요한 몬스터 개수만큼 즉시 생성
        for (int i = 0; i < maxEnemyCount; i++)
        {
            SpawnEnemy();
        }
    }

    /// <summary>
    /// 몬스터를 생성하는 함수 (즉시 실행)
    /// </summary>
    private void SpawnEnemy()
    {
        if (enemies.Count >= maxEnemyCount) return; // 현재 몬스터 개수가 최대치라면 추가 생성 X

        GameObject newEnemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
        enemies.Add(newEnemy);

        // 몬스터의 `EnemyStats` 컴포넌트에서 사망 시 알림을 받도록 설정
        EnemyStats enemyStats = newEnemy.GetComponent<EnemyStats>();
        if (enemyStats != null)
        {
            enemyStats.OnEnemyDeath += () => RemoveEnemy(newEnemy);
        }

        Debug.Log($"몬스터 생성됨! 위치: {spawnPoint.position}");
    }

    /// <summary>
    /// 몬스터 사망 시 리스트에서 제거하고 즉시 새로운 몬스터 생성
    /// </summary>
    public void RemoveEnemy(GameObject enemy)
    {
        if (enemies.Contains(enemy))
        {
            enemies.Remove(enemy);
            Destroy(enemy);
            Debug.Log("몬스터 사망 → 리스트에서 제거됨!");

            // 한 마리가 사망하면 즉시 새로운 몬스터를 스폰
            SpawnEnemy();
        }
    }
}
