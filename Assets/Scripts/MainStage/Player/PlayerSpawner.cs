using UnityEngine;
using System.Collections;

public class PlayerSpawner : MonoBehaviour
{
    [Tooltip("플레이어 체크 간격 (초)")]
    public float checkInterval = 2f;

    private GameObject currentPlayer;
    private Coroutine spawnLoop;
    // 순서대로 사용할 플레이어 스폰 위치 인덱스
    private int currentSpawnIndex = 0;

    private void Start()
    {
        spawnLoop = StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (currentPlayer == null)
            {
                SpawnPlayer();
            }
            yield return new WaitForSeconds(checkInterval);
        }
    }

    private void SpawnPlayer()
    {
        if (StageManager.Instance == null) return;

        StageData data = StageManager.Instance.GetCurrentStageData();
        if (data == null || data.playerPrefab == null)
        {
            Debug.LogWarning("[PlayerSpawner] playerPrefab is null in current StageData!");
            return;
        }

        Vector3[] spawnPositions = data.playerSpawnPositions;
        if (spawnPositions == null || spawnPositions.Length == 0)
        {
            Debug.LogWarning("[PlayerSpawner] playerSpawnPositions 배열이 비어있습니다!");
            return;
        }

        Vector3 spawnPos = spawnPositions[currentSpawnIndex];
        currentSpawnIndex = (currentSpawnIndex + 1) % spawnPositions.Length;

        currentPlayer = Instantiate(data.playerPrefab, spawnPos, Quaternion.identity, transform);
        Debug.Log("[PlayerSpawner] Player Spawned");
    }

    private void OnDisable()
    {
        if (spawnLoop != null)
            StopCoroutine(spawnLoop);
    }
}
