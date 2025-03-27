using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    [Tooltip("보스가 소환될 위치 기준 Transform (Inspector에서 드래그)")]
    public Transform spawnPointTransform;

    [Header("Spawn Settings")]
    [Tooltip("스폰 체크 간격 (초) (예: 2초)")]
    public float spawnCheckInterval = 2f;

    // StageData에서 가져온 현재 보스 프리팹
    private GameObject currentBossPrefab;

    // 현재 씬에서 살아있는 보스 (최대 1마리)
    private GameObject currentBoss;
    private Coroutine spawnLoop;

    private void Start()
    {
        // StageManager에서 현재 스테이지의 StageData를 받아옴
        if (StageManager.Instance == null)
        {
            Debug.LogError("[BossSpawner] StageManager.Instance가 존재하지 않습니다!");
            return;
        }

        StageData data = StageManager.Instance.GetCurrentStageData();
        if (data == null || data.bossPrefab == null)
        {
            Debug.LogError("[BossSpawner] 현재 스테이지의 bossPrefab이 설정되지 않았습니다!");
            return;
        }
        currentBossPrefab = data.bossPrefab;

        if (spawnPointTransform == null)
        {
            Debug.LogError("[BossSpawner] spawnPointTransform이 할당되지 않았습니다!");
            return;
        }

        spawnLoop = StartCoroutine(SpawnLoop());
        Debug.Log("[BossSpawner] Start() - 보스 스폰 코루틴 시작");
    }

    private IEnumerator SpawnLoop()
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
        if (currentBossPrefab == null || spawnPointTransform == null)
        {
            Debug.LogError("[BossSpawner] SpawnBoss() 실패: currentBossPrefab 또는 spawnPointTransform이 null입니다!");
            return;
        }

        Vector3 spawnPos = spawnPointTransform.position;
        currentBoss = Instantiate(currentBossPrefab, spawnPos, Quaternion.identity);
        Debug.Log($"[BossSpawner] 보스 소환 완료! 위치: {spawnPos}");
    }

    private void OnDisable()
    {
        if (spawnLoop != null)
        {
            StopCoroutine(spawnLoop);
            Debug.Log("[BossSpawner] OnDisable() - 보스 스폰 코루틴 중지");
        }
    }
}
