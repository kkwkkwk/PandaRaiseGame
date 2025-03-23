using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;

public class GuildListItemController : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI guildNameText;
    public TextMeshProUGUI guildLevelText;
    public TextMeshProUGUI guildHeadCountText;
    public Button guildRegisterButton;

    // 최대 인원 계산 규칙
    public int baseCapacity = 30;
    public int incrementPerLevel = 2;

    // 실제 길드 가입(요청) API 주소 & key로 교체
    private string joinGuildURL = "https://your-azure-function-url/JoinGuild?code=YOUR_FUNCTION_KEY";

    // 이미 가입 신청을 눌렀는지 표시하기 위한 플래그
    private bool isRequesting = false;

    public void SetData(GuildData guildData)
    {
        if (guildNameText != null)
            guildNameText.text = guildData.guildName;

        if (guildLevelText != null)
            guildLevelText.text = "Lv. " + guildData.guildLevel.ToString();

        if (guildHeadCountText != null)
        {
            int maxCapacity = baseCapacity + incrementPerLevel * (guildData.guildLevel - 1);
            guildHeadCountText.text = $"{guildData.guildHeadCount} / {maxCapacity}";
        }

        if (guildRegisterButton != null)
        {
            // isRequesting이 false일 때만 버튼 누를 수 있게 (SetData는 새 아이템마다 호출)
            guildRegisterButton.interactable = !isRequesting;
            guildRegisterButton.onClick.RemoveAllListeners();
            guildRegisterButton.onClick.AddListener(() => OnJoinGuild(guildData));
        }
    }

    private void OnJoinGuild(GuildData guildData)
    {
        // 1) 이미 요청 중이면 무시
        if (isRequesting)
        {
            Debug.LogWarning($"이미 가입 신청을 누른 상태입니다. guildId={guildData.guildId}");
            ToastManager.Instance.ShowToast("이미 가입 신청 중입니다.");
            return;
        }

        // 2) 인원 체크
        int maxCapacity = baseCapacity + incrementPerLevel * (guildData.guildLevel - 1);
        if (guildData.guildHeadCount >= maxCapacity)
        {
            Debug.LogWarning($"[OnJoinGuild] {guildData.guildName} (ID={guildData.guildId}) 길드는 이미 인원 가득. 가입 불가.");
            ToastManager.Instance.ShowToast("해당 길드는 인원이 꽉 찬 상태입니다.");
            return;
        }

        // 3) 가입 신청 시작
        isRequesting = true;
        if (guildRegisterButton != null)
            guildRegisterButton.interactable = false;

        StartCoroutine(CallJoinGuildCoroutine(guildData));
    }

    /// <summary>
    /// 서버에 길드 가입을 요청하는 코루틴
    /// </summary>
    private IEnumerator CallJoinGuildCoroutine(GuildData guildData)
    {
        // 서버로 보낼 JSON: { "playFabId":"...", "guildId":"..." }
        string playFabId = GlobalData.playFabId;  // 전역에서 관리하는 플레이Fab ID
        string jsonData = $"{{\"playFabId\":\"{playFabId}\",\"guildId\":\"{guildData.guildId}\"}}";

        Debug.Log($"[CallJoinGuildCoroutine] 요청 JSON: {jsonData}");

        UnityWebRequest request = new UnityWebRequest(joinGuildURL, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[CallJoinGuildCoroutine] 가입 요청 실패: {request.error}");
            ToastManager.Instance.ShowToast("길드 가입 신청에 실패하였습니다.");
            isRequesting = false;
            if (guildRegisterButton != null)
                guildRegisterButton.interactable = true;
        }
        else
        {
            string responseJson = request.downloadHandler.text;
            Debug.Log($"[CallJoinGuildCoroutine] 가입 요청 성공. 응답: {responseJson}");
            ToastManager.Instance.ShowToast("길드 가입 신청에 성공하였습니다.");
            JoinGuildResponse res = JsonConvert.DeserializeObject<JoinGuildResponse>(responseJson);
            if (res != null)
            {
                if (res.success)
                {
                    // 여기서부터 "가입 요청 성공" 시점
                    Debug.Log("[CallJoinGuildCoroutine] 가입 신청 성공");
                }
                else
                {
                    // success=false -> 서버에서 "이미 가입중이거나, 조건 불만족" 등 사유
                    Debug.LogWarning($"[CallJoinGuildCoroutine] 가입 신청 실패 메시지: {res.message}");
                    isRequesting = false;
                    if (guildRegisterButton != null)
                        guildRegisterButton.interactable = true;
                }
            }
            else
            {
                // 어떤 이유로든 JSON 구조가 다르거나 빈 응답
                Debug.LogWarning("[CallJoinGuildCoroutine] 응답 DTO 파싱 실패");
                isRequesting = false;
                if (guildRegisterButton != null)
                    guildRegisterButton.interactable = true;
            }
        }
    }

    [System.Serializable]
    private class JoinGuildResponse
    {
        public bool success;
        public string message;
    }
}
