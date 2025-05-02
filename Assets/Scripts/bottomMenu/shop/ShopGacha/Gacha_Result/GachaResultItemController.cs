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
        // 이름
        if (itemNameText != null)
            itemNameText.text = itemName;

        // 개수
        if (countText != null)
            countText.text = $"X {count}";

        // 등급 텍스트
        if (gradeText != null)
            gradeText.text = $"{grade}등급";

        // 테두리 색 변경
        if (gradeFrameImage != null)
            gradeFrameImage.color = GetColorForGrade(grade);
    }

    /// <summary>
    /// 등급별 테두리 색 매핑
    /// </summary>
    private Color GetColorForGrade(string grade)
    {
        // 아래는 예시 "D" (또는 "Common"), "R" (Rare), "S" (Super/Legendary)
        switch (grade.ToUpper())
        {
            case "R":
            case "RARE":
                return new Color32(128, 0, 128, 255); // 보라

            case "S":
            case "SUPER":
            case "LEGENDARY":
                return Color.yellow;                 // 노랑

            default:
                return Color.gray;                   // 그 외(낮은 등급): 회색
        }
    }

    /// <summary>무기 데이터 세팅 (count, grade는 서버 응답값을 넘겨주세요)</summary>
    public void Setup(WeaponItemData data, int count = 1, string grade = "D")
    {
        // 아이콘 세팅
        if (itemIconImage != null)
            itemIconImage.sprite = /* data를 통해 가져온 Sprite */ null;

        SetupInternal(data.ItemName, count, grade);
    }

    /// <summary>방어구 데이터 세팅</summary>
    public void Setup(ArmorItemData data, int count = 1, string grade = "D")
    {
        if (itemIconImage != null)
            itemIconImage.sprite = null;

        SetupInternal(data.ItemName, count, grade);
    }

    /// <summary>스킬 데이터 세팅</summary>
    public void Setup(SkillItemData data, int count = 1, string grade = "D")
    {
        if (itemIconImage != null)
            itemIconImage.sprite = null;

        SetupInternal(data.ItemName, count, grade);
    }
}
