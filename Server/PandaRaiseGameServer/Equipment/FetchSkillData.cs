using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using CommonLibrary; // 예: PlayFabConfig.Configure()가 들어 있는 라이브러리

namespace Equipment
{
    public class FetchSkillData
    {
        private readonly ILogger<FetchSkillData> _logger;

        public FetchSkillData(ILogger<FetchSkillData> logger)
        {
            _logger = logger;
        }

        [Function("FetchSkillData")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("FetchSkillData 함수가 호출되었습니다.");

            // 1) HTTP 요청 바디에서 JSON 파싱 (예: { "playFabId": "xxx" })
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation($"요청 바디: {requestBody}");

            dynamic data = JsonConvert.DeserializeObject(requestBody)!;
            string playFabId = data?.playFabId!;

            if (string.IsNullOrEmpty(playFabId))
            {
                _logger.LogWarning("요청 본문에 playFabId가 포함되어 있지 않습니다.");
                return new BadRequestObjectResult("PlayFabId가 누락되었습니다.");
            }

            // 2) PlayFab 설정 (TitleId, SecretKey 등)
            PlayFabConfig.Configure();

            // -------------------------------------------------------------------
            // 3) 먼저 "Skill" 카탈로그 아이템들을 불러와 (GetCatalogItems)
            //    각 ItemId => (level, rank 등) 정보를 딕셔너리에 저장
            // -------------------------------------------------------------------
            _logger.LogInformation("Skill 카탈로그 아이템 조회 중...");
            var getCatalogReq = new GetCatalogItemsRequest
            {
                CatalogVersion = "Skill"
            };

            var getCatalogRes = await PlayFabServerAPI.GetCatalogItemsAsync(getCatalogReq);
            if (getCatalogRes.Error != null)
            {
                _logger.LogError("GetCatalogItems 실패: " + getCatalogRes.Error.GenerateErrorReport());
                return new BadRequestObjectResult("GetCatalogItems 오류: " + getCatalogRes.Error.GenerateErrorReport());
            }

            // CatalogItem 목록
            var catalogItems = getCatalogRes.Result.Catalog;
            _logger.LogInformation($"Skill 카탈로그 아이템 개수: {catalogItems.Count}");

            // itemId -> (level, rank, etc.) 매핑 테이블
            // CatalogItem.CustomData는 string 이므로, JSON deserialize 과정을 거침
            var catalogDataDict = new Dictionary<string, (int level, string rank, string displayName)>();

            foreach (var catItem in catalogItems)
            {
                // 예: catItem.ItemId = "Sword_01"
                //     catItem.CustomData = "{\"level\":10,\"rank\":\"Rare\"}" (JSON 형태의 문자열)
                int catLevel = 0;
                string catRank = "Common";

                // catalog DisplayName (게임 내 표시용 이름)
                string catDisplayName = catItem.DisplayName ?? catItem.ItemId;

                if (!string.IsNullOrEmpty(catItem.CustomData))
                {
                    try
                    {
                        var catCustomData = JsonConvert.DeserializeObject<Dictionary<string, object>>(catItem.CustomData);

                        if (catCustomData != null)
                        {
                            // level, rank 키가 있으면 파싱
                            if (catCustomData.ContainsKey("level"))
                            {
                                // level 값이 보통 숫자이므로 int로 parse
                                catLevel = Convert.ToInt32(catCustomData["level"]);
                            }
                            if (catCustomData.ContainsKey("rank"))
                            {
#pragma warning disable CS8600
                                catRank = catCustomData["rank"].ToString();
#pragma warning restore CS8600
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"카탈로그 아이템 {catItem.ItemId} CustomData 파싱 오류: {ex.Message}");
                    }
                }

                // 딕셔너리에 저장
#pragma warning disable CS8619
                catalogDataDict[catItem.ItemId] = (catLevel, catRank, catDisplayName);
#pragma warning restore CS8619
            }

            // -------------------------------------------------------------------
            // 4) 유저 인벤토리 조회(GetUserInventory)
            //    -> Item Instance CustomData에서 enhancement 추출
            // -------------------------------------------------------------------
            _logger.LogInformation($"플레이어 {playFabId} 인벤토리 조회 중...");

            var getInvRequest = new GetUserInventoryRequest
            {
                PlayFabId = playFabId
            };
            var getInvResult = await PlayFabServerAPI.GetUserInventoryAsync(getInvRequest);
            if (getInvResult.Error != null)
            {
                _logger.LogError("GetUserInventory 실패: " + getInvResult.Error.GenerateErrorReport());
                return new BadRequestObjectResult("GetUserInventory 오류: " + getInvResult.Error.GenerateErrorReport());
            }

            // 전체 아이템 중, CatalogVersion == "Skill" 인 것만 필터링
            var allItems = getInvResult.Result.Inventory;
            var skillItems = allItems.Where(item => item.CatalogVersion == "Skill").ToList();

            // -------------------------------------------------------------------
            // 5) 최종 SkillData 리스트 구성
            //    - 레벨, 랭크는 카탈로그에서( catalogDataDict )
            //    - 강화 수치(enhancement)는 Item Instance CustomData에서
            // -------------------------------------------------------------------
            var skillDataList = new List<SkillData>();

            foreach (var item in skillItems)
            {
                // ItemId에 해당하는 카탈로그 데이터 찾기
                if (!catalogDataDict.TryGetValue(item.ItemId, out var catInfo))
                {
                    // 해당 ItemId가 카탈로그에 없다면 스킵하거나 기본값 처리
                    _logger.LogWarning($"인벤토리 아이템 {item.ItemId}에 해당하는 카탈로그 정보를 찾을 수 없습니다.");
                    continue;
                }

                // 카탈로그에서 가져온 값
                int catLevel = catInfo.level;
                string catRank = catInfo.rank;
                string catDisplayName = catInfo.displayName;

                // enhancement는 인스턴스 CustomData에서 가져온다
                int enhancement = 0;
                if (item.CustomData != null && item.CustomData.ContainsKey("enhancement"))
                {
                    int.TryParse(item.CustomData["enhancement"], out enhancement);
                }

                // skillName은 "카탈로그 DisplayName"을 우선 사용 (or item.DisplayName, etc.)
                var skillName = catDisplayName;

                // SkillData 생성
                var skillData = new SkillData
                {
                    skillName = skillName,
                    level = catLevel,           // 카탈로그 정보
                    rank = catRank,             // 카탈로그 정보
                    enhancement = enhancement   // 사용자별 강화 수치 (Item Instance)
                };
                skillDataList.Add(skillData);
            }

            _logger.LogInformation($"최종 무기 개수: {skillDataList.Count}");

            // -------------------------------------------------------------------
            // 6) 결과 반환
            // -------------------------------------------------------------------
            // 프론트엔드(SkillPopupManager.cs)에서 List<SkillData>로 역직렬화 가능
            return new OkObjectResult(skillDataList);
        }
    }

    /// <summary>
    /// 프론트엔드(SkillPopupManager.cs)에서 쓰이는 것과 동일한 구조
    /// </summary>
    public class SkillData
    {
        public string? skillName { get; set; }   // 무기 이름 (카탈로그 DisplayName)
        public int level { get; set; }          // 카탈로그에 정의된 레벨
        public string? rank { get; set; }        // 카탈로그에 정의된 랭크 (Common/Rare/Epic 등)
        public int enhancement { get; set; }    // 유저별 강화 수치 (Item Instance CustomData)
    }
}
