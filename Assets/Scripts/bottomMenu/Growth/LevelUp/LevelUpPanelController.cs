using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class LevelUpPanelController : MonoBehaviour
{
    // (예시) 플레이어 정보 조회용
    private string getPlayerLevelDataURL = "https://pandaraisegame-growth.azurewebsites.net/api/GetPlayerLevelData?code=49zXxdVpa1K-LlnSPqHbYvaLVdgqtT1Um0m30uq3dyD_AzFuMdn7cw==";

    // (예시) 레벨 업 요청용
    private string postPlayerLevelUpURL = "https://pandaraisegame-growth.azurewebsites.net/api/PostPlayerLevelUp?code=xHxqs-xYf_G0D1UP7l79o0oxiLBtYEuhlKes76yQ840oAzFuY9_oYA==";

    [Header("UI References")]
    public TextMeshProUGUI levelText;  // 플레이어 레벨 표시
    public Slider expSlider;           // 경험치 바
    public Button levelUpButton;       // 레벨업 버튼

    [Header("Character Display")]
    public GameObject characterPrefab;  // 캐릭터(Animator) 프리팹
    public Transform characterParent;   // 캐릭터를 붙일 위치

    // 현재 플레이어 레벨/경험치 상태를 담을 DTO
    private PlayerLevelResponse _playerData;
    private GameObject _spawnedCharacter;

    void Start()
    {
        // 캐릭터 프리팹 생성 (부모(characterParent)가 할당되어 있다고 가정)
        if (characterPrefab != null && characterParent != null)
        {
            _spawnedCharacter = Instantiate(characterPrefab, characterParent);

            // RectTransform이 있을 경우: 하단 가운데에 위치
            RectTransform rectTransform = _spawnedCharacter.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 앵커와 피벗을 하단 가운데로 설정합니다.
                rectTransform.anchorMin = new Vector2(0.5f, 0f);
                rectTransform.anchorMax = new Vector2(0.5f, 0f);
                rectTransform.pivot = new Vector2(0.5f, 0f);
                // 부모의 하단 가운데에 고정 (anchoredPosition은 부모 기준 좌표)
                rectTransform.anchoredPosition = Vector2.zero;
            }
            else
            {
                // RectTransform이 없을 경우 일반 Transform 설정: 부모의 하단 가운데로 이동
                // 부모의 크기를 고려하여 위치를 조정해 주세요.
                _spawnedCharacter.transform.localPosition = new Vector3(0, 0, 0);
            }

            // Scale을 강제로 (35, 35, 1)로 고정
            _spawnedCharacter.transform.localScale = new Vector3(35f, 35f, 1f);

            // 팝업 배경보다 위에 보이도록 마지막 자식으로 변경
            _spawnedCharacter.transform.SetAsLastSibling();
        }

        if (levelUpButton != null)
            levelUpButton.onClick.AddListener(OnClickLevelUpButton);

        // 시작 시점(패널이 활성화될 때) 서버에 플레이어 정보 조회
        StartCoroutine(GetPlayerLevelDataCoroutine());
    }


    /// <summary>
    /// 서버(Azure Function)에 플레이어 레벨/경험치 정보 조회 요청 → 응답 파싱 후 UI 갱신
    /// </summary>
    private IEnumerator GetPlayerLevelDataCoroutine()
    {
        if (string.IsNullOrEmpty(getPlayerLevelDataURL))
        {
            Debug.LogError("[LevelUpPanel] getPlayerLevelDataURL이 비어있음.");
            yield break;
        }

        var requestJsonObject = new
        {
            playFabId = GlobalData.playFabId,
        };
        string jsonString = JsonConvert.SerializeObject(requestJsonObject);

        // POST 요청 생성
        UnityWebRequest request = new UnityWebRequest(getPlayerLevelDataURL, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonString);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // 서버 통신
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[LevelUpPanel] GetPlayerLevelData 실패: " + request.error);
            ToastManager.Instance.ShowToast($"데이터 조회 실패: {request.error}");
        }
        else
        {
            string responseJson = request.downloadHandler.text;
            Debug.Log("[LevelUpPanel] GetPlayerLevelData 응답: " + responseJson);

            // JSON -> PlayerLevelResponse 파싱
            PlayerLevelResponse response = null;
            try
            {
                response = JsonConvert.DeserializeObject<PlayerLevelResponse>(responseJson);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[LevelUpPanel] JSON 파싱 오류: " + ex.Message);
                ToastManager.Instance.ShowToast($"JSON 파싱 오류: {ex.Message}");
            }

            if (response != null)
            {
                if (!response.IsSuccess)
                {
                    Debug.LogWarning("[LevelUpPanel] 서버 응답: 실패");
                    ToastManager.Instance.ShowToast($"오류: {response.ErrorMessage}");
                }
                else
                {
                    // 데이터 저장 후 UI 반영
                    _playerData = response;
                    UpdateLevelUpUI();
                }
            }
        }
    }

    /// <summary>
    /// 레벨업 버튼 클릭 시 서버에 레벨업 요청 → 응답 파싱 후 UI 갱신
    /// </summary>
    private void OnClickLevelUpButton()
    {
        // (클라이언트 단에서 미리 조건 체크 - 실제로는 서버에서도 중복 체크)
        if (_playerData == null)
        {
            Debug.LogWarning("[LevelUpPanel] 플레이어 데이터가 없어 레벨업 불가");
            return;
        }
        if (_playerData.CurrentExp < _playerData.RequiredExp)
        {
            Debug.LogWarning("[LevelUpPanel] 경험치 부족, 레벨업 불가");
            ToastManager.Instance.ShowToast("경험치가 부족합니다");
            return;
        }

        // 서버에 레벨업 요청
        StartCoroutine(RequestLevelUpCoroutine());
    }

    /// <summary>
    /// 서버(Azure Function)에 레벨업 POST 요청 → 응답 파싱 → UI 갱신
    /// </summary>
    private IEnumerator RequestLevelUpCoroutine()
    {
        if (string.IsNullOrEmpty(postPlayerLevelUpURL))
        {
            Debug.LogError("[LevelUpPanel] postPlayerLevelUpURL이 비어있음.");
            yield break;
        }

        // POST Body
        var levelUp = new
        {
            playFabId = GlobalData.playFabId,
        };
        string jsonData = JsonConvert.SerializeObject(levelUp);

        UnityWebRequest request = new UnityWebRequest(postPlayerLevelUpURL, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[LevelUpPanel] RequestLevelUp 실패: " + request.error);
            ToastManager.Instance.ShowToast($"레벨업 실패: {request.error}");
        }
        else
        {
            string responseJson = request.downloadHandler.text;
            Debug.Log("[LevelUpPanel] RequestLevelUp 응답: " + responseJson);

            PlayerLevelResponse response = null;
            try
            {
                response = JsonConvert.DeserializeObject<PlayerLevelResponse>(responseJson);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[LevelUpPanel] JSON 파싱 오류: " + ex.Message);
                ToastManager.Instance.ShowToast($"JSON 파싱 오류: {ex.Message}");

            }

            if (response != null)
            {
                if (!response.IsSuccess)
                {
                    Debug.LogWarning("[LevelUpPanel] 서버 응답: 레벨업 실패");
                    ToastManager.Instance.ShowToast($"레벨업 오류: {response.ErrorMessage}");
                }
                else
                {
                    // 서버가 넘겨준 최신 레벨/경험치로 갱신
                    _playerData = response;
                    UpdateLevelUpUI();

                    ToastManager.Instance.ShowToast($"레벨업 성공! 현재 레벨: {_playerData.Level}");
                }
            }
        }
    }

    /// <summary>
    /// _playerData 기준으로 레벨/경험치 UI 갱신
    /// </summary>
    private void UpdateLevelUpUI()
    {
        if (_playerData == null) return;

        // 레벨
        if (levelText != null)
        {
            levelText.text = $"Lv. {_playerData.Level}";
        }

        // 경험치 바
        if (expSlider != null)
        {
            if (_playerData.RequiredExp > 0)
            {
                float ratio = (float)_playerData.CurrentExp / _playerData.RequiredExp;
                expSlider.value = Mathf.Clamp01(ratio);
            }
            else
            {
                expSlider.value = 0;
            }
        }
    }
}
