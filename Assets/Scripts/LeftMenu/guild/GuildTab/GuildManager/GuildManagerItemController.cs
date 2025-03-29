using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuildManagerItemController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI proposerNameText;   // 가입 신청자 닉네임
    public TextMeshProUGUI proposerPowerText;  // 가입 신청자 전투력
    public Button agreeButton;                 // 가입 신청 허가 버튼
    public Button disagreeButton;              // 가입 신청 거절 버튼

    private GuildManagerData requestData; // 현재 아이템이 표시하는 가입 신청 정보

    /// <summary>
    /// 외부에서 가입 신청 데이터를 세팅하고, 버튼 이벤트도 연결
    /// </summary>
    public void Setup(GuildManagerData data)
    {
        requestData = data;

        // UI 표시
        if (proposerNameText != null)
            proposerNameText.text = requestData.applicantName;
        if (proposerPowerText != null)
            proposerPowerText.text = $"{requestData.applicantPower}";

        // 버튼 이벤트 해제 후 등록
        if (agreeButton != null)
        {
            agreeButton.onClick.RemoveAllListeners();
            agreeButton.onClick.AddListener(() =>
            {
                GuildManagerPopupManager.Instance.ApproveJoinRequest(requestData);
            });
        }

        if (disagreeButton != null)
        {
            disagreeButton.onClick.RemoveAllListeners();
            disagreeButton.onClick.AddListener(() =>
            {
                GuildManagerPopupManager.Instance.DeclineJoinRequest(requestData);
            });
        }
    }
}
