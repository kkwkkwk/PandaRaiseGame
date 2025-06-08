using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Tooltip("플레이어 최대 체력")]
    public int maxHealth = 100;

    [Tooltip("현재 체력")]
    public int currentHealth;

    /// <summary>
    /// 플레이어 사망 시 발생하는 이벤트
    /// </summary>
    public event Action OnDeath;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// 외부에서 피해를 입을 때 호출
    /// </summary>
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"[PlayerStats] 플레이어가 {damage} 피해! 남은 체력: {currentHealth}");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    /// <summary>
    /// 플레이어 사망 처리
    /// </summary>
    private void Die()
    {
        Debug.Log("[PlayerStats] 플레이어 사망!");

        // OnDeath 이벤트 발행
        OnDeath?.Invoke();

        // 기존 StageManager 알림 (필요시 유지)
        if (StageManager.Instance != null)
            StageManager.Instance.OnPlayerDefeat();
    }
}
