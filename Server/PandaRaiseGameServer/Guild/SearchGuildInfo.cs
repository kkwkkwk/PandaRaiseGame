using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using PlayFab;
using PlayFab.GroupsModels;
using PlayFab.AuthenticationModels;
using PlayFab.ServerModels;  // TitleData 관련
using CommonLibrary;

namespace Guild
{
    public class SearchGuildInfo
    {
        private readonly ILogger<SearchGuildInfo> _logger;

        public SearchGuildInfo(ILogger<SearchGuildInfo> logger)
        {
            _logger = logger;
        }

        [Function("SearchGuildInfo")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("[SearchGuildInfo] Function invoked.");

                // 1) 요청 바디 읽기
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation("[SearchGuildInfo] Request body: " + requestBody);

                // 2) JSON 파싱 및 유효성 검사
                var input = JsonConvert.DeserializeObject<GuildSearchRequest>(requestBody);
                if (input == null || string.IsNullOrEmpty(input.SearchQuery))
                {
                    _logger.LogWarning("[SearchGuildInfo] searchQuery missing in request.");
                    return new BadRequestObjectResult("Invalid request: searchQuery is required.");
                }
                if (input.EntityToken == null ||
                    string.IsNullOrEmpty(input.EntityToken.EntityToken) ||
                    input.EntityToken.Entity == null ||
                    string.IsNullOrEmpty(input.EntityToken.Entity.Id) ||
                    string.IsNullOrEmpty(input.EntityToken.Entity.Type))
                {
                    _logger.LogWarning("[SearchGuildInfo] entityToken is missing or incomplete.");
                    return new BadRequestObjectResult("Invalid request: entityToken is required.");
                }
                string searchQuery = input.SearchQuery;

                // 3) PlayFab 초기 설정 (TitleId, SecretKey 등)
                PlayFabConfig.Configure();
                _logger.LogInformation("[SearchGuildInfo] PlayFab configured.");

                // 4) TitleData에서 "GuildData" 키의 JSON 불러오기
                var titleDataRequest = new GetTitleDataRequest { Keys = new List<string> { "GuildData" } };
                var titleDataResponse = await PlayFabServerAPI.GetTitleDataAsync(titleDataRequest);
                if (titleDataResponse.Error != null)
                {
                    _logger.LogError("[SearchGuildInfo] GetTitleData error: " + titleDataResponse.Error.GenerateErrorReport());
                    return new BadRequestObjectResult("GetTitleData error");
                }
                if (titleDataResponse.Result.Data == null || !titleDataResponse.Result.Data.ContainsKey("GuildData"))
                {
                    _logger.LogWarning("[SearchGuildInfo] TitleData 'GuildData' not found.");
                    return new OkObjectResult(new GuildSearchResponse { GuildList = new List<GuildData>() });
                }
                string guildDataJson = titleDataResponse.Result.Data["GuildData"];
                _logger.LogInformation("[SearchGuildInfo] Retrieved TitleData: " + guildDataJson);

                // 5) TitleData JSON 파싱
                TitleGuildData? titleGuildData = null;
                try
                {
                    titleGuildData = JsonConvert.DeserializeObject<TitleGuildData>(guildDataJson);
                }
                catch (Exception ex)
                {
                    _logger.LogError("[SearchGuildInfo] JSON parsing error: " + ex.Message);
                    return new BadRequestObjectResult("JSON parsing error in TitleData");
                }
                if (titleGuildData == null || titleGuildData.Guilds == null)
                {
                    _logger.LogWarning("[SearchGuildInfo] No guilds in TitleData.");
                    return new OkObjectResult(new GuildSearchResponse { GuildList = new List<GuildData>() });
                }

                // 6) 검색어 기준 길드 목록 필터링 (대소문자 구분 없이 부분 일치)
                var matchedGuilds = titleGuildData.Guilds
                    .Where(g => !string.IsNullOrEmpty(g.GuildName) &&
                                g.GuildName.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
                _logger.LogInformation($"[SearchGuildInfo] Found {matchedGuilds.Count} guild(s) matching query '{searchQuery}'.");

                // 7) 그룹 멤버 정보를 조회하기 위해 서버 토큰 획득 (DeveloperSecretKey 기반)
                var tokenRequest = new GetEntityTokenRequest();
                var tokenResult = await PlayFabAuthenticationAPI.GetEntityTokenAsync(tokenRequest);
                if (tokenResult.Error != null)
                {
                    _logger.LogError("[SearchGuildInfo] Error fetching server entity token: " + tokenResult.Error.GenerateErrorReport());
                    return new BadRequestObjectResult("Error fetching server entity token");
                }
                var serverAuthContext = new PlayFabAuthenticationContext
                {
                    EntityId = tokenResult.Result.Entity.Id,
                    EntityType = tokenResult.Result.Entity.Type,
                    EntityToken = tokenResult.Result.EntityToken
                };

                // 8) 매칭된 각 길드에 대해 그룹 멤버 정보(인원 수) 조회
                List<GuildData> resultGuilds = new List<GuildData>();
                foreach (var guild in matchedGuilds)
                {
                    int memberCount = 0;
                    try
                    {
                        var listMembersReq = new ListGroupMembersRequest
                        {
                            AuthenticationContext = serverAuthContext, // 클라이언트 토큰 대신 서버 토큰 사용
                            Group = new PlayFab.GroupsModels.EntityKey { Id = guild.GuildId }
                        };
                        var listMembersRes = await PlayFabGroupsAPI.ListGroupMembersAsync(listMembersReq);
                        if (listMembersRes.Error == null && listMembersRes.Result?.Members != null)
                        {
                            foreach (var roleBlock in listMembersRes.Result.Members)
                            {
                                if (roleBlock.Members != null)
                                    memberCount += roleBlock.Members.Count;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[SearchGuildInfo] Error fetching group members for guild {guild.GuildId}: {ex.Message}");
                    }
                    // 임시로 길드 레벨은 0으로 처리
                    resultGuilds.Add(new GuildData
                    {
                        GuildName = guild.GuildName,
                        GuildId = guild.GuildId,
                        GuildLevel = 0,
                        GuildHeadCount = memberCount
                    });
                }

                // 9) 최종 응답 DTO 구성
                var response = new GuildSearchResponse { GuildList = resultGuilds };

                string debugJson = JsonConvert.SerializeObject(response);
                _logger.LogInformation("[SearchGuildInfo] Final response JSON: " + debugJson);

                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError("[SearchGuildInfo] Exception: " + ex.Message + "\n" + ex.StackTrace);
                return new BadRequestObjectResult("Internal server error in SearchGuildInfo.");
            }
        }
    }

    // 요청 DTO (클라이언트에서 { "SearchQuery": "...", "EntityToken": { ... } } 형태로 전송)
    public class GuildSearchRequest
    {
        public string? SearchQuery { get; set; }
        public EntityTokenData? EntityToken { get; set; }
    }


    // 응답 DTO: 필터링된 길드 목록 반환
    public class GuildSearchResponse
    {
        public List<GuildData>? GuildList { get; set; }
    }

    // 최종 반환할 길드 데이터 DTO (클라이언트와 동일)
    public class GuildData
    {
        public string? GuildName { get; set; }
        public int GuildLevel { get; set; }
        public int GuildHeadCount { get; set; }
        public string? GuildId { get; set; }
    }

    // TitleData에 저장된 JSON 구조에 맞춘 DTO
    public class TitleGuildData
    {
        [JsonProperty("guilds")]
        public List<GuildTitleData>? Guilds { get; set; }
    }

    public class GuildTitleData
    {
        [JsonProperty("guildId")]
        public string? GuildId { get; set; }
        [JsonProperty("guildName")]
        public string? GuildName { get; set; }
    }
}
