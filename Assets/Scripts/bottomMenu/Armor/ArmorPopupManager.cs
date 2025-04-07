using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ArmorPopupManager : MonoBehaviour
{
    // SingleTon instance. 어디서든 ArmorPopupManager.Instance로 접근 가능하게 설정(Canvas를 여러개로 나눔으로서 필요)
    // 딴 스크립트에서 ArmorPopupManager.Instance.OpenArmorPanel(); 로 함수 호출 가능
    public static ArmorPopupManager Instance { get; private set; }

    public GameObject ArmorPopupCanvas;     // 팝업 canvas

    [Header("Armor Prefab & Content")]
    [Tooltip("방어구 정보를 보여줄 아이템 Prefab (여기 안에 5개의 이미지를 담을 자리가 있어야 함)")]
    public GameObject armorItemPrefab;      // 방어구 아이템용 Prefab (예: ArmorItemPrefab)
    public Transform armorContentParent;    // ScrollView의 Content
    public GameObject equipmentImagePrefab;

    // ★ (예시) 방어구 데이터 조회 API URL
    private string getArmorDataURL = "https://pandaraisegame-equipment.azurewebsites.net/api/FetchArmorData?code=wzSWxU1xLWBs5n2plo6UbvwPPrjiLYusnyg-EV7KecE0AzFujLxIkQ==";

    [Header("(옵션) 테스트용 무기 데이터")]
    public List<ArmorData> testArmorDatas; // 임시로 몇 개 넣어둘 무기 데이터 리스트

    public void OpenArmorPanel()
    {   // 팝업 활성화 
        if (ArmorPopupCanvas != null)
        {
            ArmorPopupCanvas.SetActive(true);
        }
        WeaponPopupManager.Instance.CloseWeaponPanel();
        SkillPopupManager.Instance.CloseSkillPanel();
        DungeonPopupManager.Instance.CloseDungeonPanel();

        ChatPopupManager.Instance.OpenMiddleChatPreview();  // 중앙에 채팅 미리보기 ui 생성
        ChatPopupManager.Instance.CloseBottomChatPreview(); // 하단에 채팅 미리보기 ui 제거
        ChatPopupManager.Instance.middlePreview = true;

        // server
        StartCoroutine(FetchArmorDataCoroutine());
    }
    public void CloseArmorPanel()
    {  // 팝업 비활성화
        if (ArmorPopupCanvas != null)
        {
            ArmorPopupCanvas.SetActive(false);
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
        yield return StartCoroutine(FetchArmorDataCoroutine());

        // 2) 코루틴 완료 후 비활성화
        ArmorPopupCanvas.SetActive(false);
        Debug.Log("[LeaderboardPopupManager] StartSequence() 완료 후 Canvas 비활성화");
    }

    private IEnumerator FetchArmorDataCoroutine()
    {
        Debug.Log("[ArmorPopupManager] FetchArmorDataCoroutine 시작");

        var payload = new
        {
            playFabId = GlobalData.playFabId
        };

        // JSON 직렬화
        string jsonData = JsonConvert.SerializeObject(payload);
        Debug.Log("[ArmorPopupManager] 요청 JSON: " + jsonData);

        // POST 요청 생성
        UnityWebRequest request = new UnityWebRequest(getArmorDataURL, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // 요청 전송 & 응답 대기
        yield return request.SendWebRequest();

        // 응답 처리
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[ArmorPopupManager] 방어구 데이터 조회 실패: " + request.error);
            ToastManager.Instance.ShowToast("방어구 데이터를 불러올 수 없습니다.");
        }
        else
        {
            string resp = request.downloadHandler.text;
            Debug.Log("[ArmorPopupManager] 방어구 데이터 조회 응답: " + resp);

            List<ArmorData> armorList = null;
            try
            {
                // 서버 응답(JSON) → List<ArmorData> 역직렬화
                armorList = JsonConvert.DeserializeObject<List<ArmorData>>(resp);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[ArmorPopupManager] JSON 파싱 오류: " + ex.Message);
                ToastManager.Instance.ShowToast("방어구 데이터 파싱 실패");
                yield break;
            }

            if (armorList == null)
            {
                Debug.LogWarning("[ArmorPopupManager] 서버에서 방어구 데이터가 없습니다.");
                ToastManager.Instance.ShowToast("방어구 데이터가 없습니다.");
            }
            else
            {
                // UI 갱신
                PopulateArmorItems(armorList);
            }
        }
    }

    // -----------------------[Prefab 생성 & 데이터 적용 메서드 추가]-----------------------
    /// <summary>
    /// ScrollView Content를 비우고, armorDataList에 있는 데이터만큼 방어구 아이템 Prefab을 생성해 배치합니다.
    /// </summary>
    public void PopulateArmorItems(List<ArmorData> armorDataList)
    {
        Debug.Log($"[ArmorPopupManager] PopulateArmorItems 호출됨, 데이터 수: {armorDataList?.Count ?? 0}");
        ClearArmorItems();

        if (armorDataList == null || armorDataList.Count == 0)
        {
            Debug.LogWarning("[ArmorPopupManager] 방어구 데이터가 없습니다.");
            return;
        }

        // 한 줄(한 Row_Panel)에 몇 개씩 보여줄지
        int itemsPerRow = 5;
        int totalCount = armorDataList.Count;

        // i = 0, 5, 10, 15 ... 형태로 증가
        for (int i = 0; i < totalCount; i += itemsPerRow)
        {
            // 이번에 만들 Row_Panel에 들어갈 데이터 개수
            // (마지막에 5개 미만이 남을 수도 있으니 min 처리)
            int chunkSize = Mathf.Min(itemsPerRow, totalCount - i);

            // [i..i+chunkSize-1] 구간의 데이터만 잘라서 한 줄에 표시
            List<ArmorData> rowData = armorDataList.GetRange(i, chunkSize);

            // 1) Row_Panel (ArmorItemPrefab) 생성
            GameObject rowPanelObj = Instantiate(armorItemPrefab, armorContentParent);

            var rowController = rowPanelObj.GetComponent<ArmorItemController>();
            if (rowController != null)
            {
                // SetData를 "List<ArmorData>"로 받을 수 있게 수정
                rowController.SetData(rowData, equipmentImagePrefab);
            }
        }
    }

    /// <summary>
    /// Content 안에 들어있는 자식 객체(기존 무기 아이템 Prefab)를 모두 제거합니다.
    /// </summary>
    private void ClearArmorItems()
    {
        Debug.Log($"[ArmorPopupManager] ClearArmorItems 호출됨, 자식 수: {armorContentParent.childCount}");
        for (int i = armorContentParent.childCount - 1; i >= 0; i--)
        {
            Debug.Log($"[ArmorPopupManager] 제거할 무기 아이템: {armorContentParent.GetChild(i).name}");
            Destroy(armorContentParent.GetChild(i).gameObject);
        }
    }
}
