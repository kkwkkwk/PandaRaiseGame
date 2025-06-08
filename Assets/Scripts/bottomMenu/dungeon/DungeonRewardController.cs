using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DungeonRewardController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("보상 아이콘을 표시할 Image 컴포넌트")]
    [SerializeField] private Image rewardIcon;
    [Tooltip("+n 텍스트를 표시할 TextMeshProUGUI 컴포넌트")]
    [SerializeField] private TextMeshProUGUI rewardAmount;

    [Header("Reward Icons")]
    [Tooltip("골드 아이콘 스프라이트")]
    [SerializeField] private Sprite goldIcon;
    [Tooltip("다이아 아이콘 스프라이트")]
    [SerializeField] private Sprite diamondIcon;
    [Tooltip("경험치 아이콘 스프라이트")]
    [SerializeField] private Sprite expIcon;
    // 필요하다면 다른 보상 아이콘도 여기에 선언

    /// <summary>
    /// 보상 타입과 획득량을 받아 UI에 설정합니다.
    /// - rewardType: "GOLD"/"GC", "DIAMOND"/"DM", "EXP" 등을 지원
    /// - amount: 획득량
    /// </summary>
    public void Setup(string rewardType, int amount)
    {
        // 1) 보상 타입 매핑 및 아이콘 설정
        string key = rewardType.ToUpper();
        switch (key)
        {
            case "GOLD":
            case "GC":
                rewardIcon.enabled = true;
                rewardIcon.sprite = goldIcon;
                break;

            case "DIAMOND":
            case "DM":
                rewardIcon.enabled = true;
                rewardIcon.sprite = diamondIcon;
                break;

            case "EXP":
                rewardIcon.enabled = true;
                rewardIcon.sprite = expIcon;
                break;

            default:
                // 알 수 없는 보상 타입은 숨기고 경고 로그 출력
                rewardIcon.enabled = false;
                Debug.LogWarning($"[DungeonRewardController] 알 수 없는 보상 타입: {rewardType}");
                break;
        }

        // 2) 획득량 텍스트 설정
        rewardAmount.text = $"+ {amount}";
    }
}
