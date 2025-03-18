using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public float moveSpeed = 2.0f; // 이동 속도 (Inspector에서 조정 가능)
    private Transform player;      // 플레이어의 위치

    void Start()
    {
        // 플레이어 찾기 (태그가 "Player"인 오브젝트)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void Update()
    {
        if (player != null)
        {
            // 현재 위치에서 플레이어 위치로 이동 (X축 이동)
            transform.position = Vector2.MoveTowards(transform.position, new Vector2(player.position.x, transform.position.y), moveSpeed * Time.deltaTime);
        }
    }
}
