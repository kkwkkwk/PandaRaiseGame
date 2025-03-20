using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Required Prefabs/Transforms")]
    [Tooltip("소환할 플레이어(캐릭터) 프리팹 (Inspector에서 드래그)")]
    public GameObject playerPrefab;

    [Tooltip("플레이어가 스폰될 위치(씬에서 빈 오브젝트를 만들어 드래그)")]
    public Transform spawnPointTransform;

    [Header("Spawn Settings")]
    [Tooltip("플레이어 생존 여부를 몇 초 간격으로 체크할지 (예: 2초)")]
    public float spawnCheckInterval = 2f;

    // 현재 씬에서 살아있는 플레이어를 추적
    private GameObject currentPlayer;

    // 자동으로 주기 체크할 코루틴
    private Coroutine spawnLoop;

    private void Start()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] playerPrefab이 할당되지 않았습니다!");
            return;
        }
        if (spawnPointTransform == null)
        {
            Debug.LogError("[PlayerSpawner] spawnPointTransform이 할당되지 않았습니다!");
            return;
        }

        // 플레이하면 주기적으로 플레이어가 존재하는지 검사하고, 없으면 소환
        spawnLoop = StartCoroutine(SpawnLoop());
        Debug.Log("[PlayerSpawner] Start() - 플레이어 스폰 검사 코루틴 시작");
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            // 만약 currentPlayer가 씬에서 존재하지 않거나 Destroy되었다면 (null)
            if (currentPlayer == null)
            {
                SpawnPlayer();
            }

            yield return new WaitForSeconds(spawnCheckInterval);
        }
    }

    private void SpawnPlayer()
    {
        // playerPrefab을 spawnPointTransform 위치에 생성
        currentPlayer = Instantiate(playerPrefab, spawnPointTransform.position, Quaternion.identity);

        Debug.Log($"[PlayerSpawner] 플레이어 소환 완료! 위치: {spawnPointTransform.position}");
    }

    private void OnDisable()
    {
        if (spawnLoop != null)
        {
            StopCoroutine(spawnLoop);
            Debug.Log("[PlayerSpawner] OnDisable() - 플레이어 스폰 검사 중지");
        }
    }
}
