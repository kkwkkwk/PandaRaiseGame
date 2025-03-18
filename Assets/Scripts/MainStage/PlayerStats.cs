using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public int maxHealth = 100;  // 플레이어 최대 체력
    public int currentHealth;    // 현재 체력
    public int attackPower = 15; // 공격력

    void Start()
    {
        currentHealth = maxHealth; // 게임 시작 시 최대 체력으로 설정
    }

    /// <summary>
    /// 피해를 입었을 때 체력을 감소시키는 함수
    /// </summary>
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"플레이어가 {damage} 피해를 입음! 현재 체력: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 플레이어가 죽었을 때 실행되는 함수
    /// </summary>
    private void Die()
    {
        Debug.Log("플레이어 사망!");
        // 여기서 게임 오버 처리 로직 추가 가능
    }
}
