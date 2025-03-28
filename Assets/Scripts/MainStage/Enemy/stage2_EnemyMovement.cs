using UnityEngine;

public class stage2_EnemyMovement : MonoBehaviour
{
    [Tooltip("이동 속도 (초당 거리)")]
    public float moveSpeed = 2f;

    private Transform playerTransform;

    private void Start()
    {
        // 태그가 'Player'인 오브젝트의 Transform을 찾기
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("[EnemyMovement] 태그가 'Player'인 오브젝트를 찾을 수 없습니다!");
        }
    }

    private void Update()
    {
        if (playerTransform != null)
        {
            // 현재 위치
            Vector3 currentPos = transform.position;
            // 이동 거리
            float step = moveSpeed * Time.deltaTime;

            // 플레이어와의 x축 차이
            float dx = currentPos.x - playerTransform.position.x;
            // dx > 0이면 적이 플레이어보다 오른쪽에 있음 → 더 오른쪽으로 이동
            // dx < 0이면 적이 플레이어보다 왼쪽에 있음 → 더 왼쪽으로 이동
            float moveDir = (dx > 0) ? 1f : -1f;

            // 새 x좌표 = 현재 x + (moveDir * step)
            float newX = currentPos.x + (moveDir * step);
            transform.position = new Vector3(newX, currentPos.y, currentPos.z);
        }
    }
}
