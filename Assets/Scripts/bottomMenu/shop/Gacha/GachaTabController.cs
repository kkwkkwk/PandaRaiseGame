using UnityEngine;

public class GachaTabController : MonoBehaviour
{
    [Header("뽑기 탭 패널들")]
    public GameObject weaponPanel;
    public GameObject armorPanel;
    public GameObject skillPanel;

    [Header("퍼블릭 매니저 참조")]
    public WeaponManager weaponManager;
    public ArmorManager armorManager;
    public SkillManager skillManager;

    private void Start()
    {
        // 게임 시작 시(한 번만) 무기 패널을 기본으로 표시
        ShowWeaponPanel();
    }

    private void OnEnable()
    {
        // 뷰가 활성화될 때마다 무기 패널을 초기 서브탭으로 설정
        ShowWeaponPanel();
    }

    private void HideAllTabs()
    {
        weaponPanel.SetActive(false);
        armorPanel.SetActive(false);
        skillPanel.SetActive(false);
    }

    /// <summary>
    /// 무기 뽑기 패널 활성화 + 데이터 로드
    /// </summary>
    public void ShowWeaponPanel()
    {
        HideAllTabs();
        weaponPanel.SetActive(true);
        Debug.Log("[GachaTabController] 무기 탭 열림");
        weaponManager.OpenWeaponPanel();
    }

    /// <summary>
    /// 방어구 뽑기 패널 활성화 + 데이터 로드
    /// </summary>
    public void ShowArmorPanel()
    {
        HideAllTabs();
        armorPanel.SetActive(true);
        Debug.Log("[GachaTabController] 방어구 탭 열림");
        armorManager.OpenArmorPanel();
    }

    /// <summary>
    /// 스킬 뽑기 패널 활성화 + 데이터 로드
    /// </summary>
    public void ShowSkillPanel()
    {
        HideAllTabs();
        skillPanel.SetActive(true);
        Debug.Log("[GachaTabController] 스킬 탭 열림");
        skillManager.OpenSkillPanel();
    }
}
