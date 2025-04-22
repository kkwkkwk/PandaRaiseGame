using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class WeaponPopupManager : MonoBehaviour
{
    // SingleTon instance. 어디서든 EquipmentPopupManager.Instance로 접근 가능하게 설정(Canvas를 여러개로 나눔으로서 필요)
    // 딴 스크립트에서 EquipmentPopupManager.Instance.OpenEquipmentPanel(); 로 함수 호출 가능
    public static WeaponPopupManager Instance { get; private set; }

    public GameObject WeaponPopupCanvas;     // 팝업 canvas

    [Header("Weapon Prefab & Content")]
    [Tooltip("무기 정보를 보여줄 아이템 Prefab (여기 안에 5개의 이미지를 담을 자리가 있어야 함)")]
    public GameObject weaponItemPrefab;      // 무기 아이템용 Prefab (예: WeaponItemPrefab)
    public Transform weaponContentParent;    // ScrollView의 Content
    public GameObject equipmentImagePrefab;

    // ★ (예시) 무기 데이터 조회 API URL
    private string getWeaponDataURL = "https://pandaraisegame-equipment.azurewebsites.net/api/FetchWeaponData?code=hTkMAN2m4CZFtDLT6wrt0N8R6Fd3gpW--pOM0nqsgi9KAzFu-yMktQ==";

    [Header("(옵션) 테스트용 무기 데이터")]
    public List<WeaponData> testWeaponDatas; // 임시로 몇 개 넣어둘 무기 데이터 리스트

    public void OpenWeaponPanel()
    {   // 팝업 활성화 
        Debug.Log($"[WeaponPopupManager] OpenWeaponPanel 실행");
        if (WeaponPopupCanvas != null)
        {
            WeaponPopupCanvas.SetActive(true);
            Debug.Log($"[WeaponPopupManager] Canvas 활성화 실행");
        }
        ArmorPopupManager.Instance.CloseArmorPanel();
        SkillPopupManager.Instance.CloseSkillPanel();
        DungeonPopupManager.Instance.CloseDungeonPanel();

        ChatPopupManager.Instance.OpenMiddleChatPreview();  // 중앙에 채팅 미리보기 ui 생성
        ChatPopupManager.Instance.CloseBottomChatPreview(); // 하단에 채팅 미리보기 ui 제거
        ChatPopupManager.Instance.middlePreview = true;

        // server
        StartCoroutine(FetchWeaponDataCoroutine());
    }
    public void CloseWeaponPanel()
    {  // 팝업 비활성화
        if (WeaponPopupCanvas != null)
        {
            WeaponPopupCanvas.SetActive(false);
        }

        ChatPopupManager.Instance.CloseMiddleChatPreview();  // 중앙에 채팅 미리보기 ui 제거
        ChatPopupManager.Instance.OpenBottomChatPreview();   // 하단에 채팅 미리보기 ui 생성
        ChatPopupManager.Instance.middlePreview = false;
    }

    private void Awake() // Unity의 Awake() 메서드에서 싱글톤 설정
    {
        // 만약 Instance가 비어 있으면, 이 스크립트를 싱글톤 인스턴스로 설정
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        Debug.Log($"[WeaponPopupManager] Awake on {gameObject.name}");
    }

    public IEnumerator StartSequence()
    {
        // 1) 먼저 무기 조회
        yield return StartCoroutine(FetchWeaponDataCoroutine());

        // 2) 코루틴 완료 후 비활성화
        WeaponPopupCanvas.SetActive(false);
        Debug.Log("[WeaponPopupManager] StartSequence() 완료 후 Canvas 비활성화");
    }

    /// <summary>
    /// 서버(PlayFab+AzureFunction)에서 무기 데이터 목록을 받아오는 코루틴
    /// </summary>
    private IEnumerator FetchWeaponDataCoroutine()
    {
        Debug.Log("[WeaponPopupManager] FetchWeaponDataCoroutine 시작");

        var payload = new
        {
            playFabId = GlobalData.playFabId
        };

        // JSON 직렬화
        string jsonData = JsonConvert.SerializeObject(payload);
        Debug.Log("[WeaponPopupManager] 요청 JSON: " + jsonData);

        // POST 요청 생성
        UnityWebRequest request = new UnityWebRequest(getWeaponDataURL, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // 요청 전송 & 응답 대기
        yield return request.SendWebRequest();

        // 응답 처리
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[WeaponPopupManager] 무기 데이터 조회 실패: " + request.error);
            ToastManager.Instance.ShowToast("무기 데이터를 불러올 수 없습니다.");
        }
        else
        {
            string resp = request.downloadHandler.text;
            Debug.Log("[WeaponPopupManager] 무기 데이터 조회 응답: " + resp);

            List<WeaponData> weaponList = null;
            try
            {
                // 서버 응답(JSON) → List<WeaponData> 역직렬화
                weaponList = JsonConvert.DeserializeObject<List<WeaponData>>(resp);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[WeaponPopupManager] JSON 파싱 오류: " + ex.Message);
                ToastManager.Instance.ShowToast("무기 데이터 파싱 실패");
                yield break;
            }

            if (weaponList == null)
            {
                Debug.LogWarning("[WeaponPopupManager] 서버에서 무기 데이터가 없습니다.");
                ToastManager.Instance.ShowToast("무기 데이터가 없습니다.");
            }
            else
            {
                GlobalData.WeaponDataList = weaponList;
                Debug.Log($"[WeaponPopupManager] GlobalData.WeaponDataList에 {weaponList.Count}개 저장");
                // UI 갱신
                PopulateWeaponItems(weaponList);
            }
        }
    }

    // -----------------------[Prefab 생성 & 데이터 적용 메서드 추가]-----------------------
    /// <summary>
    /// ScrollView Content를 비우고, weaponDataList에 있는 데이터만큼 무기 아이템 Prefab을 생성해 배치합니다.
    /// </summary>
    public void PopulateWeaponItems(List<WeaponData> weaponDataList)
    {
        Debug.Log($"[WeaponPopupManager] PopulateWeaponItems 호출됨, 데이터 수: {weaponDataList?.Count ?? 0}");
        ClearWeaponItems();

        if (weaponDataList == null || weaponDataList.Count == 0)
        {
            Debug.LogWarning("[WeaponPopupManager] 무기 데이터가 없습니다.");
            return;
        }

        // 한 줄(한 Row_Panel)에 몇 개씩 보여줄지
        int itemsPerRow = 5;
        int totalCount = weaponDataList.Count;

        // i = 0, 5, 10, 15 ... 형태로 증가
        for (int i = 0; i < totalCount; i += itemsPerRow)
        {
            // 이번에 만들 Row_Panel에 들어갈 데이터 개수
            // (마지막에 5개 미만이 남을 수도 있으니 min 처리)
            int chunkSize = Mathf.Min(itemsPerRow, totalCount - i);

            // [i..i+chunkSize-1] 구간의 데이터만 잘라서 한 줄에 표시
            List<WeaponData> rowData = weaponDataList.GetRange(i, chunkSize);

            // 1) Row_Panel (WeaponItemPrefab) 생성
            GameObject rowPanelObj = Instantiate(weaponItemPrefab, weaponContentParent);

            var rowController = rowPanelObj.GetComponent<WeaponItemController>();
            if (rowController != null)
            {
                // SetData를 "List<WeaponData>"로 받을 수 있게 수정
                rowController.SetData(rowData, equipmentImagePrefab);
            }
        }
    }

    /// <summary>
    /// Content 안에 들어있는 자식 객체(기존 무기 아이템 Prefab)를 모두 제거합니다.
    /// </summary>
    private void ClearWeaponItems()
    {
        Debug.Log($"[WeaponPopupManager] ClearWeaponItems 호출됨, 자식 수: {weaponContentParent.childCount}");
        for (int i = weaponContentParent.childCount - 1; i >= 0; i--)
        {
            Debug.Log($"[WeaponPopupManager] 제거할 무기 아이템: {weaponContentParent.GetChild(i).name}");
            Destroy(weaponContentParent.GetChild(i).gameObject);
        }
    }
    public void OnWeaponImageClicked(WeaponData clickedWeapon)
    {
        Debug.Log($"[WeaponPopupManager] 무기 이미지 클릭됨 - {clickedWeapon.weaponName}, 레벨:{clickedWeapon.level}, 등급:{clickedWeapon.rank}");

        // => 장비 팝업 열기 (해당 장비 정보 전달)
        EquipmentPopupManager.Instance.OpenEquipmentPopup(clickedWeapon);
    }
}