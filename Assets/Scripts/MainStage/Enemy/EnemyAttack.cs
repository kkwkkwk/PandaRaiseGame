using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [Tooltip("적의 공격력")]
    public int damage = 8;

    [Tooltip("공격 사거리 (거리가 이 값 이하일 때 공격)")]
    public float attackRange = 1.5f;

    [Tooltip("얼마나 자주 공격을 시도할지 (초 간격)")]
    public float attackInterval = 1f;

    private Transform playerTransform;
    private Coroutine attackLoop;

    private void Start()
    {
        // 태그 "Player"인 오브젝트 찾기
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("[EnemyAttack] 태그가 'Player'인 오브젝트를 찾지 못했습니다!");
        }

        attackLoop = StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        while (true)
        {
            if (playerTransform != null)
            {
                float distance = Vector2.Distance(transform.position, playerTransform.position);
                if (distance <= attackRange)
                {
                    // 플레이어에게 데미지를 준다
                    PlayerStats playerStats = playerTransform.GetComponent<PlayerStats>();
                    if (playerStats != null)
                    {
                        playerStats.TakeDamage(damage);
                        Debug.Log($"[EnemyAttack] Enemy가 Player에게 {damage} 데미지! 남은 체력: {playerStats.currentHealth}");
                    }
                }
            }

            yield return new WaitForSeconds(attackInterval);
        }
    }

    private void OnDisable()
    {
        if (attackLoop != null)
        {
            StopCoroutine(attackLoop);
        }
    }
}
