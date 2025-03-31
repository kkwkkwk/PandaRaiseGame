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
    private string joinGuildURL = "https://pandaraisegame-guild.azurewebsites.net/api/JoinGuild?code=Lj1P0F-ktTP-bS_VaOKOj43lxnRKBYRY-s3estRhuBq_AzFuM3JVTQ==";

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
        var requestBody = new
        {
            playFabId = GlobalData.playFabId,      // 서버가 playFabId를 식별자로 사용
            guildId = guildData.guildId,         // 가입할 길드 ID
            entityToken = new
            {
                Entity = new
                {
                    Id = GlobalData.entityToken.Entity.Id,
                    Type = GlobalData.entityToken.Entity.Type
                },
                EntityToken = GlobalData.entityToken.EntityToken,
                TokenExpiration = GlobalData.entityToken.TokenExpiration
            }
        };

        string jsonData = JsonConvert.SerializeObject(requestBody);
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
            // 다시 신청 가능하도록
            isRequesting = false;
            if (guildRegisterButton != null)
                guildRegisterButton.interactable = true;
        }
        else
        {
            string responseJson = request.downloadHandler.text;
            Debug.Log($"[CallJoinGuildCoroutine] 가입 요청 성공. 응답: {responseJson}");

            // 서버 응답 DTO 파싱
            JoinGuildResponse res = null;
            try
            {
                res = JsonConvert.DeserializeObject<JoinGuildResponse>(responseJson);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[CallJoinGuildCoroutine] 응답 파싱 실패: " + ex.Message);
            }

            if (res == null)
            {
                ToastManager.Instance.ShowToast("알 수 없는 응답입니다.");
                isRequesting = false;
                if (guildRegisterButton != null)
                    guildRegisterButton.interactable = true;
                yield break;
            }

            if (res.success)
            {
                // "success = true" => 서버에서 가입 신청이 정상 처리됨
                // 실제로 바로 길드에 가입되는지, 아니면 '마스터 승인 대기' 상태인지는 서버 로직에 따라 달라짐
                Debug.Log("[CallJoinGuildCoroutine] 가입 신청 성공.");
                ToastManager.Instance.ShowToast("길드 가입 신청이 완료되었습니다.");
            }
            else
            {
                // 서버에서 success=false -> 사유(이미 가입 중, 조건 불만족 등)
                Debug.LogWarning($"[CallJoinGuildCoroutine] 가입 신청 실패: {res.message}");
                ToastManager.Instance.ShowToast(res.message);

                // 다시 신청 가능하도록 복원
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
