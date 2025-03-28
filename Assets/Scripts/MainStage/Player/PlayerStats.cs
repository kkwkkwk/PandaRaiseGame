using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Tooltip("플레이어 최대 체력")]
    public int maxHealth = 100;

    // 현재 체력
    public int currentHealth;

    private void Start()
    {
        // 게임 시작 시 체력을 최대치로 설정
        currentHealth = maxHealth;
    }

    /// <summary>
    /// 외부(적 공격 등)에서 피해를 입을 때 호출
    /// </summary>
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        Debug.Log($"[PlayerStats] 플레이어가 {damage} 피해를 입음! 남은 체력: {currentHealth}");

        // 체력이 0 이하로 떨어지면 0으로 고정 후 사망 처리
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
        Debug.Log("플레이어 사망!");

        // 여기서 게임 오버나 다음 스테이지 이동 등 원하는 로직 구현
        // 예) StageManager.Instance.OnPlayerDead(); 등을 호출할 수도 있음

        // 플레이어 오브젝트 파괴가 필요하다면 Destroy(gameObject) 호출 가능
        // Destroy(gameObject);
    }
}
