using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GachaResultItemController : MonoBehaviour
{
    [Header("UI 연결")]
    [Tooltip("아이템의 큰 아이콘")]
    public Image itemIconImage;
    [Tooltip("아이템 등급 프레임")]
    public Image gradeFrameImage;
    [Tooltip("아이템 이름 텍스트")]
    public TextMeshProUGUI itemNameText;
    [Tooltip("갯수 텍스트 (예: X 3)")]
    public TextMeshProUGUI countText;

    /// <summary>
    /// 내부 공통 설정: 이름과 “X {count}” 표시
    /// </summary>
    private void SetupInternal(string itemName, int count)
    {
        if (itemNameText != null)
            itemNameText.text = itemName;
        if (countText != null)
            countText.text = "X " + count;
    }

    /// <summary>무기 데이터를 받아 세팅</summary>
    public void Setup(WeaponItemData data, int count = 1)
    {
        SetupInternal(data.ItemName, count);
    }

    /// <summary>방어구 데이터를 받아 세팅</summary>
    public void Setup(ArmorItemData data, int count = 1)
    {
        SetupInternal(data.ItemName, count);
    }

    /// <summary>스킬 데이터를 받아 세팅</summary>
    public void Setup(SkillItemData data, int count = 1)
    {
        SetupInternal(data.ItemName, count);
    }
}
