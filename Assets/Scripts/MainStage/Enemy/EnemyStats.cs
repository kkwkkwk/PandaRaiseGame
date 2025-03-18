using UnityEngine;
using System;

public class EnemyStats : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public event Action OnEnemyDeath; // 몬스터 사망 이벤트

    void Start()
    {
        currentHealth = maxHealth; // 초기 체력 설정
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"몬스터가 피해를 입음! 현재 체력: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("몬스터 사망!");

        // 몬스터 사망 이벤트 실행 → `EnemySpawner`가 감지
        OnEnemyDeath?.Invoke();

        // 즉시 오브젝트 제거 (스폰 시스템이 알아서 새로운 몬스터를 생성)
        Destroy(gameObject);
    }
}
