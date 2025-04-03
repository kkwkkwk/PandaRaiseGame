using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentImageController : MonoBehaviour
{
    public Image icon;

    [Header("Weapon Sprites")]
    public Sprite testWeaponSprite; // test 용 무기 이미지

    // 단일 이미지 Prefab(EquipmentImagePrefab)에 있는 텍스트 3개
    public TextMeshProUGUI enchantText;  // 강화 수치
    public TextMeshProUGUI levelText;    // 레벨
    public TextMeshProUGUI classText;    // 등급

    /// <summary>
    /// WeaponData의 강화, 레벨, 등급을 받아서 3개 Text에 표시
    /// </summary>
    public void SetData(WeaponData weaponData)
    {
        if (enchantText != null)
            enchantText.text = $"+{weaponData.enhancement}";  // ex) +5

        if (levelText != null)
            levelText.text = $"Lv. {weaponData.level}";       // ex) Lv. 10

        if (classText != null)
            classText.text = weaponData.rank;                 // ex) Epic, Rare, etc.

        if (icon != null)
            icon.sprite = testWeaponSprite;

        Debug.Log($"[EquipmentImageController] 무기데이터 적용됨: 강화(+{weaponData.enhancement}), 레벨({weaponData.level}), 등급({weaponData.rank})");
    }
    private void Awake()
    {
        testWeaponSprite = Resources.Load<Sprite>("Sprites/Weapon/weapon_icon");

        if (testWeaponSprite == null)
            Debug.LogError("testWeapon sprite 로드 실패");
    }
}
