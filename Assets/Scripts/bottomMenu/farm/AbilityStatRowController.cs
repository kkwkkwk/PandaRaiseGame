using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 하단 스탯 항목 1줄(아이템 드랍률, 골드, 경험치) Prefab 컨트롤러
/// </summary>
public class AbilityStatRowController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI statNameText;   // "아이템 드랍률 : n%" 등
    public TextMeshProUGUI costStatText;   // "필요 스탯량 : n개"
    public Button plusBtn;                 // "+" 버튼

    private AbilityStatData _data;
    private FarmPopupManager _parent;

    /// <summary>
    /// 생성 직후 초기화
    /// </summary>
    public void Initialize(AbilityStatData data, FarmPopupManager parent)
    {
        _data = data;
        _parent = parent;

        // 텍스트 세팅
        statNameText.text = $"{data.Name} : {data.CurrentValue}%";
        costStatText.text = $"필요 스탯량 : {data.CostStat}개";

        // 버튼 리스너
        plusBtn.onClick.AddListener(OnClickPlus);
    }

    private void OnClickPlus()
    {
        _parent.OnRequestStatUpgrade(_data);
    }
}
