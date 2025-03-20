using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private Animator animator;
    private PlayerAttack playerAttack; // PlayerAttack 스크립트 참조

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            Debug.LogWarning("[PlayerAnimationController] Animator가 없습니다!");

        // 동일 오브젝트에 있는 PlayerAttack 찾기
        playerAttack = GetComponent<PlayerAttack>();
        if (playerAttack == null)
            Debug.LogWarning("[PlayerAnimationController] PlayerAttack가 없습니다!");
    }

    void Update()
    {
        // 만약 이번 공격 사이클에서 데미지를 줬다면 isAttacking=true → 공격 모션
        // 그렇지 않으면 걷기 모션
        if (playerAttack != null && playerAttack.isAttacking)
        {
            // 공격 모션
            animator.SetBool("isAttacking", true);
            animator.SetBool("isRunning", false);
        }
        else
        {
            // 데미지를 주지 않았다면 (or playerAttack가 null)
            // => 걷기(달리기) 모션
            animator.SetBool("isAttacking", false);
            animator.SetBool("isRunning", true);
        }
    }
}
