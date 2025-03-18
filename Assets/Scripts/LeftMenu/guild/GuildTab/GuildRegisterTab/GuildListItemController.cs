using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GuildListItemController : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI guildNameText;
    public TextMeshProUGUI guildLevelText;
    public TextMeshProUGUI guildHeadCountText;
    public Button guildRegisterButton;

    // 최대 인원 계산 규칙:
    // "기본값 30명 + (길드 레벨 - 1) * 2"
    // 필요하면 아래 두 값(기본값, 증가량)을 인스펙터에서 조절할 수도 있음
    public int baseCapacity = 30; // 레벨 1부터 30, 32, 34 ... 이렇게 늘어나게 설정한 상태
    public int incrementPerLevel = 2;

    public void SetData(GuildData guildData)
    {
        if (guildNameText != null)
            guildNameText.text = guildData.guildName;

        if (guildLevelText != null)
            guildLevelText.text = "Lv. " + guildData.guildLevel.ToString();

        if (guildHeadCountText != null)
        {
            // 1) 길드 레벨에 따른 최대 인원 수 계산
            int maxCapacity = baseCapacity + incrementPerLevel * (guildData.guildLevel - 1);

            // 2) "현재인원/최대인원" 형식으로 표시
            guildHeadCountText.text = $"{guildData.guildHeadCount} / {maxCapacity}";
        }

        if (guildRegisterButton != null)
        {
            guildRegisterButton.onClick.RemoveAllListeners();
            guildRegisterButton.onClick.AddListener(() => OnJoinGuild(guildData));
        }
    }

    private void OnJoinGuild(GuildData guildData)
    {
        // UI 계산으로는 자리가 있다 해도, 실제론 서버에서 이미 꽉 찼을 수 있으므로
        // 다시 확인 (클라이언트 단 체크)
        int maxCapacity = baseCapacity + incrementPerLevel * (guildData.guildLevel - 1);
        if (guildData.guildHeadCount >= maxCapacity)
        {
            Debug.LogWarning($"[OnJoinGuild] {guildData.guildName} 길드는 이미 인원 가득. 가입 불가.");
            // TODO: 팝업 메시지 표시 등
            return;
        }

        // 여기서 서버 가입 요청
        // 서버 측에서도 최종적으로 인원 수를 확인해주어야 확실히 방지 가능
        Debug.Log($"[OnJoinGuild] {guildData.guildName} 길드 가입 로직 진행...");
    }
}
