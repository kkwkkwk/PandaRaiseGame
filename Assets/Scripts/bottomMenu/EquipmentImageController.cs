using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentImageController : MonoBehaviour
{
    public Image icon;

    [Header("Sprites")]
    public Sprite testWeaponSprite; // test 용 무기 이미지
    public Sprite testArmorSprite;
    public Sprite testSkillSprite;
    public Button imageButton; // 이미지 버튼

    // 단일 이미지 Prefab(EquipmentImagePrefab)에 있는 텍스트 3개
    public TextMeshProUGUI enchantText;  // 강화 수치
    public TextMeshProUGUI levelText;    // 레벨
    public TextMeshProUGUI classText;    // 등급

    private WeaponData currentWeapon;
    private ArmorData currentArmor;
    private SkillData currentSkill;

    /// <summary>
    /// WeaponData의 강화, 레벨, 등급을 받아서 3개 Text에 표시
    /// </summary>
    public void WeaponSetData(WeaponData weaponData)
    {
        currentWeapon = weaponData;
        if (enchantText != null)
            enchantText.text = $"+{weaponData.enhancement}";  // ex) +5

        if (levelText != null)
            levelText.text = $"Lv. {weaponData.level}";       // ex) Lv. 10

        if (classText != null)
            classText.text = weaponData.rank;                 // ex) Epic, Rare, etc.

        if (icon != null)
            icon.sprite = testWeaponSprite;

        // 클릭 리스너 등록
        if (imageButton != null)
        {
            imageButton.onClick.RemoveAllListeners();
            imageButton.onClick.AddListener(() => OnClickImage(0));
        }
        Debug.Log($"[EquipmentImageController] 무기데이터 적용됨: 강화(+{weaponData.enhancement}), 레벨({weaponData.level}), 등급({weaponData.rank})");
    }
    /// <summary>
    /// ArmorData의 강화, 레벨, 등급을 받아서 3개 Text에 표시
    /// </summary>
    public void ArmorSetData(ArmorData armorData)
    {
        currentArmor = armorData;
        if (enchantText != null)
            enchantText.text = $"+{armorData.enhancement}";  // ex) +5

        if (levelText != null)
            levelText.text = $"Lv. {armorData.level}";       // ex) Lv. 10

        if (classText != null)
            classText.text = armorData.rank;                 // ex) Epic, Rare, etc.

        if (icon != null)
            icon.sprite = testArmorSprite;

        // 클릭 리스너 등록
        if (imageButton != null)
        {
            imageButton.onClick.RemoveAllListeners();
            imageButton.onClick.AddListener(() => OnClickImage(1));
        }
        Debug.Log($"[EquipmentImageController] 방어구데이터 적용됨: 강화(+{armorData.enhancement}), 레벨({armorData.level}), 등급({armorData.rank})");
    }
    /// <summary>
    /// SkillData의 강화, 레벨, 등급을 받아서 3개 Text에 표시
    /// </summary>
    public void SkillSetData(SkillData skillData)
    {
        currentSkill = skillData;
        if (enchantText != null)
            enchantText.text = $"+{skillData.enhancement}";  // ex) +5

        if (levelText != null)
            levelText.text = $"Lv. {skillData.level}";       // ex) Lv. 10

        if (classText != null)
            classText.text = skillData.rank;                 // ex) Epic, Rare, etc.

        if (icon != null)
            icon.sprite = testSkillSprite;

        // 클릭 리스너 등록
        if (imageButton != null)
        {
            imageButton.onClick.RemoveAllListeners();
            imageButton.onClick.AddListener(() => OnClickImage(2));
        }
        Debug.Log($"[EquipmentImageController] 스킬데이터 적용됨: 강화(+{skillData.enhancement}), 레벨({skillData.level}), 등급({skillData.rank})");
    }
    private void Awake()
    {
        // 무기 아이콘
        testWeaponSprite = Resources.Load<Sprite>("Sprites/Weapon/weapon_icon");
        if (testWeaponSprite == null)
            Debug.LogError("testWeapon sprite 로드 실패");
        // 방어구 아이콘
        testArmorSprite = Resources.Load<Sprite>("Sprites/Armor/armor_icon");
        if (testArmorSprite == null)
            Debug.LogError("testArmor sprite 로드 실패");
        // 스킬 아이콘
        testSkillSprite = Resources.Load<Sprite>("Sprites/Skill/skill_icon");
        if (testSkillSprite == null)
            Debug.LogError("testSkill sprite 로드 실패");
    }
    private void OnClickImage(int num)
    {
        switch (num)
        {
            case 0:
                WeaponPopupManager.Instance.OnWeaponImageClicked(currentWeapon);
                break;
            case 1:
                ArmorPopupManager.Instance.OnArmorImageClicked(currentArmor);
                break;
            case 2:
                SkillPopupManager.Instance.OnSkillImageClicked(currentSkill);
                break; 
            default:
                break;
        }
    }
}
