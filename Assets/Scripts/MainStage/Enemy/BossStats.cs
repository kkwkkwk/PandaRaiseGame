using UnityEngine;
using System;

public class BossStats : MonoBehaviour
{
    [Tooltip("보스 최대 체력")]
    public int maxHealth = 500;
    [Tooltip("보스 현재 체력")]
    public int currentHealth;

    // 보스 사망 이벤트 (필요하다면 다른 스크립트가 OnBossDeath += handler 로 구독할 수 있음)
    public event Action OnBossDeath;

    private void Start()
    {
        currentHealth = maxHealth; // 초기 체력 설정
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"보스가 피해를 입음! 현재 체력: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("보스 사망!");

        // (1) StageManager에 "보스 승리" 알림 → 10스테이지에서 11로 넘어가는 등 진행
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnBossVictory();
        }

        // (2) 보스 사망 이벤트 (필요한 다른 로직이 있다면 구독해서 처리 가능)
        OnBossDeath?.Invoke();

        // (3) 보스 오브젝트 제거
        Destroy(gameObject);
    }
}
