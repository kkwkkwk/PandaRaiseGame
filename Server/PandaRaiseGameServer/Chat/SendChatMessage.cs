using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using CommonLibrary; // PlayFabConfig, AzureChatConfig

using Azure;
using Azure.Messaging.WebPubSub;

namespace Chat
{
    public class SendChatMessage
    {
        private readonly ILogger<SendChatMessage> _logger;

        // 허브 이름은 "Chat"으로 고정 (GetChatConnection과 동일)
        private const string HubName = "Chat";

        public SendChatMessage(ILogger<SendChatMessage> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 클라이언트에서 { "playFabId": "...", "content": "..." } 형태의 JSON을 보내면
        /// 해당 플레이어의 길드를 조회하여, "Chat" 허브의 길드 그룹에 메시지를 전송한다.
        /// </summary>
        [Function("SendChatMessage")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("SendChatMessage 함수가 호출되었습니다.");

            // 1) 요청 바디 파싱
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation($"요청 바디: {requestBody}");

            dynamic data = JsonConvert.DeserializeObject(requestBody)!;
            string playFabId = data.playFabId;
            string content = data.content;

            if (string.IsNullOrEmpty(playFabId) || string.IsNullOrEmpty(content))
            {
                _logger.LogWarning("playFabId 또는 content가 누락됨.");
                return new BadRequestObjectResult("playFabId와 content를 모두 포함해야 합니다.");
            }

            // 2) PlayFab 설정 적용 + 길드 정보 조회
            PlayFabConfig.Configure();
            _logger.LogInformation($"플레이어 {playFabId}의 길드(UserData) 조회 중...");

            var getUserDataReq = new GetUserDataRequest { PlayFabId = playFabId };
            var getUserDataResult = await PlayFabServerAPI.GetUserDataAsync(getUserDataReq);
            if (getUserDataResult.Error != null)
            {
                _logger.LogError("GetUserData 실패: " + getUserDataResult.Error.GenerateErrorReport());
                return new BadRequestObjectResult("UserData 조회 오류: " + getUserDataResult.Error.GenerateErrorReport());
            }

            var userData = getUserDataResult.Result.Data;
            if (userData == null || !userData.ContainsKey("Guild"))
            {
                _logger.LogWarning($"플레이어 {playFabId}의 Guild 정보가 없음.");
                return new NotFoundObjectResult("Guild 정보가 없습니다. (UserData['Guild'] 누락)");
            }
            string guildName = userData["Guild"].Value;
            _logger.LogInformation($"플레이어 {playFabId}의 길드 = {guildName}");

            // 3) Azure Web PubSub 연결 정보
            //    Hub = "Chat", 그룹 = guildName
            string endpoint = AzureChatConfig.GetEndpoint();
            string accessKey = AzureChatConfig.GetAccessKey();

            var serviceClient = new WebPubSubServiceClient(
                new System.Uri(endpoint),
                HubName,
                new AzureKeyCredential(accessKey)
            );

            // 4) 메시지 전송
            //    "guildName" 그룹에게 (userId + 메시지) 형태로 보냄
            //    예: "Player1: Hello guild!"
            string broadcastMsg = $"{playFabId}: {content}";
            _logger.LogInformation($"그룹 {guildName} 에 메시지 전송: {broadcastMsg}");

            await serviceClient.SendToGroupAsync(guildName, broadcastMsg);
            _logger.LogInformation("메시지 전송 완료.");

            // 5) 응답
            var resultObj = new
            {
                playFabId = playFabId,
                guild = guildName,
                content = content,
                messageSent = broadcastMsg
            };
            return new OkObjectResult(resultObj);
        }
    }
}
