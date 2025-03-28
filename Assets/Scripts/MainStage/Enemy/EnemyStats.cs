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

        // (1) StageManager에 몹 처치 알림
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnEnemyKilled();
        }

        // (2) 몬스터 사망 이벤트 (필요하면 유지)
        OnEnemyDeath?.Invoke();

        // (3) 오브젝트 제거
        Destroy(gameObject);
    }

}
