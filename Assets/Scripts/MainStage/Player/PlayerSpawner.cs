using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Tooltip("플레이어가 소환될 위치 (씬의 빈 오브젝트를 드래그)")]
    public Transform spawnPointTransform;

    [Tooltip("플레이어 존재 여부를 체크할 간격 (초)")]
    public float spawnCheckInterval = 2f;

    private GameObject currentPlayer;
    private Coroutine spawnLoop;

    private void Start()
    {
        if (spawnPointTransform == null)
        {
            Debug.LogError("[PlayerSpawner] spawnPointTransform이 할당되지 않았습니다!");
            return;
        }

        spawnLoop = StartCoroutine(SpawnLoop());
        Debug.Log("[PlayerSpawner] Start() - 플레이어 스폰 검사 코루틴 시작");
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (currentPlayer == null)
            {
                SpawnPlayer();
            }
            yield return new WaitForSeconds(spawnCheckInterval);
        }
    }

    private void SpawnPlayer()
    {
        StageData currentStage = StageManager.Instance.GetCurrentStageData();
        if (currentStage == null || currentStage.playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] 현재 스테이지의 playerPrefab이 설정되지 않았습니다!");
            return;
        }

        currentPlayer = Instantiate(currentStage.playerPrefab, spawnPointTransform.position, Quaternion.identity);
        Debug.Log($"[PlayerSpawner] 플레이어 소환 완료! 위치: {spawnPointTransform.position}");
    }

    private void OnDisable()
    {
        if (spawnLoop != null)
        {
            StopCoroutine(spawnLoop);
            Debug.Log("[PlayerSpawner] OnDisable() - 플레이어 스폰 검사 코루틴 중지");
        }
    }
}
