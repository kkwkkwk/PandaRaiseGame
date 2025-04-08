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
using CommonLibrary;

namespace Skills
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

            // 1) 요청 JSON 파싱
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody)!;
            string playFabId = data?.playFabId!;

            if (string.IsNullOrEmpty(playFabId))
            {
                return new BadRequestObjectResult("PlayFabId가 누락되었습니다.");
            }

            // 2) PlayFab 설정
            PlayFabConfig.Configure();

            // 3) Skill 카탈로그 조회 -> rank, displayName
            var getCatalogReq = new GetCatalogItemsRequest
            {
                CatalogVersion = "Skill"
            };
            var getCatalogRes = await PlayFabServerAPI.GetCatalogItemsAsync(getCatalogReq);
            if (getCatalogRes.Error != null)
            {
                return new BadRequestObjectResult("GetCatalogItems(Skill) 오류: " + getCatalogRes.Error.GenerateErrorReport());
            }

            var catalogItems = getCatalogRes.Result.Catalog;
            var catalogDataDict = new Dictionary<string, (string rank, string displayName)>();

            foreach (var catItem in catalogItems)
            {
                string rank = "common";
                string catDisplayName = catItem.DisplayName ?? catItem.ItemId;

                if (!string.IsNullOrEmpty(catItem.CustomData))
                {
                    try
                    {
                        var catData = JsonConvert.DeserializeObject<Dictionary<string, object>>(catItem.CustomData);
                        if (catData != null && catData.ContainsKey("rank"))
                        {
                            // 1) "rank" 키가 있는지, 그 값이 null이 아닌지를 검사.
                            // 2) 값이 있다면 object?.ToString(), 없다면 "common" 사용
                            rank = catData.ContainsKey("rank")
                                ? catData["rank"]?.ToString() ?? "common"
                                : "common";
                        }
                    }
                    catch
                    {
                        // ignore
                    }
                }
                catalogDataDict[catItem.ItemId] = (rank, catDisplayName);
            }

            // 4) 유저 인벤토리에서 Skill 카탈로그 아이템만 조회
            var getInvReq = new GetUserInventoryRequest { PlayFabId = playFabId };
            var getInvRes = await PlayFabServerAPI.GetUserInventoryAsync(getInvReq);
            if (getInvRes.Error != null)
            {
                return new BadRequestObjectResult("GetUserInventory 오류: " + getInvRes.Error.GenerateErrorReport());
            }

            var allItems = getInvRes.Result.Inventory;
            var skillItems = allItems.Where(i => i.CatalogVersion == "Skill").ToList();

            // 5) SkillData 리스트
            var skillDataList = new List<SkillData>();
            foreach (var item in skillItems)
            {
                if (!catalogDataDict.TryGetValue(item.ItemId, out var catInfo))
                    continue;

                int level = 1;
                int enhancement = 0;

                if (item.CustomData != null)
                {
                    if (item.CustomData.ContainsKey("level"))
                    {
                        int.TryParse(item.CustomData["level"], out level);
                    }
                    if (item.CustomData.ContainsKey("enhancement"))
                    {
                        int.TryParse(item.CustomData["enhancement"], out enhancement);
                    }
                }

                var skillData = new SkillData
                {
                    itemInstanceId = item.ItemInstanceId,
                    skillName = catInfo.displayName,
                    rank = catInfo.rank,
                    level = level,
                    enhancement = enhancement
                };
                skillDataList.Add(skillData);
            }

            return new OkObjectResult(skillDataList);
        }
    }

    /// <summary>
    /// 스킬 아이템 DTO
    /// </summary>
    public class SkillData
    {
        public string? itemInstanceId { get; set; }
        public string? skillName { get; set; }
        public string? rank { get; set; }
        public int level { get; set; }
        public int enhancement { get; set; }
    }
}
