using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GuildMissionItemController : MonoBehaviour
{
    public TextMeshProUGUI missionText;  // guild_mission_text
    public Button missionButton;         // guild_mission_button

    public void SetData(GuildMissionData missionData)
    {
        if (missionText != null)
        {
            // 미션 내용만 표시
            missionText.text = missionData.missionContent;
            // 미션 클리어 여부에 따라 텍스트 색상을 변경 (예: 클리어 시 회색, 미클리어 시 흰색)
            missionText.color = missionData.isCleared ? Color.gray : Color.white;
        }

        if (missionButton != null)
        {
            missionButton.onClick.RemoveAllListeners();
            if (missionData.isCleared)
            {
                // 미션이 클리어된 경우, 버튼을 비활성화하거나 다른 처리를 할 수 있음
                missionButton.interactable = false;
            }
            else
            {
                missionButton.interactable = true;
                missionButton.onClick.AddListener(() => OnClickReward(missionData));
            }
        }
    }

    private void OnClickReward(GuildMissionData missionData)
    {
        Debug.Log($"[GuildMissionItemController] 보상 받기 클릭 - 미션 내용: {missionData.missionContent}");
        // 보상 로직 처리
    }
}
