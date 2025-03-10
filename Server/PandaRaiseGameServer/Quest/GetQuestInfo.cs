using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using CommonLibrary; // PlayFabConfig.Configure() 등이 정의되어 있다고 가정
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Quest
{
    public class GetQuestInfo
    {
        private readonly ILogger<GetQuestInfo> _logger;

        public GetQuestInfo(ILogger<GetQuestInfo> logger)
        {
            _logger = logger;
        }

        [Function("GetQuestInfo")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("GetQuestInfo 함수가 호출되었습니다.");

            // 1. HTTP 요청에서 JSON 바디 파싱
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation($"요청 바디: {requestBody}");

            // 2. PlayFab 설정 (TitleId, SecretKey 등)
            PlayFabConfig.Configure();

            // 3. PlayFab에서 Title Data (QuestData) 조회
            _logger.LogInformation("QuestData 타이틀 데이터 조회 중...");
            var titleDataRequest = new GetTitleDataRequest
            {
                Keys = new List<string> { "QuestData" }
            };

            var titleDataResult = await PlayFabServerAPI.GetTitleDataAsync(titleDataRequest);
            if (titleDataResult.Error != null)
            {
                _logger.LogError("GetTitleData 실패: " + titleDataResult.Error.GenerateErrorReport());
                return new BadRequestObjectResult("GetTitleData 오류: " + titleDataResult.Error.GenerateErrorReport());
            }
            _logger.LogInformation("QuestData 타이틀 데이터 조회 성공.");

            // 4. QuestData 키의 값 가져오기
            string? questDataJson = null;
            if (titleDataResult.Result.Data != null && titleDataResult.Result.Data.ContainsKey("QuestData"))
            {
                questDataJson = titleDataResult.Result.Data["QuestData"];
                _logger.LogInformation("QuestData 값: " + questDataJson);
            }
            else
            {
                _logger.LogWarning("QuestData 키가 타이틀 데이터에 존재하지 않습니다.");
                return new NotFoundObjectResult("QuestData가 존재하지 않습니다.");
            }

            // 5. 응답용 DTO 대신 단순 문자열로 반환
            var response = new { QuestData = questDataJson };
            _logger.LogInformation("응답용 객체 작성 완료. Response 내용: " + JsonConvert.SerializeObject(response));

            // 6. JSON 응답 반환
            _logger.LogInformation("JSON 응답 반환 준비 완료. Response 내용: " + JsonConvert.SerializeObject(response));
            return new OkObjectResult(response);
        }
    }
}
