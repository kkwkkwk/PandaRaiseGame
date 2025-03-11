using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// PlayFab
using PlayFab;
using PlayFab.ServerModels;
using CommonLibrary; // PlayFabConfig, AzureChatConfig 등

// Azure Web PubSub
using Azure;
using Azure.Messaging.WebPubSub;

namespace Chat
{
    public class GetChatConnection
    {
        private readonly ILogger<GetChatConnection> _logger;
        private const string HubName = "Chat";

        public GetChatConnection(ILogger<GetChatConnection> logger)
        {
            _logger = logger;
        }

        [Function("GetChatConnection")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            // 1) 함수 진입
            _logger.LogInformation("GetChatConnection 함수가 호출되었습니다.");

            // 2) 요청 바디 파싱
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation($"(1) Raw request body: {requestBody}");

            dynamic data = JsonConvert.DeserializeObject(requestBody)!;
            string playFabId = data.playFabId;
            if (string.IsNullOrEmpty(playFabId))
            {
                _logger.LogWarning("(1) playFabId가 누락되었습니다.");
                return new BadRequestObjectResult("PlayFabId가 필요합니다.");
            }
            _logger.LogInformation($"(1) playFabId = {playFabId}");

            // 3) PlayFab 설정 및 UserData 조회
            _logger.LogInformation("(2) PlayFabConfig.Configure() 호출");
            PlayFabConfig.Configure();

            _logger.LogInformation($"(2) GetUserDataAsync 호출. 플레이어 {playFabId}");
            var getUserDataReq = new GetUserDataRequest { PlayFabId = playFabId };
            var getUserDataResult = await PlayFabServerAPI.GetUserDataAsync(getUserDataReq);

            if (getUserDataResult.Error != null)
            {
                string err = getUserDataResult.Error.GenerateErrorReport();
                _logger.LogError($"(2) GetUserData 실패: {err}");
                return new BadRequestObjectResult("UserData 조회 오류: " + err);
            }

            var userData = getUserDataResult.Result.Data;
            if (userData == null || !userData.ContainsKey("Guild"))
            {
                _logger.LogWarning($"(2) {playFabId}의 Guild 정보 없음.");
                return new NotFoundObjectResult("Guild 정보가 없습니다.");
            }
            string guildName = userData["Guild"].Value;
            _logger.LogInformation($"(2) 길드: {guildName}");

            // 4) WebPubSubServiceClient 생성
            _logger.LogInformation("(3) WebPubSubServiceClient 생성 준비");
            string? endpoint = AzureChatConfig.GetEndpoint();
            string? accessKey = AzureChatConfig.GetAccessKey();

            // 로그 레벨이 Information 이상이라면 아래도 표시됨.
            // (보안을 위해 실제 키를 그대로 찍기보다는 길이만 표기)
            _logger.LogInformation($"(3) endpoint = {endpoint}");
            _logger.LogInformation($"(3) accessKey length = {accessKey.Length}");

            // 여기서 예외가 발생하면 이후 코드는 실행 안 되고, Azure Functions 런타임이 예외를 기록(스택 트레이스 포함)함.
            _logger.LogInformation("(3) new WebPubSubServiceClient(...) 호출");
            var serviceClient = new WebPubSubServiceClient(
                new Uri(endpoint),      // 여기서 UriFormatException 가능
                HubName,
                new AzureKeyCredential(accessKey)  // 여기서 인증 오류 등 가능
            );
            _logger.LogInformation("(3) WebPubSubServiceClient 생성 성공");

            // 5) Roles, Groups, 만료 시간 등
            _logger.LogInformation("(4) roles, groups, 만료 시간 설정");
            var roles = new string[]
            {
                $"webpubsub.joinLeaveGroup.{guildName}",
                $"webpubsub.sendToGroup.{guildName}"
            };
            var autoJoinGroups = new string[]
            {
                guildName
            };
            var expires = TimeSpan.FromMinutes(30);
            _logger.LogInformation($"(4) roles={JsonConvert.SerializeObject(roles)}, autoJoinGroups={JsonConvert.SerializeObject(autoJoinGroups)}, expires={expires.TotalMinutes}분");

            // 6) 클라이언트 URL 생성
            _logger.LogInformation("(5) GetClientAccessUri 호출");
            var clientUri = serviceClient.GetClientAccessUri(
                userId: playFabId,
                roles: roles,
                groups: autoJoinGroups,
                expiresAfter: expires
            );
            _logger.LogInformation($"(5) clientUri 생성 성공: {clientUri}");

            // 7) JSON 응답
            _logger.LogInformation("(6) 최종 OkObjectResult 반환");
            var resultObj = new
            {
                playFabId = playFabId,
                guild = guildName,
                hub = HubName,
                expiresInMinutes = 30,
                accessUrl = clientUri.ToString(),
                roles,
                autoJoinGroups
            };
            return new OkObjectResult(resultObj);
        }
    }
}
