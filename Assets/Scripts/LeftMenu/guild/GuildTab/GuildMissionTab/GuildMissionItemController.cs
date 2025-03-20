using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GuildMissionItemController : MonoBehaviour
{
    public TextMeshProUGUI missionText;  // guild_mission_text
    public Button missionButton;         // guild_mission_button

    public void SetData(GuildMissionData missionData)
    {
        Debug.Log("[GuildMissionItemController] SetData 호출됨, 미션 ID: " + missionData.guildMissionID);

        if (missionText != null)
        {
            // 미션 내용만 표시
            missionText.text = missionData.missionContent;
            Debug.Log("[GuildMissionItemController] 미션 내용 설정: " + missionData.missionContent);
            // 미션 클리어 여부에 따라 텍스트 색상 변경
            missionText.color = missionData.isCleared ? Color.gray : Color.white;
        }
        else
        {
            Debug.LogWarning("[GuildMissionItemController] missionText가 null입니다.");
        }

        if (missionButton != null)
        {
            missionButton.onClick.RemoveAllListeners();
            if (missionData.isCleared)
            {
                missionButton.interactable = false;
                Debug.Log("[GuildMissionItemController] 미션 클리어됨, 버튼 비활성화");
            }
            else
            {
                missionButton.interactable = true;
                missionButton.onClick.AddListener(() => OnClickReward(missionData));
                Debug.Log("[GuildMissionItemController] 미션 미클리어됨, 버튼 활성화 및 이벤트 추가");
            }
        }
        else
        {
            Debug.LogWarning("[GuildMissionItemController] missionButton이 null입니다.");
        }
    }

    private void OnClickReward(GuildMissionData missionData)
    {
        Debug.Log($"[GuildMissionItemController] 보상 받기 클릭 - 미션 ID: {missionData.guildMissionID}, 내용: {missionData.missionContent}");
        // 보상 로직 처리 (추후 구현)
    }
}
