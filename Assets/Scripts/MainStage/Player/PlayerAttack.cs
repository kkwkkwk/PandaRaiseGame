using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [Tooltip("플레이어의 공격력")]
    public int damage = 10;

    [Tooltip("공격 사거리 (거리가 이 값 이하일 때 공격)")]
    public float attackRange = 2f;

    [Tooltip("얼마나 자주 공격을 시도할지 (초 간격)")]
    public float attackInterval = 1f;

    // "이번 공격 시도에서 실제로 적에게 데미지를 줬는지" 여부
    [HideInInspector] public bool isAttacking = false;

    private Coroutine attackLoop;

    void Start()
    {
        attackLoop = StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        while (true)
        {
            // 매 공격 사이클마다 일단 false로 초기화
            isAttacking = false;

            // (1) 가장 가까운 Enemy 찾기
            Transform targetEnemy = FindClosestEnemy();
            if (targetEnemy != null)
            {
                float distance = Vector2.Distance(transform.position, targetEnemy.position);
                // (2) 범위 이내면 데미지
                if (distance <= attackRange)
                {
                    // 먼저 EnemyStats 시도
                    EnemyStats enemyStats = targetEnemy.GetComponent<EnemyStats>();
                    if (enemyStats != null)
                    {
                        enemyStats.TakeDamage(damage);
                        isAttacking = true;
                        Debug.Log($"[PlayerAttack] 플레이어가 Enemy에게 {damage} 데미지! (EnemyStats)");
                    }
                    else
                    {
                        // EnemyStats가 없다면 BossStats 시도
                        BossStats bossStats = targetEnemy.GetComponent<BossStats>();
                        if (bossStats != null)
                        {
                            bossStats.TakeDamage(damage);
                            isAttacking = true;
                            Debug.Log($"[PlayerAttack] 플레이어가 Enemy(보스)에게 {damage} 데미지! (BossStats)");
                        }
                    }
                }
            }

            // 다음 공격까지 대기
            yield return new WaitForSeconds(attackInterval);
        }
    }

    private Transform FindClosestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float closestDist = Mathf.Infinity;
        Transform closest = null;

        foreach (GameObject e in enemies)
        {
            float dist = Vector2.Distance(transform.position, e.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = e.transform;
            }
        }
        return closest;
    }

    private void OnDisable()
    {
        if (attackLoop != null)
            StopCoroutine(attackLoop);
    }
}
