using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
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

    [Header("Harvest Panel & Buttons")]
    public GameObject harvestPanel;           // 수확 패널
    public Button harvestBtn;                 // 수확 버튼

    private string getSeedsUrl = "https://pandaraisegame-farm.azurewebsites.net/api/GetSeeds?code=Tv8mHmBDbE6pz84BooiWxqK6tW2shaP-bRoeNJNvAQq1AzFuuGuDdw==";
    private string plantSeedUrl = "https://pandaraisegame-farm.azurewebsites.net/api/PlantSeed?code=JH5yYearnjOvVi9T9NT1NRFVKe6Li-SL17KXvFwElu1wAzFud9aKlQ==";
    private string getFarmPopupUrl = "https://pandaraisegame-farm.azurewebsites.net/api/GetFarmPopup?code=pXQfhig4Ik9qwKpu5ymGf5pml0UEnfRpqehn3kNUnh3cAzFunjCvpQ==";
    private string harvestSeedUrl = "https://pandaraisegame-farm.azurewebsites.net/api/HarvestSeed?code=vIsoXbjWnezNCwy9MbFwhcb7AAVnYDOrBTFEjX4ZiQj6AzFujmRhww==";             // 수확 요청 URL

    // 받아온 데이터 저장

    // 서버로부터 받아온 농지 6칸의 상태 정보 목록
    private List<PlotInfo> _plots;
    // 플레이어 인벤토리에서 보유한 씨앗 종류별 개수 정보 목록
    private List<UserSeedInfo> _inventory;
    // 사용자가 클릭한 농지 패널의 인덱스 (0부터 5까지)
    private int _currentPlotIndex;
    // PlotDetailPanel에서 사용자가 선택한 씨앗 데이터
    private SeedData _selectedSeed;

    public void OpenFarmPanel() // 농장 팝업 열기
    {   // 팝업 활성화 
        if (FarmPopupCanvas != null)
        {
            FarmPopupCanvas.SetActive(true);
        }
        ChatPopupManager.Instance.ChatCanvas.SetActive(false);

        StartCoroutine(CallGetFarmPopup());
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

        // 농장 스탯 버튼 리스터
        if (farmStatBtn != null)
            farmStatBtn.onClick.AddListener(OnClickFarmStat);
        // 농장 인벤토리 버튼 리스너
        if (farmInventoryBtn != null)
            farmInventoryBtn.onClick.AddListener(OnClickFarmInventory);
        // 수확 버튼 리스너
        if (harvestBtn != null)
            harvestBtn.onClick.AddListener(OnConfirmHarvest);
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
        // 수확 패널 숨기기
        if (harvestPanel != null)
            harvestPanel.SetActive(false);
    }

    public IEnumerator StartSequence()
    {
        // 1) 먼저 농장 정보 조회
        yield return StartCoroutine(CallGetFarmPopup());

        // 2) 코루틴 완료 후 비활성화
        FarmPopupCanvas.SetActive(false);
        Debug.Log("[ProfilePopupManager] StartSequence() 완료 후 Canvas 비활성화");
    }

    /// <summary>
    /// 서버에서 농지 상태(6개 plots)와 씨앗 인벤토리 조회
    /// </summary>
    private IEnumerator CallGetFarmPopup()
    {
        var requestObj = new { playFabId = GlobalData.playFabId };
        string json = JsonConvert.SerializeObject(requestObj);

        using (var uwr = new UnityWebRequest(getFarmPopupUrl, "POST"))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            uwr.uploadHandler = new UploadHandlerRaw(body);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[FarmPopup] CallGetFarmPopup 실패: " + uwr.error);
                yield break;
            }

            var resp = JsonConvert.DeserializeObject<FarmPopupResponse>(uwr.downloadHandler.text);
            if (resp == null || !resp.IsSuccess)
            {
                Debug.LogError("[FarmPopup] 응답 에러: " + (resp?.ErrorMessage ?? "NULL"));
                yield break;
            }

            // 1) 데이터 저장
            _plots = resp.Plots;
            _inventory = resp.Inventory;

            // 2) UI 갱신
            UpdateFarmPlotsUI();
            UpdateSeedInventoryUI();
        }
    }

    /// <summary>
    /// 6개 plot 버튼에 상태(씨앗 심김/빈땅/성장단계) 적용
    /// </summary>
    private void UpdateFarmPlotsUI()
    {
        // _plots가 아직 준비되지 않았다면 무시
        if (_plots == null || farmPlotButtons == null) return;

        for (int i = 0; i < farmPlotButtons.Length; i++)
        {
            var btn = farmPlotButtons[i];
            if (btn == null) continue;

            // 버튼 안에 TextMeshProUGUI가 꼭 있다고 가정하지 말고 미리 꺼내 두세요
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label == null) continue;

            // PlotInfo를 안전하게 꺼냅니다.
            var plot = _plots.FirstOrDefault(p => p.PlotIndex == i);
            if (plot == null)
            {
                // 서버에 데이터가 없으면 '알 수 없음' 등으로 표시
                label.text = $"Plot {i}\n상태 알 수 없음";
                continue;
            }

            if (plot.HasSeed)
            {
                // 기존 로직—널 체크가 끝난 뒤에는 안전하게 접근 가능
                double elapsed = (DateTime.UtcNow - plot.PlantedTimeUtc).TotalSeconds;
                int remaining = Mathf.Clamp(plot.GrowthDurationSeconds - (int)elapsed, 0, plot.GrowthDurationSeconds);
                if (remaining > 0)
                    label.text = $"Plot {i}\n남은 {remaining}s";
                else
                    label.text = $"Plot {i}\n성장 완료!";
            }
            else
            {
                label.text = $"Plot {i}\n빈 땅";
            }
        }
    }

    /// <summary>
    /// 인벤토리 패널(별도 UI)에 소유 씨앗 목록과 개수 표시
    /// </summary>
    private void UpdateSeedInventoryUI()
    {
        // TODO: 인벤토리 전용 패널이 있다면, inventory 데이터를 바인딩
        // 예) inventoryPanel.Refresh(_inventory);
        foreach (var item in _inventory)
            Debug.Log($"[Inventory] {item.SeedName}: {item.Count}개");
    }

    public void OnClickFarmStat()   // 농장 능력치 버튼 클릭 시
    {

    }
    public void OnClickFarmInventory()  // 농장 인벤토리 버튼 클릭 시
    {

    }
    private void OnClickFarmPlot(int plotIndex) // 수확 버튼 클릭 시
    {
        _currentPlotIndex = plotIndex;
        _selectedSeed = null;

        var plot = _plots.Find(p => p.PlotIndex == plotIndex);
        if (plot != null && plot.HasSeed)
        {
            var elapsed = (DateTime.UtcNow - plot.PlantedTimeUtc).TotalSeconds;
            var remaining = Mathf.Clamp(plot.GrowthDurationSeconds - (int)elapsed, 0, plot.GrowthDurationSeconds);

            if (remaining <= 0)
            {
                // 성장 완료: 수확 패널 보여주기
                harvestPanel.SetActive(true);
                plotDetailPanel.SetActive(false);
                return;
            }
        }

        // 그 외: 기존 심기/선택 로직
        plotDetailPanel.SetActive(true);
        StartCoroutine(GetSeedsForPlot());
    }
    /// <summary>
    /// 수확 버튼 클릭 시 호출
    /// </summary>
    private void OnConfirmHarvest()
    {
        harvestPanel.SetActive(false);
        StartCoroutine(HarvestCropCoroutine(_currentPlotIndex));
    }

    /// <summary>
    /// 서버에 수확 요청을 보내고, 로컬 데이터 및 UI 갱신
    /// </summary>
    private IEnumerator HarvestCropCoroutine(int plotIndex)
    {
        var req = new
        {
            playFabId = GlobalData.playFabId,
            plotIndex = plotIndex
        };
        string body = JsonConvert.SerializeObject(req);

        using var uwr = new UnityWebRequest(harvestSeedUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
        uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
        uwr.downloadHandler = new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");

        yield return uwr.SendWebRequest();
        if (uwr.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[FarmPopup] 수확 요청 실패: {uwr.error}");
            yield break;
        }

        var resp = JsonConvert.DeserializeObject<HarvestSeedResponse>(uwr.downloadHandler.text);
        if (resp == null || !resp.IsSuccess)
        {
            Debug.LogWarning($"[FarmPopup] 수확 실패: {resp?.ErrorMessage}");
            yield break;
        }

        // 로컬 데이터 업데이트: 농지 초기화
        var plot = _plots.Find(p => p.PlotIndex == plotIndex);
        if (plot != null)
        {
            plot.HasSeed = false;
            plot.SeedId = null;
            plot.PlantedTimeUtc = DateTime.MinValue;
            plot.GrowthDurationSeconds = 0;
        }

        // UI 갱신
        UpdateFarmPlotsUI();
        Debug.Log($"[FarmPopup] Plot {plotIndex} 수확 완료, {resp.StatType} +{resp.Amount}");
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
    /// 선택한 농지(plotIndex)에 seedId를 심는 서버 요청 및 로컬 상태·UI 갱신
    /// </summary>
    private IEnumerator PlantSeedRequestCoroutine(int plotIndex, string seedId)
    {
        // 1) 요청 바디 생성
        var req = new
        {
            playFabId = GlobalData.playFabId,
            plotIndex = plotIndex,
            seedId = seedId
        };
        string bodyJson = JsonConvert.SerializeObject(req);

        using (var uwr = new UnityWebRequest(plantSeedUrl, "POST"))
        {
            // 2) 바디 설정 및 헤더
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(bodyJson);
            uwr.uploadHandler = new UploadHandlerRaw(jsonBytes);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            // 3) 서버 요청
            yield return uwr.SendWebRequest();

            // 4) 네트워크 에러 처리
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[FarmPopup] 씨앗 심기 요청 실패: {uwr.error}");
                yield break;
            }

            // 5) 응답 파싱
            var resp = JsonConvert.DeserializeObject<PlantSeedResponse>(uwr.downloadHandler.text);
            if (resp == null || !resp.IsSuccess)
            {
                Debug.LogWarning($"[FarmPopup] 씨앗 심기 실패: {resp?.ErrorMessage}");
                yield break;
            }

            // 6) 로컬 데이터 업데이트
            var plot = _plots.Find(p => p.PlotIndex == plotIndex);
            if (plot != null)
            {
                plot.HasSeed = true;                                   // 심기 상태 반영
                plot.SeedId = seedId;                                  // 심어진 씨앗 ID 저장
                plot.PlantedTimeUtc = DateTime.UtcNow;                 // 심기 시각 현재로 설정
                plot.GrowthDurationSeconds = resp.GrowthDurationSeconds; // 서버가 내려준 성장 시간
            }

            // 7) UI 갱신
            UpdateFarmPlotsUI();    // 농지 버튼 상태(텍스트/아이콘 등) 업데이트
            Debug.Log($"[FarmPopup] Plot {plotIndex}에 Seed {seedId} 심기 완료, UI 갱신");
        }
    }

    // “심기” 버튼 누르면 서버에 요청
    public void OnPlantSeed(int plotIndex, string seedId)
    {
        Debug.Log($"Plot {plotIndex}에 Seed {seedId} 심기 요청");
        StartCoroutine(PlantSeedRequestCoroutine(plotIndex, seedId));
    }

    // 프리팹 내 “정보” 버튼 누르면 팝업 띄우기
    public void OnShowSeedInfo(SeedData data)
    {
        // 예: FarmSeedInfoPopupManager.Instance.Open(data);
    }
}
