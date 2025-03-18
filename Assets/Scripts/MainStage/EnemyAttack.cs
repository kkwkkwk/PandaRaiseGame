using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    public float attackRange = 1.5f; // 공격 범위
    public float attackDelay = 1.0f; // 공격 딜레이 (몇 초마다 공격할지)
    public int attackPower = 10;     // 몬스터 공격력
    private Transform player;        // 플레이어 위치
    private PlayerStats playerStats; // 플레이어 스탯
    private bool isAttacking = false; // 현재 공격 중인지 여부

    void Start()
    {
        // 플레이어 찾기
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerStats = playerObj.GetComponent<PlayerStats>();
        }
    }

    void Update()
    {
        if (player != null && playerStats != null)
        {
            // 몬스터와 플레이어 간 거리 계산
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            // 플레이어가 공격 범위 안에 있다면 공격 실행
            if (distanceToPlayer <= attackRange && !isAttacking)
            {
                StartCoroutine(Attack());
            }
        }
    }

    IEnumerator Attack()
    {
        isAttacking = true;
        Debug.Log($"몬스터가 플레이어를 공격! 플레이어 체력: {playerStats.currentHealth}");

        // 플레이어에게 데미지 입힘
        playerStats.TakeDamage(attackPower);

        // 공격 대기 시간 후 다시 공격 가능
        yield return new WaitForSeconds(attackDelay);
        isAttacking = false;
    }
}
