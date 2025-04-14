using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GrowthLevelStatItemController : MonoBehaviour
{
    [Header("Panel1 (능력치 이름 & 스탯 수치)")]
    public TextMeshProUGUI levelstat_ability_text;
    public TextMeshProUGUI current_levelstat_text;

    [Header("Panel2 (능력치 레벨)")]
    public TextMeshProUGUI abilityLevelText;

    [Header("Panel3 (버튼)")]
    public Button increaseButton;
    public Button resetButton;

    // 서버에서 받은 이 능력치의 최신 상태
    private ServerAbilityStatData _statData;
    // 상위 컨트롤러(서버 요청 + 전체 UI 재구성)
    private LevelStatPanelListCreator _parent;

    /// <summary>
    /// 프리팹 초기화: 서버 능력치 데이터를 받아서 UI 표시, 버튼 연결
    /// </summary>
    public void Initialize(ServerAbilityStatData statData, LevelStatPanelListCreator parent)
    {
        _statData = statData;
        _parent = parent;

        // 능력치 이름
        if (levelstat_ability_text != null)
            levelstat_ability_text.text = _statData.AbilityName;

        // 버튼 클릭 이벤트
        if (increaseButton != null)
            increaseButton.onClick.AddListener(OnClickIncrease);
        if (resetButton != null)
            resetButton.onClick.AddListener(OnClickReset);

        UpdateUI();
    }

    private void OnClickIncrease()
    {
        if (_parent != null && _statData != null)
        {
            // 상위에게 "이 능력치 increase" 요청
            _parent.IncreaseStat(_statData.AbilityName);
        }
    }

    private void OnClickReset()
    {
        if (_parent != null && _statData != null)
        {
            // 상위에게 "이 능력치 reset" 요청
            _parent.ResetStat(_statData.AbilityName);
        }
    }

    /// <summary>
    /// 화면에 스탯 수치(기존 -> 업글 후), 레벨을 표시
    /// </summary>
    public void UpdateUI()
    {
        if (_statData == null) return;

        // "기존 스탯" = BaseValue%
        // "업글 후 스탯" = BaseValue + (currentLevel * incrementValue)
        int currentValue = _statData.BaseValue + (_statData.CurrentLevel * _statData.IncrementValue);
        int upgradedValue = _statData.BaseValue + (_statData.CurrentLevel + 1) * _statData.IncrementValue;
        if (current_levelstat_text != null)
        {
            current_levelstat_text.text = $"{currentValue}% -> {upgradedValue}%";
        }

        if (abilityLevelText != null)
        {
            abilityLevelText.text = $"Lv. {_statData.CurrentLevel}";
        }
    }
}
