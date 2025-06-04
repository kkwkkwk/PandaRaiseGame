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

    [Header("Farm Main / Stat Panels")]
    public GameObject FarmMain_Panel;               // FarmMain_Panel (심기 / 수확 UI)
    public GameObject FarmStatPopup_Panel;          // FarmStatPopup_Panel (능력치 UI)

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

    [Header("Farm Plot Panel Backgrounds")]
    [Tooltip("각 농지 패널(GameObject)에 붙어 있는 배경 Image 컴포넌트를 순서대로 연결하세요.")]
    public Image[] plotPanelBackgrounds;

    [Header("Plot State Sprites")]
    public Sprite nonSeedSprite;       // 빈 땅 상태 이미지 (Non_Seed)
    public Sprite growingSeedSprite;   // 성장 중 상태 이미지 (Growing_seed)
    public Sprite grownBambooSprite;   // 다 자란 대나무 (Grown_Bamboo)
    public Sprite grownCarrotSprite;   // 다 자란 당근 (Grown_Carrot)
    public Sprite grownCornSprite;     // 다 자란 옥수수 (Grown_Corn)
    public Sprite grownGrapeSprite;    // 다 자란 포도 (Grown_Grape)

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
    }
    public void CloseFarmPanel() // 농장 팝업 닫기
    {  // 팝업 비활성화
        if (FarmPopupCanvas != null)
            FarmPopupCanvas.SetActive(false);
        ChatPopupManager.Instance.ChatCanvas.SetActive(true);
    }
    private void Awake() // Unity의 Awake() 메서드에서 싱글톤 설정
    {
        if (Instance == null)
            Instance = this; // 현재 객체를 싱글톤으로 지정
        else
            // 이미 싱글톤 인스턴스가 존재하면, 중복된 객체를 제거
            Destroy(gameObject);
    }
    private void OnEnable()
    {
        // 팝업을 열 때마다 1초 간격으로 UpdateFarmPlotsUI를 호출해서 '남은 시간'을 띄워 줌
        InvokeRepeating(nameof(UpdateFarmPlotsUI), 0f, 1f);
    }
    private void OnDisable()
    {
        // 팝업을 닫으면 반복 호출 중단
        CancelInvoke(nameof(UpdateFarmPlotsUI));
    }
    void Start()
    {
        // 닫기 버튼 연결
        if (closeFarmBtn != null)
            closeFarmBtn.onClick.AddListener(CloseFarmPanel);

        // 농장 스탯 버튼 리스터
        if (farmStatBtn != null)
            farmStatBtn.onClick.AddListener(OnClickFarmStat);
        // 수확 버튼 리스너
        if (harvestBtn != null)
            harvestBtn.onClick.AddListener(OnConfirmHarvest);
        // 중앙 농지 버튼 6개
        for (int i = 0; i < farmPlotButtons.Length; i++)
        {
            int index = i;
            if (farmPlotButtons[i] != null)
                farmPlotButtons[i].onClick.AddListener(() => OnClickFarmPlot(index));
        }

        // PlotDetailPanel 내 “심기/취소” 버튼
        if (confirmPlantBtn != null)
            confirmPlantBtn.onClick.AddListener(OnConfirmPlant);
        if (cancelPlantBtn != null)
            cancelPlantBtn.onClick.AddListener(OnCancelPlant);

        // 초기 상태: FarmMain_Panel 켜고, FarmStatPopup_Panel 끄기
        if (FarmMain_Panel != null)
            FarmMain_Panel.SetActive(true);
        if (FarmStatPopup_Panel != null)
            FarmStatPopup_Panel.SetActive(false);

        // 시작할 때 detail 패널은 숨겨두기
        if (plotDetailPanel != null)
            plotDetailPanel.SetActive(false);
        // 수확 패널 숨기기
        if (harvestPanel != null)
            harvestPanel.SetActive(false);

        // 만약 Inspector에서 Sprite를 할당하지 않았으면, Resources 폴더에서 로드
        if (nonSeedSprite == null)
            nonSeedSprite = Resources.Load<Sprite>("Sprites/Farm/Non_Seed");
        if (growingSeedSprite == null)
            growingSeedSprite = Resources.Load<Sprite>("Sprites/Farm/Growing_seed");
        if (grownBambooSprite == null)
            grownBambooSprite = Resources.Load<Sprite>("Sprites/Farm/Grown_Bamboo");
        if (grownCarrotSprite == null)
            grownCarrotSprite = Resources.Load<Sprite>("Sprites/Farm/Grown_Carrot");
        if (grownCornSprite == null)
            grownCornSprite = Resources.Load<Sprite>("Sprites/Farm/Grown_Corn");
        if (grownGrapeSprite == null)
            grownGrapeSprite = Resources.Load<Sprite>("Sprites/Farm/Grown_Grape");

        Debug.Log("[FarmPopup] Farm Start() called");
    }

    public IEnumerator StartSequence()
    {
        Debug.Log("[FarmPopup] StartSequence() 시작됨");
        // 1) 먼저 농장 정보 조회
        yield return StartCoroutine(CallGetFarmPopup());

        // 2) 코루틴 완료 후 팝업 꺼버리기
        if (FarmPopupCanvas != null)
        {
            FarmPopupCanvas.SetActive(false);
            Debug.Log("[FarmPopup] StartSequence() 완료 → FarmPopupCanvas.SetActive(false) 호출됨");
        }
        else
        {
            Debug.LogWarning("[FarmPopup] StartSequence()에서 FarmPopupCanvas가 null입니다.");
        }
        Debug.Log("[ProfilePopupManager] StartSequence() 완료 후 Canvas 비활성화");
    }

    /// <summary>
    /// 서버에서 농지 상태(6개 plots)와 씨앗 인벤토리 조회
    /// </summary>
    private IEnumerator CallGetFarmPopup()
    {
        Debug.Log("[FarmPopup] CallGetFarmPopup() 시작");

        var requestObj = new { playFabId = GlobalData.playFabId };
        string json = JsonConvert.SerializeObject(requestObj);

        using (var uwr = new UnityWebRequest(getFarmPopupUrl, "POST"))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            uwr.uploadHandler = new UploadHandlerRaw(body);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            Debug.Log("[FarmPopup] 서버에 GET 요청 보내는 중 → URL: " + getFarmPopupUrl);
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[FarmPopup] CallGetFarmPopup 실패: " + uwr.error);
                yield break;
            }

            Debug.Log("[FarmPopup] CallGetFarmPopup 응답 수신됨 → 응답 JSON: " + uwr.downloadHandler.text);

            var resp = JsonConvert.DeserializeObject<FarmPopupResponse>(uwr.downloadHandler.text);
            if (resp == null)
            {
                Debug.LogError("[FarmPopup] CallGetFarmPopup: JSON 파싱 결과가 null");
                yield break;
            }
            if (!resp.IsSuccess)
            {
                Debug.LogError("[FarmPopup] CallGetFarmPopup: 서버 응답 IsSuccess == false → " + resp.ErrorMessage);
                yield break;
            }

            // 데이터 저장
            _plots = resp.Plots;
            _inventory = resp.Inventory;
            Debug.Log($"[FarmPopup] 서버 데이터 파싱 완료 → Plots: {_plots.Count}개, Inventory: {_inventory.Count}개");

            // UI 갱신
            UpdateFarmPlotsUI();
            UpdateSeedInventoryUI();
            Debug.Log("[FarmPopup] CallGetFarmPopup: UI 업데이트 완료");
        }
    }

    /// <summary>
    /// 6개 plot 버튼에 상태(Non_Seed / Growing_seed / Grown_XXX) 적용
    /// </summary>
    private void UpdateFarmPlotsUI()
    {
        Debug.Log("[FarmPopup] UpdateFarmPlotsUI called");
        if (_plots == null || plotPanelBackgrounds == null)
            return;


        for (int i = 0; i < plotPanelBackgrounds.Length; i++)
        {
            var bgImg = plotPanelBackgrounds[i];
            if (bgImg == null)
                continue;

            // 버튼 텍스트 (Plot 번호, 상태 표시)
            var btn = farmPlotButtons[i];
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label == null)
                continue;

            // 해당 인덱스의 PlotInfo 찾기
            var plot = _plots.FirstOrDefault(p => p.PlotIndex == i);
            Debug.Log($"[FarmPopup] Plot {i} 상태: HasSeed={plot?.HasSeed}, SeedId={plot?.SeedId}, PlantedTime={plot?.PlantedTimeUtc}, Duration={plot?.GrowthDurationSeconds}");
            // 1) PlotInfo가 없거나 HasSeed == false → 빈 땅
            if (plot == null || !plot.HasSeed)
            {
                label.text = $"Plot {i}\n빈 땅";
                bgImg.sprite = nonSeedSprite;
                continue;
            }

            // 2) 심어져 있는 상태 → 자라는 중인지 vs 다 자란 상태인지 확인
            double elapsed = (DateTime.UtcNow - plot.PlantedTimeUtc).TotalSeconds;
            int remaining = Mathf.Clamp(plot.GrowthDurationSeconds - (int)elapsed, 0, plot.GrowthDurationSeconds);

            if (remaining > 0)
            {
                // 아직 자라는 중
                label.text = $"Plot {i}\n남은 {remaining}s";
                bgImg.sprite = growingSeedSprite;
            }
            else
            {
                // 다 자란 상태 (수확 가능)
                label.text = $"Plot {i}\n수확 가능";
                Debug.Log($"  → Plot {i}는 다 자람, grown 이미지 할당 (seedId={plot.SeedId})");

                // 약어→풀네임 매핑
                string normalizedSeedId = plot.SeedId.ToLower();
                switch (normalizedSeedId)
                {
                    case "ba":
                        normalizedSeedId = "bamboo";
                        break;
                    case "ca":
                        normalizedSeedId = "carrot";
                        break;
                    case "co":
                        normalizedSeedId = "corn";
                        break;
                    case "gr":
                        normalizedSeedId = "grape";
                        break;
                    default:
                        normalizedSeedId = "NON";
                        break;
                }

                // plot.SeedId 에 담긴 값(예: "bamboo", "carrot", "corn", "grape")에 따라 다른 스프라이트 적용
                switch (normalizedSeedId)
                {
                    case "bamboo":
                        bgImg.sprite = grownBambooSprite;
                        break;
                    case "carrot":
                        bgImg.sprite = grownCarrotSprite;
                        break;
                    case "corn":
                        bgImg.sprite = grownCornSprite;
                        break;
                    case "grape":
                        bgImg.sprite = grownGrapeSprite;
                        break;
                    default:
                        // 만약 다른 작물이 있다면 nonSeedSprite 로 대체
                        bgImg.sprite = nonSeedSprite;
                        break;
                }
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

    /// <summary>
    /// “능력치” 버튼 클릭 시 FarmMain_Panel을 끄고, FarmStatPopup_Panel을 켜기
    /// </summary>
    private void OnClickFarmStat()
    {
        if (FarmMain_Panel != null)
            FarmMain_Panel.SetActive(false);

        if (FarmStatPopup_Panel != null)
            FarmStatPopup_Panel.SetActive(true);
    }

    private void OnClickFarmPlot(int plotIndex) // 수확 버튼 클릭 시
    {
        Debug.Log($"[FarmPopup] OnClickFarmPlot 호출됨 → plotIndex = {plotIndex}");
        // _plots가 아직 없거나, Index 범위가 올바르지 않으면 클릭 무시
        if (_plots == null)
        {
            Debug.LogWarning("[FarmPopup] _plots가 아직 준비되지 않아서 클릭을 무시합니다.");
            return;
        }
        if (plotIndex < 0 || plotIndex >= _plots.Count)
        {
            Debug.LogWarning($"[FarmPopup] plotIndex {plotIndex}가 _plots 범위를 벗어났습니다.");
            return;
        }

        _currentPlotIndex = plotIndex;
        _selectedSeed = null;

        // detail, harvest 패널 숨김
        if (plotDetailPanel != null) plotDetailPanel.SetActive(false);
        if (harvestPanel != null) harvestPanel.SetActive(false);

        var plot = _plots.FirstOrDefault(p => p.PlotIndex == plotIndex);
        if (plot != null && plot.HasSeed)
        {
            double elapsed = (DateTime.UtcNow - plot.PlantedTimeUtc).TotalSeconds;
            int remaining = Mathf.Clamp(plot.GrowthDurationSeconds - (int)elapsed, 0, plot.GrowthDurationSeconds);

            if (remaining > 0)
            {
                // 아직 성장 중이므로 재심기 금지
                Debug.Log($"Plot {plotIndex}은 아직 성장 중입니다. 남은 시간: {remaining}s");
                return;
            }
            else
            {
                // 성장 완료 → 수확 패널 열기
                if (harvestPanel != null)
                    harvestPanel.SetActive(true);
                return;
            }
        }

        // 빈 땅 → 심기 스크롤뷰 열기
        if (plotDetailPanel != null)
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
        if (confirmPlantBtn != null)
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
