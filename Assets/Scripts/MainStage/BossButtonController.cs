using UnityEngine;

public class BossButtonController : MonoBehaviour
{
    // 버튼 클릭 시 호출될 함수
    public void OnBossButtonClicked()
    {
        if (StageManager.Instance != null)
        {
            Debug.Log("[BossButtonController] 보스 버튼 클릭 - 보스 모드로 전환합니다.");
            StageManager.Instance.GoToBossMode();
        }
        else
        {
            Debug.LogWarning("[BossButtonController] StageManager 인스턴스를 찾을 수 없습니다!");
        }
    }
}
