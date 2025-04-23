using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SkillPopupManager : MonoBehaviour
{
    // SingleTon instance. 어디서든 SkillPopupManager.Instance로 접근 가능하게 설정(Canvas를 여러개로 나눔으로서 필요)
    // 딴 스크립트에서 SkillPopupManager.Instance.OpenSkillPanel(); 로 함수 호출 가능
    public static SkillPopupManager Instance { get; private set; }

    public GameObject SkillPopupCanvas;     // 팝업 canvas

    [Header("Skill Prefab & Content")]
    [Tooltip("스킬 정보를 보여줄 아이템 Prefab (여기 안에 5개의 이미지를 담을 자리가 있어야 함)")]
    public GameObject skillItemPrefab;      // 스킬 Prefab (예: SkillItemPrefab)
    public Transform skillContentParent;    // ScrollView의 Content
    public GameObject equipmentImagePrefab;

    // ★ (예시) 스킬 데이터 조회 API URL
    private string getSkillDataURL = "https://pandaraisegame-equipment.azurewebsites.net/api/FetchSkillData?code=IgYNcr7iN3ayfNU5QDTT5Feb5LoJOQa2WoUYtyYt5MimAzFu5wLDzA==";

    [Header("(옵션) 테스트용 무기 데이터")]
    public List<SkillData> testSkillDatas; // 임시로 몇 개 넣어둘 무기 데이터 리스트

    public void OpenSkillPanel()
    {   // 팝업 활성화 
        if (SkillPopupCanvas != null)
        {
            SkillPopupCanvas.SetActive(true);
        }
        WeaponPopupManager.Instance.CloseWeaponPanel();
        ArmorPopupManager.Instance.CloseArmorPanel();
        DungeonPopupManager.Instance.CloseDungeonPanel();

        ChatPopupManager.Instance.OpenMiddleChatPreview();  // 중앙에 채팅 미리보기 ui 생성
        ChatPopupManager.Instance.CloseBottomChatPreview(); // 하단에 채팅 미리보기 ui 제거
        ChatPopupManager.Instance.middlePreview = true;

        PopulateSkillFromGlobal();
    }

    public void CloseSkillPanel()
    {  // 팝업 비활성화
        if (SkillPopupCanvas != null)
        {
            SkillPopupCanvas.SetActive(false);
        }
        ChatPopupManager.Instance.CloseMiddleChatPreview();  // 중앙에 채팅 미리보기 ui 제거
        ChatPopupManager.Instance.OpenBottomChatPreview();   // 하단에 채팅 미리보기 ui 생성
        ChatPopupManager.Instance.middlePreview = false;
    }

    private void Awake() // Unity의 Awake() 메서드에서 싱글톤 설정
    {
        // 만약 Instance가 비어 있으면, 이 스크립트를 싱글톤 인스턴스로 설정
        if (Instance == null)
            Instance = this; // 현재 객체를 싱글톤으로 지정
        else
            // 이미 싱글톤 인스턴스가 존재하면, 중복된 객체를 제거
            Destroy(gameObject);
    }

    public IEnumerator StartSequence()
    {
        // 1) 먼저 퀘스트 조회
        yield return StartCoroutine(FetchSkillDataCoroutine());

        // 2) 코루틴 완료 후 비활성화
        SkillPopupCanvas.SetActive(false);
        Debug.Log("[LeaderboardPopupManager] StartSequence() 완료 후 Canvas 비활성화");
    }

    private IEnumerator FetchSkillDataCoroutine()
    {
        Debug.Log("[SkillPopupManager] FetchSkillDataCoroutine 시작");

        var payload = new
        {
            playFabId = GlobalData.playFabId
        };

        // JSON 직렬화
        string jsonData = JsonConvert.SerializeObject(payload);
        Debug.Log("[SkillPopupManager] 요청 JSON: " + jsonData);

        // POST 요청 생성
        UnityWebRequest request = new UnityWebRequest(getSkillDataURL, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // 요청 전송 & 응답 대기
        yield return request.SendWebRequest();

        // 응답 처리
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[SkillPopupManager] 스킬 데이터 조회 실패: " + request.error);
            ToastManager.Instance.ShowToast("스킬 데이터를 불러올 수 없습니다.");
        }
        else
        {
            string resp = request.downloadHandler.text;
            Debug.Log("[SkillPopupManager] 스킬 데이터 조회 응답: " + resp);

            List<SkillData> skillList = null;
            try
            {
                // 서버 응답(JSON) → List<SkillData> 역직렬화
                skillList = JsonConvert.DeserializeObject<List<SkillData>>(resp);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[SkillPopupManager] JSON 파싱 오류: " + ex.Message);
                ToastManager.Instance.ShowToast("스킬 데이터 파싱 실패");
                yield break;
            }

            if (skillList == null)
            {
                Debug.LogWarning("[SkillPopupManager] 서버에서 스킬 데이터가 없습니다.");
                ToastManager.Instance.ShowToast("스킬 데이터가 없습니다.");
            }
            else
            {
                GlobalData.SkillDataList = skillList;
                Debug.Log($"[SkillPopupManager] GlobalData.SkillDataList에 {skillList.Count}개 저장");
                // UI 갱신
                PopulateSkillItems(skillList);
            }
        }
    }

    private void PopulateSkillFromGlobal()
    {
        var list = GlobalData.SkillDataList;
        if (list != null && list.Count > 0)
        {
            // 기존 PopulateSkillItems(List<SkillData>) 호출
            PopulateSkillItems(list);
            Debug.Log($"[SkillPopupManager] GlobalData.SkillDataList에서 {list.Count}개 적용");
        }
        else
        {
            Debug.LogWarning("[SkillPopupManager] 전역 스킬 데이터가 없습니다.");
        }
    }

    // -----------------------[Prefab 생성 & 데이터 적용 메서드 추가]-----------------------
    /// <summary>
    /// ScrollView Content를 비우고, skillDataList에 있는 데이터만큼 스킬 아이템 Prefab을 생성해 배치합니다.
    /// </summary>
    public void PopulateSkillItems(List<SkillData> skillDataList)
    {
        Debug.Log($"[SkillPopupManager] PopulateSkillItems 호출됨, 데이터 수: {skillDataList?.Count ?? 0}");
        ClearSkillItems();

        if (skillDataList == null || skillDataList.Count == 0)
        {
            Debug.LogWarning("[SkillPopupManager] 스킬 데이터가 없습니다.");
            return;
        }

        // 한 줄(한 Row_Panel)에 몇 개씩 보여줄지
        int itemsPerRow = 5;
        int totalCount = skillDataList.Count;

        // i = 0, 5, 10, 15 ... 형태로 증가
        for (int i = 0; i < totalCount; i += itemsPerRow)
        {
            // 이번에 만들 Row_Panel에 들어갈 데이터 개수
            // (마지막에 5개 미만이 남을 수도 있으니 min 처리)
            int chunkSize = Mathf.Min(itemsPerRow, totalCount - i);

            // [i..i+chunkSize-1] 구간의 데이터만 잘라서 한 줄에 표시
            List<SkillData> rowData = skillDataList.GetRange(i, chunkSize);

            // 1) Row_Panel (SkillItemPrefab) 생성
            GameObject rowPanelObj = Instantiate(skillItemPrefab, skillContentParent);

            var rowController = rowPanelObj.GetComponent<SkillItemController>();
            if (rowController != null)
            {
                // SetData를 "List<SkillData>"로 받을 수 있게 수정
                rowController.SetData(rowData, equipmentImagePrefab);
            }
        }
    }

    /// <summary>
    /// Content 안에 들어있는 자식 객체(기존 무기 아이템 Prefab)를 모두 제거합니다.
    /// </summary>
    private void ClearSkillItems()
    {
        Debug.Log($"[SkillPopupManager] ClearSkillItems 호출됨, 자식 수: {skillContentParent.childCount}");
        for (int i = skillContentParent.childCount - 1; i >= 0; i--)
        {
            Debug.Log($"[SkillPopupManager] 제거할 무기 아이템: {skillContentParent.GetChild(i).name}");
            Destroy(skillContentParent.GetChild(i).gameObject);
        }
    }
    public void OnSkillImageClicked(SkillData clickedSkill)
    {
        Debug.Log($"[SkillPopupManager] 스킬 이미지 클릭됨 - {clickedSkill.skillName}, 레벨:{clickedSkill.level}, 등급:{clickedSkill.rank}");

        // => 장비 팝업 열기 (해당 장비 정보 전달)
        EquipmentPopupManager.Instance.OpenEquipmentPopup(clickedSkill);
    }
}
