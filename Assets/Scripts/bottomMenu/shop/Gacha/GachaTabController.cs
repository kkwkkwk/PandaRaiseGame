using UnityEngine;

public class GachaTabController : MonoBehaviour
{
    [Header("뽑기 탭 패널들")]
    public GameObject weaponPanel;  // 무기 탭 패널
    public GameObject armorPanel;   // 방어구 탭 패널
    public GameObject skillPanel;   // 스킬 탭 패널

    private void Start()
    {
        // 시작 시 기본 패널(예: 무기)을 열고 싶다면 아래 메서드 호출
        ShowWeaponPanel();
    }

    /// <summary>
    /// 모든 탭 패널을 비활성화
    /// </summary>
    private void HideAllTabs()
    {
        if (weaponPanel != null) weaponPanel.SetActive(false);
        if (armorPanel != null) armorPanel.SetActive(false);
        if (skillPanel != null) skillPanel.SetActive(false);
    }

    /// <summary>
    /// 무기 패널 열기
    /// </summary>
    public void ShowWeaponPanel()
    {
        HideAllTabs();
        if (weaponPanel != null)
        {
            weaponPanel.SetActive(true);
            Debug.Log("[GachaTabController] 무기 탭 열림");
        }
    }

    /// <summary>
    /// 방어구 패널 열기
    /// </summary>
    public void ShowArmorPanel()
    {
        HideAllTabs();
        if (armorPanel != null)
        {
            armorPanel.SetActive(true);
            Debug.Log("[GachaTabController] 방어구 탭 열림");
        }
    }

    /// <summary>
    /// 스킬 패널 열기
    /// </summary>
    public void ShowSkillPanel()
    {
        HideAllTabs();
        if (skillPanel != null)
        {
            skillPanel.SetActive(true);
            Debug.Log("[GachaTabController] 스킬 탭 열림");
        }
    }
}
