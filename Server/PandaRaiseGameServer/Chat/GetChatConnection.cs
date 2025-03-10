using System;
using System.IO;
using System.Net;
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

        // 허브 이름 고정
        private const string HubName = "Chat";

        public GetChatConnection(ILogger<GetChatConnection> logger)
        {
            _logger = logger;
        }

        [Function("GetChatConnection")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("GetChatConnection 함수가 호출되었습니다.");

            // 1) 요청 바디 파싱
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody)!;
            string playFabId = data.playFabId;
            if (string.IsNullOrEmpty(playFabId))
            {
                _logger.LogWarning("playFabId가 누락되었습니다.");
                return new BadRequestObjectResult("PlayFabId가 필요합니다.");
            }

            // 2) PlayFab 설정 및 UserData 조회
            PlayFabConfig.Configure();
            _logger.LogInformation($"플레이어 {playFabId}의 Guild 정보 조회 중...");
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
                _logger.LogWarning($"{playFabId}의 Guild 정보 없음.");
                return new NotFoundObjectResult("Guild 정보가 없습니다.");
            }
            string guildName = userData["Guild"].Value;
            _logger.LogInformation($"길드: {guildName}");

            // 3) Web PubSubServiceClient 생성
            string endpoint = AzureChatConfig.GetEndpoint();   //환경 변수
            string accessKey = AzureChatConfig.GetAccessKey(); //환경 변수
            var serviceClient = new WebPubSubServiceClient(
                new Uri(endpoint),
                HubName,
                new AzureKeyCredential(accessKey)
            );

            // 4) Roles, Groups, 만료 시간 등 구성
            // 예: “guildName” 그룹에 메시지 전송과 joinLeave 권한 부여
            // 예: URL 만료 시간을 30분으로 설정
            var roles = new string[]
            {
                $"webpubsub.joinLeaveGroup.{guildName}",  // 그룹 join/leave 권한
                $"webpubsub.sendToGroup.{guildName}"      // 해당 그룹에 메시지 전송 권한
            };

            // 연결 시 자동 가입할 그룹(guildName)
            var autoJoinGroups = new string[]
            {
                guildName
            };

            var expires = TimeSpan.FromMinutes(30); // URL 만료 30분

            // 5) 클라이언트 URL 생성
            var clientUri = serviceClient.GetClientAccessUri(
                userId: playFabId,
                roles: roles,              // 역할(권한)
                groups: autoJoinGroups,    // 자동 가입 그룹
                expiresAfter: expires      // 만료 시간
            );

            _logger.LogInformation($"클라이언트 URL 생성. hub=Chat, group={guildName}, expires=30분");

            // 6) JSON 응답
            var resultObj = new
            {
                playFabId = playFabId,
                guild = guildName,
                hub = HubName,
                expiresInMinutes = 30,
                accessUrl = clientUri.ToString(),
                roles = roles,
                autoJoinGroups = autoJoinGroups
            };

            return new OkObjectResult(resultObj);
        }
    }
}
