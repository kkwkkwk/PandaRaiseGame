using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class FarmPopupManager : MonoBehaviour
{

    public static FarmPopupManager Instance { get; private set; }

    [Header("Popup Canvas")]
    public GameObject FarmPopupCanvas;              // 농장 팝업 canvas

    [Header("Control Buttons")]
    public Button closeFarmBtn;                     // 농장 팝업 닫기 버튼
    public Button farmStatBtn;                      // 농장 능력치 버튼
    public Button farmInventoryBtn;                 // 농장 인벤토리 버튼

    [Header("Farm Plot Buttons (6개)")]
    public Button[] farmPlotButtons;                // 중앙의 6개 농지 패널 버튼

    [Header("Plot Detail Panel & ScrollView")]
    public GameObject plotDetailPanel;      // 전체 패널
    public Transform seedContentParent;     // ScrollView > Content
    public GameObject farmPlotSeedPrefab;   // FarmPlotSeed_Prefab

    [Header("Plot Detail Action Buttons")]
    public Button confirmPlantBtn;          // “심기” 버튼
    public Button cancelPlantBtn;           // “취소” 버튼

    private string getSeedsUrl;
    private string plantSeedUrl;

    private int _currentPlotIndex;
    private SeedData _selectedSeed;

    public void OpenFarmPanel() // 농장 팝업 열기
    {   // 팝업 활성화 
        if (FarmPopupCanvas != null)
        {
            FarmPopupCanvas.SetActive(true);
        }
        ChatPopupManager.Instance.ChatCanvas.SetActive(false);
    }
    public void CloseFarmPanel() // 농장 팝업 닫기
    {  // 팝업 비활성화
        if (FarmPopupCanvas != null)
            FarmPopupCanvas.SetActive(false);
        ChatPopupManager.Instance.ChatCanvas.SetActive(true);
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
    void Start()
    {
        // 닫기 버튼 연결
        if (closeFarmBtn != null)
            closeFarmBtn.onClick.AddListener(CloseFarmPanel);
        // 탭 버튼 클릭 리스너 추가
        if (farmStatBtn != null)
            farmStatBtn.onClick.AddListener(OnClickFarmStat);
        if (farmInventoryBtn != null)
            farmInventoryBtn.onClick.AddListener(OnClickFarmInventory);
        // 중앙 농지 패널 6개에 리스너 등록
        for (int i = 0; i < farmPlotButtons.Length; i++)
        {
            int index = i; // 클로저 문제 방지
            farmPlotButtons[i].onClick.AddListener(() => OnClickFarmPlot(index));
        }

        // PlotDetailPanel 내 액션 버튼
        confirmPlantBtn.onClick.AddListener(OnConfirmPlant);
        cancelPlantBtn.onClick.AddListener(OnCancelPlant);

        // 시작할 때 detail 패널은 숨겨두기
        if (plotDetailPanel != null)
            plotDetailPanel.SetActive(false);
    }

    public void OnClickFarmStat()   // 농장 능력치 버튼 클릭 시
    {

    }
    public void OnClickFarmInventory()  // 농장 인벤토리 버튼 클릭 시
    {

    }
    private void OnClickFarmPlot(int plotIndex)
    {
        _currentPlotIndex = plotIndex;
        _selectedSeed = null;              // 이전 선택 초기화
        plotDetailPanel.SetActive(true);
        StartCoroutine(GetSeedsForPlot());
    }
    public void OnSelectSeed(SeedData data)
    {
        _selectedSeed = data;
        confirmPlantBtn.interactable = true;  // 심기 가능
    }

    /// <summary>
    /// “심기” 버튼 클릭
    /// </summary>
    private void OnConfirmPlant()
    {
        if (_selectedSeed == null)
            return;

        StartCoroutine(PlantSeedRequestCoroutine(_currentPlotIndex, _selectedSeed.Id));
        plotDetailPanel.SetActive(false);
    }
    /// <summary>
    /// “취소” 버튼 클릭
    /// </summary>
    private void OnCancelPlant()
    {
        _selectedSeed = null;
        plotDetailPanel.SetActive(false);
    }
    private IEnumerator GetSeedsForPlot()
    {
        var reqObj = new { plotIndex = _currentPlotIndex, playFabId = GlobalData.playFabId };
        string body = JsonConvert.SerializeObject(reqObj);

        using (var uwr = new UnityWebRequest(getSeedsUrl, "POST"))
        {
            byte[] jsonToSend = System.Text.Encoding.UTF8.GetBytes(body);
            uwr.uploadHandler = new UploadHandlerRaw(jsonToSend);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("씨앗 리스트 조회 실패: " + uwr.error);
                yield break;
            }

            var resp = JsonConvert.DeserializeObject<SeedsResponse>(uwr.downloadHandler.text);
            if (resp == null || !resp.IsSuccess)
            {
                Debug.LogWarning("응답 에러: " + resp?.ErrorMessage);
                yield break;
            }
            RebuildSeedUI(resp.SeedsList);
        }
    }

    private void RebuildSeedUI(List<SeedData> seeds)
    {
        // 기존 제거
        foreach (Transform child in seedContentParent)
            Destroy(child.gameObject);

        // 새로 생성
        foreach (var seed in seeds)
        {
            var go = Instantiate(farmPlotSeedPrefab, seedContentParent);
            var ctrl = go.GetComponent<FarmPlotSeedController>();
            ctrl.Initialize(seed, this, _currentPlotIndex);
        }

        // Plant/Cancel 버튼 초기 상태
        confirmPlantBtn.interactable = false;
    }

    /// <summary>
    /// 씨앗 클릭 시 호출—단순 선택만
    /// </summary>


    private IEnumerator PlantSeedRequestCoroutine(int plotIndex, string seedId)
    {
        var req = new { playFabId = GlobalData.playFabId, plotIndex = plotIndex, seedId = seedId };
        string body = JsonConvert.SerializeObject(req);

        using (var uwr = new UnityWebRequest(plantSeedUrl, "POST"))
        {
            byte[] jsonToSend = System.Text.Encoding.UTF8.GetBytes(body);
            uwr.uploadHandler = new UploadHandlerRaw(jsonToSend);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("씨앗 심기 요청 실패: " + uwr.error);
                yield break;
            }
            Debug.Log("씨앗 심기 성공: " + uwr.downloadHandler.text);

            // TODO: 성공 UI 처리 (예: 농지 상태 갱신)
        }
    }

    

    // “심기” 버튼 누르면 서버에 요청
    public void OnPlantSeed(int plotIndex, string seedId)
    {
        Debug.Log($"Plot {plotIndex}에 Seed {seedId} 심기 요청");
        // TODO: PlantSeedRequestCoroutine(plotIndex, seedId) 구현
    }

    // “정보” 버튼 누르면 팝업 띄우기
    public void OnShowSeedInfo(SeedData data)
    {
        // 예: FarmSeedInfoPopupManager.Instance.Open(data);
    }
}
