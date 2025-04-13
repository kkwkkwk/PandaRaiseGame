using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GrowthGoldItemController : MonoBehaviour
{
    [Header("Panel1 (능력치 이름 & 스탯)")]
    public TextMeshProUGUI abilityNameText;
    public TextMeshProUGUI currentStatText; // "기존 스탯 -> 업글 스탯"

    [Header("Panel2 (레벨 표시)")]
    public TextMeshProUGUI abilityLevelText; // "Lv. n"

    [Header("Panel3 (필요 골드 & 버튼)")]
    public TextMeshProUGUI costGoldText;
    public Button enhanceButton; // 골드 강화 버튼

    private ServerGoldGrowthData _data;
    private GoldGrowthPanelListCreator _parentController;

    public void Initialize(ServerGoldGrowthData data, GoldGrowthPanelListCreator parent)
    {
        _data = data;
        _parentController = parent;

        // 버튼 클릭 시
        if (enhanceButton != null)
            enhanceButton.onClick.AddListener(OnClickEnhance);

        UpdateUI();
    }

    /// <summary>
    /// UI를 _data 기반으로 갱신
    /// </summary>
    private void UpdateUI()
    {
        if (_data == null) return;

        // 1) 능력치 이름
        if (abilityNameText != null)
            abilityNameText.text = _data.AbilityName;

        // 2) 스탯 계산: baseValue + (currentLevel * incrementValue)
        int currentValue = _data.BaseValue + _data.CurrentLevel * _data.IncrementValue;
        int upgradedValue = _data.BaseValue + (_data.CurrentLevel + 1) * _data.IncrementValue;
        if (currentStatText != null)
            currentStatText.text = $"{currentValue}% -> {upgradedValue}%";

        // 3) 레벨 표시
        if (abilityLevelText != null)
            abilityLevelText.text = $"Lv. {_data.CurrentLevel}";

        // 4) 필요 골드 표시 (축약)
        if (costGoldText != null)
        {
            string formattedCost = NumberFormatter.FormatBigNumber(_data.CostGold);
            costGoldText.text = $"{formattedCost} Gold";
        }
    }

    private void OnClickEnhance()
    {
        if (_parentController != null && _data != null)
        {
            // 골드 강화 요청
            _parentController.RequestGoldEnhance(_data.AbilityName);
        }
    }
}
