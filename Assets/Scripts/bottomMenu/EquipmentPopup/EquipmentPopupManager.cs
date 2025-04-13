using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class EquipmentPopupManager : MonoBehaviour
{
    public static EquipmentPopupManager Instance { get; private set; }

    public GameObject EquipmentPopupCanvas;            // 팝업 canvas

    [Header("Selected Equipment Prefab")]
    public GameObject equipmentImagePrefab;            // 중단부 이미지 프리팹
    public Transform middleParent;          // 중단부 부모 (Instatiate할 위치)

    [Header("Selected Equipment Effect Text")]
    public TextMeshProUGUI equipEffectText;            // 장착효과 text
    public TextMeshProUGUI passiveEffectText;          // 보유효과 text

    [Header("Material Icon UI")]
    public Sprite goldIcon;                            // 골드 아이콘
    public Sprite reinforcementStoneIcon;              // 강화석 아이콘

    [Header("Level Up UI")]
    public TextMeshProUGUI levelMaterialText;          // 레벨업에 필요한 재료(강화석) 수치 "보유 강화석/필요 강화석" 표시
    public Button levelUpBtn;                          // 레벨업 버튼

    [Header("Enchant UI")]
    public TextMeshProUGUI EnchantMaterialGoldText;    // 강화에 필요한 재료(골드) 수치 text
    public TextMeshProUGUI EnchantMaterialText;        // 강화에 필요한 재료(중복 장비 보유량) 수치 text
    public TextMeshProUGUI EnchantProbabilityText;     // 강화 성공 확률 text
    public Button enchantBtn;                          // 강화 버튼

    [Header("etc UI")]
    public Button equipBtn;                            // 장착 버튼
    public Button closeBtn;                            // 닫기 버튼

    private int MaxEquipmentLevel = 1000;              // 최대 레벨 수치
    private int MaxEnchantLevel = 10;                  // 최대 강화 수치

    // ----------------- [서버 API URL들] -----------------
    [Header("Server API URLs")]
    [Tooltip("서버에서 강화 정보(비용, 확률 등)를 조회하는 API")]
    private string getEnhancementInfoURL = "https://pandaraisegame-equipment.azurewebsites.net/api/GetEnhancementInfo?code=Gv5bmxhOajuDS1TDx2dC2diskqmdOp1xq91Hep0VzLaAAzFuHxxFaw==";
    [Tooltip("서버에서 실제 강화 처리 API")]
    private string performEnhancementURL = "https://pandaraisegame-equipment.azurewebsites.net/api/PerformEnhancement?code=3QXxQX7s8GAhX9_DOPaNUQdY5QzB7OLMvK2EXXJCX9w6AzFusIZ7vg==";
    [Tooltip("서버에서 레벨업에 필요한 강화석 수를 조회하는 API")]
    private string getLevelUpInfoURL = "https://pandaraisegame-equipment.azurewebsites.net/api/GetLevelUpInfo?code=5Y7RQsoGbuPUPfxVeLIaSBFX0OkDXcGC8Xfl61lPgwjTAzFucq4bfA==";
    [Tooltip("서버에서 실제 레벨업을 처리하는 API")]
    private string performLevelUpURL = "https://pandaraisegame-equipment.azurewebsites.net/api/PerformLevelUp?code=PCluh6Jp0U4MogG-velVQUqm1Tm2W51YeJ78SrXXXPgNAzFu0FLEfQ==";

    // 현재 아이템이 플레이어 인벤토리 상에서 어떤 InstanceId를 갖는지 (서버 통신용)
    private string currentItemInstanceId;
    // 강화 정보(서버에서 받은)
    private EnhancementInfoResult currentEnhInfo;

    private WeaponData currentWeapon;
    private ArmorData currentArmor;
    private SkillData currentSkill;



    // 중단부에 생성된 프리팹
    private GameObject currentMiddleObj;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // 버튼 연결
        if (closeBtn != null)
            closeBtn.onClick.AddListener(CloseEquipmentPopup);
        if (levelUpBtn != null)
            levelUpBtn.onClick.AddListener(OnClickLevelUp);
        if (enchantBtn != null)
            enchantBtn.onClick.AddListener(OnClickEnchant);
        if (equipBtn != null)
            equipBtn.onClick.AddListener(OnClickEquip);

        // 처음 팝업은 닫힌 상태
        if (EquipmentPopupCanvas != null)
            EquipmentPopupCanvas.SetActive(false);
    }

    public void OpenEquipmentPopup(WeaponData weapon) // 무기 프리팹 클릭시
    {   // 팝업 활성화 
        currentWeapon = weapon;
        currentArmor = null;
        currentSkill = null;
        currentItemInstanceId = weapon.itemInstanceId;

        Debug.Log($"[EquipmentPopupManager] OpenEquipmentPopup 실행");
        if (EquipmentPopupCanvas != null)
        {
            EquipmentPopupCanvas.SetActive(true);
            Debug.Log($"[EquipmentPopupManager] Canvas 활성화 실행");
        }
        // 상단 UI (착용 효과, 보유 효과)
        SetTopEffectText(weapon.equipEffectDesc, weapon.passiveEffectDesc, weapon.isOwned);
        // 중단부 프리팹 표시 (무기 정보)
        CreateMiddlePrefab_AndDisplayWeapon(weapon);
        // 강화 정보 받아오기
        StartCoroutine(FetchEnhancementInfoCoroutine(weapon.itemInstanceId));
        // 서버로부터 레벨업 정보(강화석 필요량) 받아오기
        StartCoroutine(FetchLevelUpInfoCoroutine(weapon.itemInstanceId));

    }

    public void OpenEquipmentPopup(ArmorData armor) // 방어구 프리팹 클릭시
    {   // 팝업 활성화 
        currentWeapon = null;
        currentArmor = armor;
        currentSkill = null;
        currentItemInstanceId = armor.itemInstanceId;

        Debug.Log($"[EquipmentPopupManager] OpenEquipmentPopup 실행");
        if (EquipmentPopupCanvas != null)
        {
            EquipmentPopupCanvas.SetActive(true);
            Debug.Log($"[EquipmentPopupManager] Canvas 활성화 실행");
        }
        SetTopEffectText(armor.equipEffectDesc, armor.passiveEffectDesc, armor.isOwned);
        CreateMiddlePrefab_AndDisplayArmor(armor);

        // 서버로부터 강화비용/확률/재료 필요량 등 받아오기
        StartCoroutine(FetchEnhancementInfoCoroutine(armor.itemInstanceId));
        // 서버로부터 레벨업 정보(강화석 필요량) 받아오기
        StartCoroutine(FetchLevelUpInfoCoroutine(armor.itemInstanceId));

    }

    public void OpenEquipmentPopup(SkillData skill) // 스킬 프리팹 클릭시
    {   // 팝업 활성화 
        currentWeapon = null;
        currentArmor = null;
        currentSkill = skill;
        currentItemInstanceId = skill.itemInstanceId;

        Debug.Log($"[EquipmentPopupManager] OpenEquipmentPopup 실행");
        if (EquipmentPopupCanvas != null)
        {
            EquipmentPopupCanvas.SetActive(true);
            Debug.Log($"[EquipmentPopupManager] Canvas 활성화 실행");
        }

        SetTopEffectText(skill.equipEffectDesc, skill.passiveEffectDesc, skill.isOwned);
        CreateMiddlePrefab_AndDisplaySkill(skill);

        // 서버로부터 강화비용/확률/재료 필요량 등 받아오기
        StartCoroutine(FetchEnhancementInfoCoroutine(skill.itemInstanceId));
        // 서버로부터 레벨업 정보(강화석 필요량) 받아오기
        StartCoroutine(FetchLevelUpInfoCoroutine(skill.itemInstanceId));
    }

    public void CloseEquipmentPopup()
    {  // 팝업 비활성화
        if (EquipmentPopupCanvas != null)
            EquipmentPopupCanvas.SetActive(false);

        // 중단부 프리팹 제거
        if (currentMiddleObj != null)
        {
            Destroy(currentMiddleObj);
            currentMiddleObj = null;
        }
        currentWeapon = null;
        currentArmor = null;
        currentSkill = null;
        currentEnhInfo = null;
        Debug.Log("[EquipmentPopupManager] CloseEquipmentPopup()");
    }

    //  공통 UI 세팅 (상단부 + 장착 버튼 활성화)
    private void SetTopEffectText(string equipDesc, string passiveDesc, bool isOwned)
    {
        if (equipEffectText != null) equipEffectText.text = equipDesc;
        if (passiveEffectText != null) passiveEffectText.text = passiveDesc;
        if (equipBtn != null) equipBtn.interactable = isOwned;
    }


    // 중단부에 Prefab 생성 -> 간단히 이름/레벨/강화 수치 표시

    private void CreateMiddlePrefab_AndDisplayWeapon(WeaponData wData)
    {
        if (equipmentImagePrefab == null || middleParent == null) return;

        currentMiddleObj = Instantiate(equipmentImagePrefab, middleParent);
        SetPrefabSize();
        DisableButtonsInCurrentPrefab();
        var imgController = currentMiddleObj.GetComponent<EquipmentImageController>();
        if (imgController != null)
        {
            // 여긴 WeaponData에 맞게 표시
            imgController.WeaponSetData(wData);
        }
    }
    private void CreateMiddlePrefab_AndDisplayArmor(ArmorData aData)
    {
        if (equipmentImagePrefab == null || middleParent == null) return;

        currentMiddleObj = Instantiate(equipmentImagePrefab, middleParent);
        SetPrefabSize();
        DisableButtonsInCurrentPrefab();
        var imgController = currentMiddleObj.GetComponent<EquipmentImageController>();
        if (imgController != null)
        {
            // 방어구 표시
            imgController.ArmorSetData(aData);
        }
    }
    private void CreateMiddlePrefab_AndDisplaySkill(SkillData sData)
    {
        if (equipmentImagePrefab == null || middleParent == null) return;

        currentMiddleObj = Instantiate(equipmentImagePrefab, middleParent);
        SetPrefabSize();
        DisableButtonsInCurrentPrefab();
        var imgController = currentMiddleObj.GetComponent<EquipmentImageController>();
        if (imgController != null)
        {
            // 스킬 표시
            imgController.SkillSetData(sData);
        }
    }

    private void SetPrefabSize()
    {
        RectTransform childRect = currentMiddleObj.GetComponent<RectTransform>();
        if (childRect != null)
        {
            // 2) 부모도 RectTransform이어야 함
            //    만약 parent가 일반 Transform이면, UI 레이아웃이 정상 동작하지 않습니다.
            //    (대부분은 Canvas 안의 Panel/빈오브젝트 등일 것이므로 RectTransform일 겁니다)

            // 위치/스케일 초기화
            currentMiddleObj.transform.localScale = Vector3.one;

            // Anchor를 (0,0)~(1,1)로 설정 → 부모와 똑같은 크기로 확장
            childRect.anchorMin = Vector2.zero;
            childRect.anchorMax = Vector2.one;
            childRect.offsetMin = Vector2.zero;
            childRect.offsetMax = Vector2.zero;
            childRect.anchoredPosition = Vector2.zero;
        }
    }
    private void DisableButtonsInCurrentPrefab()
    {
        if (currentMiddleObj == null) return;

        // GetComponentsInChildren<Button>(true)는 자식(비활성화 포함)까지 Button 컴포넌트를 전부 찾음
        Button[] buttons = currentMiddleObj.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            btn.interactable = false;
        }
    }
    #region 레벨업 - 서버
    /// <summary>
    /// 서버에서 레벨업 정보 조회 (GetLevelUpInfo)
    /// </summary>
    private IEnumerator FetchLevelUpInfoCoroutine(string itemInstanceId)
    {
        if (string.IsNullOrEmpty(getLevelUpInfoURL))
        {
            Debug.LogError("[EquipmentPopupManager] getLevelUpInfoURL이 설정되지 않았습니다.");
            yield break;
        }

        // POST Body
        var payload = new
        {
            playFabId = GlobalData.playFabId,
            itemInstanceId = itemInstanceId
        };
        string jsonData = JsonConvert.SerializeObject(payload);

        using (UnityWebRequest req = new UnityWebRequest(getLevelUpInfoURL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[EquipmentPopupManager] GetLevelUpInfo 실패: {req.error}");
            }
            else
            {
                string resp = req.downloadHandler.text;
                Debug.Log($"[EquipmentPopupManager] GetLevelUpInfo 응답: {resp}");

                var info = JsonConvert.DeserializeObject<LevelUpInfoResult>(resp);
                if (info != null && info.success)
                {
                    //  서버가 보정한 현재 레벨을 적용
                    //    무기, 방어구, 스킬 중 무엇이 세팅되어 있는지 확인
                    if (currentWeapon != null)
                    {
                        currentWeapon.level = info.currentLevel;
                    }
                    else if (currentArmor != null)
                    {
                        currentArmor.level = info.currentLevel;
                    }
                    else if (currentSkill != null)
                    {
                        currentSkill.level = info.currentLevel;
                    }

                    //  UI 표시: levelMaterialText = "보유강화석/필요강화석"
                    if (levelMaterialText != null)
                        levelMaterialText.text = $"{info.userStoneCount}/{info.requiredStones}";

                    if (currentMiddleObj != null)
                    {
                        var imgController = currentMiddleObj.GetComponent<EquipmentImageController>();
                        if (imgController != null)
                        {
                            if (currentWeapon != null)
                            {
                                imgController.WeaponSetData(currentWeapon);
                            }
                            else if (currentArmor != null)
                            {
                                imgController.ArmorSetData(currentArmor);
                            }
                            else if (currentSkill != null)
                            {
                                imgController.SkillSetData(currentSkill);
                            }
                        }
                    }

                    //  버튼 활성화 결정
                    bool canLevelUp = true;
                    // 예: 만렙이면 불가 (만렙 상황에 따라)
                    int currentLevel = info.currentLevel;
                    if (currentLevel >= MaxEquipmentLevel)
                        canLevelUp = false;
                    if (info.userStoneCount < info.requiredStones)
                        canLevelUp = false;

                    if (levelUpBtn != null)
                        levelUpBtn.interactable = canLevelUp;
                }
                else
                {
                    // 실패 or 이미 만렙
                    if (info != null)
                    {
                        Debug.LogWarning($"[EquipmentPopupManager] 레벨업 정보 조회 실패 or 만렙: {info.message}");
                        if (levelMaterialText != null)
                            levelMaterialText.text = info.message;
                    }
                    if (levelUpBtn != null)
                        levelUpBtn.interactable = false;
                }
            }
        }
    }
    /// <summary>
    /// 서버에 레벨업 요청 (PerformLevelUp)
    /// </summary>
    private IEnumerator PerformLevelUpCoroutine(string itemInstanceId)
    {
        if (string.IsNullOrEmpty(performLevelUpURL))
        {
            Debug.LogError("performLevelUpURL이 설정되지 않았습니다.");
            yield break;
        }

        //  JSON 바디 생성
        var payload = new
        {
            playFabId = GlobalData.playFabId,
            itemInstanceId = itemInstanceId
        };
        string jsonData = JsonConvert.SerializeObject(payload);

        //  POST 요청
        using (UnityWebRequest req = new UnityWebRequest(performLevelUpURL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            //  요청 전송
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[EquipmentPopupManager] PerformLevelUp 실패: " + req.error);
            }
            else
            {
                //  응답 파싱
                string resp = req.downloadHandler.text;
                Debug.Log("[EquipmentPopupManager] PerformLevelUp 응답: " + resp);

                var result = JsonConvert.DeserializeObject<PerformLevelUpResult>(resp);
                if (result != null)
                {
                    if (result.success)
                    {
                        // 새 레벨 반영
                        if (currentWeapon != null)
                            currentWeapon.level = result.newLevel;
                        else if (currentArmor != null)
                            currentArmor.level = result.newLevel;
                        else if (currentSkill != null)
                            currentSkill.level = result.newLevel;

                        Debug.Log($"[EquipmentPopupManager] 레벨업 성공! 새 레벨: {result.newLevel}");

                        // 레벨업 정보 다시 조회 -> UI 갱신
                        StartCoroutine(FetchLevelUpInfoCoroutine(itemInstanceId));
                    }
                    else
                    {
                        Debug.Log("[EquipmentPopupManager] 레벨업 실패: " + result.message);
                    }
                }
            }
        }
    }
    #endregion
    #region 강화 - 서버
    /// <summary>
    /// 서버에 강화 정보 조회 (GetEnhancementInfo)
    /// </summary>
    private IEnumerator FetchEnhancementInfoCoroutine(string itemInstanceId)
    {
        if (string.IsNullOrEmpty(getEnhancementInfoURL))
        {
            Debug.LogError("getEnhancementInfoURL이 설정되지 않았습니다.");
            yield break;
        }

        // POST body
        var payload = new
        {
            playFabId = GlobalData.playFabId,
            itemInstanceId = itemInstanceId
        };
        string jsonData = JsonConvert.SerializeObject(payload);

        using (UnityWebRequest req = new UnityWebRequest(getEnhancementInfoURL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[EquipmentPopupManager] GetEnhancementInfo 실패: " + req.error);
            }
            else
            {
                string resp = req.downloadHandler.text;
                Debug.Log("[EquipmentPopupManager] GetEnhancementInfo 응답: " + resp);

                var info = JsonConvert.DeserializeObject<EnhancementInfoResult>(resp);
                if (info != null && info.success)
                {
                    currentEnhInfo = info;
                    UpdateEnhancementUI(info);
                }
                else
                {
                    Debug.LogWarning("[EquipmentPopupManager] 강화 정보 조회 실패 or success = false");
                }
                if (currentMiddleObj != null)
                {
                    var imgController = currentMiddleObj.GetComponent<EquipmentImageController>();
                    if (imgController != null)
                    {
                        if (currentWeapon != null)
                        {
                            imgController.WeaponSetData(currentWeapon);
                        }
                        else if (currentArmor != null)
                        {
                            imgController.ArmorSetData(currentArmor);
                        }
                        else if (currentSkill != null)
                        {
                            imgController.SkillSetData(currentSkill);
                        }
                    }
                }
            }
        }
    }
    /// <summary>
    /// 서버에 실제 강화 요청 (PerformEnhancement)
    /// </summary>
    private IEnumerator PerformEnhancementCoroutine(string itemInstanceId)
    {
        if (string.IsNullOrEmpty(performEnhancementURL))
        {
            Debug.LogError("[EquipmentPopupManager] performEnhancementURL이 설정되지 않았습니다.");
            yield break;
        }

        // 1) JSON 바디
        var payload = new
        {
            playFabId = GlobalData.playFabId,
            itemInstanceId = itemInstanceId
        };
        string jsonData = JsonConvert.SerializeObject(payload);

        // 2) POST 요청
        using (UnityWebRequest req = new UnityWebRequest(performEnhancementURL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            // 3) 요청 전송
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[EquipmentPopupManager] PerformEnhancement 실패: " + req.error);
            }
            else
            {
                // 4) 응답 파싱
                string resp = req.downloadHandler.text;
                Debug.Log("[EquipmentPopupManager] PerformEnhancement 응답: " + resp);

                var result = JsonConvert.DeserializeObject<PerformEnhancementResult>(resp);
                if (result != null)
                {
                    Debug.Log("[EquipmentPopupManager] 강화 결과: " + result.message);

                    if (result.success)
                    {
                        // 강화 성공 -> newEnhancement 반영
                        int newEnh = result.newEnhancement;
                        if (currentWeapon != null)
                            currentWeapon.enhancement = newEnh;
                        else if (currentArmor != null)
                            currentArmor.enhancement = newEnh;
                        else if (currentSkill != null)
                            currentSkill.enhancement = newEnh;

                        Debug.Log($"[EquipmentPopupManager] 강화 성공! 새 +강: {newEnh}");
                    }
                    else
                    {
                        // 강화 실패 -> 필요한 처리
                    }

                    // 강화 후 다시 정보 조회
                    StartCoroutine(FetchEnhancementInfoCoroutine(itemInstanceId));
                }
            }
        }
    }
    /// <summary>
    /// 서버에 강화 정보 UI 반영
    /// </summary>
    private void UpdateEnhancementUI(EnhancementInfoResult info)
    {
        // UI 텍스트 설정
        if (EnchantMaterialGoldText != null)
            EnchantMaterialGoldText.text = $"{info.userGold}/{info.goldCost}";
        if (EnchantMaterialText != null)
            EnchantMaterialText.text = $"{info.sameItemCount} / {info.requiredItemCount}";
        if (EnchantProbabilityText != null)
        {
            int percent = Mathf.RoundToInt(info.successRate * 100f);
            EnchantProbabilityText.text = $"{percent}%";
        }

        // 버튼 활성화/비활성 로직
        bool canEnchant = true;

        //  현재 장비가 이미 +최대치이면 불가
        int currentEnhance = 0;
        if (currentWeapon != null) currentEnhance = currentWeapon.enhancement;
        else if (currentArmor != null) currentEnhance = currentArmor.enhancement;
        else if (currentSkill != null) currentEnhance = currentSkill.enhancement;
        if (currentEnhance >= MaxEnchantLevel)
        {
            canEnchant = false;
        }

        //  골드 부족하면 불가
        if (info.userGold < info.goldCost)
        {
            canEnchant = false;
        }

        //  중복 아이템 부족하면 불가
        if (info.sameItemCount < info.requiredItemCount)
        {
            canEnchant = false;
        }

        // 결정
        if (enchantBtn != null)
            enchantBtn.interactable = canEnchant;
    }
    #endregion

    #region 하단 버튼 클릭 이벤트
    /// <summary>
    /// 레벨업 버튼 클릭 이벤트
    /// </summary>
    private void OnClickLevelUp()
    {
        if (string.IsNullOrEmpty(performLevelUpURL))
        {
            Debug.LogError("performLevelUpURL이 설정되지 않았습니다.");
            return;
        }

        // itemInstanceId를 매개변수로 전달
        StartCoroutine(PerformLevelUpCoroutine(currentItemInstanceId));
    }

    /// <summary>
    /// 장착 버튼 클릭 이벤트
    /// </summary>
    private void OnClickEquip()
    {
        // 무기, 방어구, 스킬 중 어떤 것인지 구분
        if (currentWeapon != null)
        {
            // 무기 장착 로직
            Debug.Log($"[EquipmentPopupManager] 무기 장착: {currentWeapon.weaponName}");
        }
        else if (currentArmor != null)
        {
            // 방어구 장착 로직
            Debug.Log($"[EquipmentPopupManager] 방어구 장착: {currentArmor.armorName}");
        }
        else if (currentSkill != null)
        {
            // 스킬 장착 로직
            Debug.Log($"[EquipmentPopupManager] 스킬 장착: {currentSkill.skillName}");
        }
    }

    /// <summary>
    /// 강화 버튼 클릭 이벤트
    /// </summary>
    private void OnClickEnchant()
    {
        if (string.IsNullOrEmpty(currentItemInstanceId))
        {
            Debug.LogError("[EquipmentPopupManager] itemInstanceId가 없습니다.");
            return;
        }
        StartCoroutine(PerformEnhancementCoroutine(currentItemInstanceId));
    }
    #endregion

    #region 서버 응답 구조
    [System.Serializable]
    public class EnhancementInfoResult
    {
        public bool success;
        public string message;
        public int enhancement;
        public string rank;
        public int goldCost;
        public float successRate;
        public int requiredItemCount;
        public int userGold;
        public int sameItemCount;
    }

    [System.Serializable]
    public class PerformEnhancementResult
    {
        public bool success;
        public string message;
        public int newEnhancement;
        public int goldUsed;
        public int itemUsedCount;
    }

    [System.Serializable]
    public class LevelUpInfoResult
    {
        public bool success;
        public string message;
        public int currentLevel;
        public int requiredStones;  // 이번 레벨업에 필요한 강화석 수
        public int userStoneCount;  // 유저가 현재 가진 강화석
    }

    [System.Serializable]
    public class PerformLevelUpResult
    {
        public bool success;
        public string message;
        public int newLevel;
        public int stoneUsed;
    }
    #endregion
}
