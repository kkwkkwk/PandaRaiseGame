using System.Collections.Generic;
using UnityEngine;

public class GachaTabController : MonoBehaviour
{
    [Header("뽑기 탭 패널들")]
    public GameObject weaponPanel;
    public GameObject armorPanel;
    public GameObject skillPanel;

    // Open…Panel 호출
    public WeaponManager weaponManager;
    public ArmorManager armorManager;
    public SkillManager skillManager;

    private void Start()
    {
        ShowWeaponPanel();
    }

    private void HideAllTabs()
    {
        weaponPanel.SetActive(false);
        armorPanel.SetActive(false);
        skillPanel.SetActive(false);
    }

    public void ShowWeaponPanel()
    {
        HideAllTabs();
        weaponPanel.SetActive(true);
        weaponManager.OpenWeaponPanel();
    }

    public void ShowArmorPanel()
    {
        HideAllTabs();
        armorPanel.SetActive(true);
        armorManager.OpenArmorPanel();
    }

    public void ShowSkillPanel()
    {
        HideAllTabs();
        skillPanel.SetActive(true);
        skillManager.OpenSkillPanel();
    }
}
