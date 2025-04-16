using UnityEngine;

/// <summary>
/// 뽑기(Gacha) 탭 전체를 총괄하는 매니저.
/// - gachaPopupPanel: 뽑기 전체 패널 (캔버스)
/// - gachaTabController: 무기/방어구/스킬 UI 탭 전환 담당
/// - weaponManager / armorManager / skillManager: 각각 데이터 로딩 및 내부 로직 담당
/// 
/// 기본 탭: 무기. 열 때 weaponManager.OpenWeaponPanel()도 함께 호출.
/// </summary>
public class GachaManager : MonoBehaviour
{
    public static GachaManager Instance { get; private set; }

    [Header("뽑기(Gacha) 탭 전체 패널")]
    public GameObject gachaPopupPanel;   // 뽑기 탭 전체 패널/Canvas

    [Header("뽑기 탭 전환 스크립트 (UI)")]
    public GachaTabController gachaTabController;

    [Header("하위 메뉴 매니저 (데이터 로딩/로직 담당)")]
    public WeaponManager weaponManager;
    public ArmorManager armorManager;
    public SkillManager skillManager;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// 뽑기(Gacha) 탭 전체를 열고, 기본 탭(무기)을 UI로 활성화한 뒤
    /// 무기 매니저(WeaponManager)에게도 OpenWeaponPanel() 지시(서버 통신 / 데이터 로딩 등).
    /// </summary>
    public void OpenGachaPanel()
    {
        // 1) 뽑기 탭 패널 열기
        if (gachaPopupPanel != null)
            gachaPopupPanel.SetActive(true);

        // 2) GachaTabController로 기본 탭(무기) 활성화
        if (gachaTabController != null)
            gachaTabController.ShowWeaponPanel();

        // 3) 무기 매니저도 호출 (데이터 로딩, UI 세팅 등)
        if (weaponManager != null)
            weaponManager.OpenWeaponPanel();

        Debug.Log("[GachaManager] OpenGachaPanel - 기본 무기 메뉴 열림(탭 + 매니저)");
    }

    /// <summary>
    /// 뽑기(Gacha) 탭을 닫는다. 
    /// 필요하면 하위 메뉴(무기, 방어구, 스킬)도 닫기 지시.
    /// </summary>
    public void CloseGachaPanel()
    {
        // 팝업 패널 비활성화
        if (gachaPopupPanel != null)
            gachaPopupPanel.SetActive(false);

        // 무기/방어구/스킬 매니저도 닫기
        if (weaponManager != null)
            weaponManager.CloseWeaponPanel();
        if (armorManager != null)
            armorManager.CloseArmorPanel();
        if (skillManager != null)
            skillManager.CloseSkillPanel();

        Debug.Log("[GachaManager] CloseGachaPanel 호출 - 하위 메뉴도 닫음");
    }

    /// <summary>
    /// 무기 탭을 직접 선택(버튼)했을 때 호출할 수 있는 메서드:
    /// GachaTabController.ShowWeaponPanel() + WeaponManager.OpenWeaponPanel()
    /// </summary>
    public void ShowWeaponMenu()
    {
        if (gachaTabController != null)
            gachaTabController.ShowWeaponPanel();
        if (weaponManager != null)
            weaponManager.OpenWeaponPanel();

        Debug.Log("[GachaManager] ShowWeaponMenu - 무기 UI + 매니저");
    }

    /// <summary>
    /// 방어구 탭 선택 시
    /// </summary>
    public void ShowArmorMenu()
    {
        if (gachaTabController != null)
            gachaTabController.ShowArmorPanel();
        if (armorManager != null)
            armorManager.OpenArmorPanel();

        Debug.Log("[GachaManager] ShowArmorMenu - 방어구 UI + 매니저");
    }

    /// <summary>
    /// 스킬 탭 선택 시
    /// </summary>
    public void ShowSkillMenu()
    {
        if (gachaTabController != null)
            gachaTabController.ShowSkillPanel();
        if (skillManager != null)
            skillManager.OpenSkillPanel();

        Debug.Log("[GachaManager] ShowSkillMenu - 스킬 UI + 매니저");
    }
}
