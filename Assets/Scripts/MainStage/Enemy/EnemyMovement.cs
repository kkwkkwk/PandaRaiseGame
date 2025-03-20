using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Tooltip("이동 속도 (초당 거리)")]
    public float moveSpeed = 2f;

    private Transform playerTransform;

    private void Start()
    {
        // 씬에서 태그가 "Player"인 오브젝트를 찾고 Transform을 가져옵니다.
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
        // 플레이어를 찾았다면, x좌표만 향해 이동
        if (playerTransform != null)
        {
            // 현재 위치
            Vector3 currentPos = transform.position;
            // 목표 x좌표(플레이어의 x)
            float targetX = playerTransform.position.x;

            // moveSpeed * Time.deltaTime 만큼 부드럽게 이동
            float step = moveSpeed * Time.deltaTime;

            // x좌표만 플레이어 쪽으로 이동, y/z는 기존값 유지
            float newX = Mathf.MoveTowards(currentPos.x, targetX, step);
            transform.position = new Vector3(newX, currentPos.y, currentPos.z);
        }
    }
}
