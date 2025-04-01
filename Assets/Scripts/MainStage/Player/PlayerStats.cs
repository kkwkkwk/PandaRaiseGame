using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Tooltip("플레이어 최대 체력")]
    public int maxHealth = 100;

    // 현재 체력
    public int currentHealth;

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
        Debug.Log($"[PlayerStats] 플레이어가 {damage} 피해를 입음! 남은 체력: {currentHealth}");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    /// <summary>
    /// 플레이어 사망 처리: StageManager에 알림
    /// </summary>
    private void Die()
    {
        Debug.Log("플레이어 사망!");

        // StageManager의 OnPlayerDefeat()를 호출하여,
        // 보스 스테이지에서 사망한 경우 OnBossDefeat()가 실행되도록 합니다.
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnPlayerDefeat();
        }

        // 필요하다면 플레이어 오브젝트를 제거하거나 비활성화합니다.
        // Destroy(gameObject);
    }
}
