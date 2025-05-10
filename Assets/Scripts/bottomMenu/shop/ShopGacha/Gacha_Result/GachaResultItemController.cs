using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GachaResultItemController : MonoBehaviour
{
    [Header("UI 연결")]
    public Image itemIconImage;   // 아이템 큰 아이콘
    public Image gradeFrameImage; // 테두리 이미지 (색 변경)
    public TextMeshProUGUI gradeText;       // 등급 텍스트
    public TextMeshProUGUI itemNameText;    // 아이템 이름
    public TextMeshProUGUI countText;       // X {count}

    /// <summary>
    /// 내부 공통 설정: 이름, 개수, 등급 텍스트, 테두리 색
    /// </summary>
    private void SetupInternal(string itemName, int count, string grade)
    {
        if (itemNameText != null)
            itemNameText.text = itemName;
        if (countText != null)
            countText.text = $"X {count}";
        if (gradeText != null)
            gradeText.text = $"{grade}등급";
        if (gradeFrameImage != null)
            gradeFrameImage.color = GetColorForGrade(grade);
    }

    private Color GetColorForGrade(string grade)
    {
        switch (grade.ToUpper())
        {
            case "R": case "RARE": return new Color32(128, 0, 128, 255);
            case "S": case "SUPER": case "LEGENDARY": return Color.yellow;
            default: return Color.gray;
        }
    }

    /// <summary>
    /// 무기 데이터 세팅 (WeaponGachaItemData 사용)
    /// </summary>
    public void Setup(WeaponGachaItemData data, int count, string grade)
    {
        if (itemIconImage != null)
            itemIconImage.sprite = null;
        SetupInternal(data.ItemName, count, grade);
    }

    /// <summary>
    /// 방어구 데이터 세팅 (ArmorGachaItemData 사용)
    /// </summary>
    public void Setup(ArmorGachaItemData data, int count, string grade)
    {
        if (itemIconImage != null)
            itemIconImage.sprite = null;
        SetupInternal(data.ItemName, count, grade);
    }

    /// <summary>
    /// 스킬 데이터 세팅 (SkillGachaItemData 사용)
    /// </summary>
    public void Setup(SkillGachaItemData data, int count, string grade)
    {
        if (itemIconImage != null)
            itemIconImage.sprite = null;
        SetupInternal(data.ItemName, count, grade);
    }
}
