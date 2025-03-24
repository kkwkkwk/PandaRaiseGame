using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels; // 서버 인증 관련
using PlayFab.GroupsModels; // 그룹 생성, 업데이트 관련
using PlayFab.AuthenticationModels; // GetEntityTokenRequest 등
using CommonLibrary;

namespace Guild
{
    public class CreateGuild
    {
        private readonly ILogger<CreateGuild> _logger;

        public CreateGuild(ILogger<CreateGuild> logger)
        {
            _logger = logger;
        }

        [Function("CreateGuild")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("[CreateGuild] Function invoked.");

                // 1) 요청 바디 파싱
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation($"[CreateGuild] RequestBody: {requestBody}");

                var input = JsonConvert.DeserializeObject<CreateGuildRequest>(requestBody);
                if (input == null || string.IsNullOrEmpty(input.playFabId) || string.IsNullOrEmpty(input.guildName))
                {
                    _logger.LogWarning("[CreateGuild] playFabId or guildName is missing in request.");
                    return new BadRequestObjectResult("Invalid request: playFabId and guildName are required.");
                }

                // 2) PlayFab 설정 (SecretKey, TitleId 등)
                PlayFabConfig.Configure();
                _logger.LogInformation("[CreateGuild] PlayFab configured.");

                // 3) 서버 인증(DeveloperSecretKey) 기반 토큰 획득
                //    Groups API 호출 시, 더 높은 권한이 필요
                var tokenRequest = new GetEntityTokenRequest();
                var tokenResult = await PlayFabAuthenticationAPI.GetEntityTokenAsync(tokenRequest);
                if (tokenResult.Error != null)
                {
                    _logger.LogError("[CreateGuild] Error fetching server entity token: " + tokenResult.Error.GenerateErrorReport());
                    return new BadRequestObjectResult("Error fetching server entity token");
                }
                // 서버 인증 Context
                var serverAuthContext = new PlayFabAuthenticationContext
                {
                    EntityId = tokenResult.Result.Entity.Id,
                    EntityType = tokenResult.Result.Entity.Type,
                    EntityToken = tokenResult.Result.EntityToken
                };

                // 4) 그룹 생성 요청
                //    그룹 이름은 guildName, 추가로 용량(멤버수)이나 닉네임 등 설정 가능
                var createGroupReq = new CreateGroupRequest
                {
                    AuthenticationContext = serverAuthContext,
                    GroupName = input.guildName
                };
                _logger.LogInformation($"[CreateGuild] Creating group: {input.guildName}");
                var createGroupRes = await PlayFabGroupsAPI.CreateGroupAsync(createGroupReq);
                if (createGroupRes.Error != null)
                {
                    _logger.LogError("[CreateGuild] CreateGroup error: " + createGroupRes.Error.GenerateErrorReport());
                    return new BadRequestObjectResult("Failed to create group: " + createGroupRes.Error.ErrorMessage);
                }
                var createdGroup = createGroupRes.Result;
                _logger.LogInformation($"[CreateGuild] New group created: GroupId={createdGroup.Group.Id}, GroupName={createdGroup.GroupName}");

                // 5) 이제 생성된 그룹의 Admin/Owner 역할 설정
                //    기본적으로 그룹 생성자가 Admin 역할을 갖도록 설정
                //    보통 CreateGroup 성공 시 "Admins" 역할이 자동 생성되는데,
                //    필요하다면 "Administrators" 라는 RoleId에 user를 추가할 수도 있음

                // (옵션) 만약 커스텀 역할을 만들고 싶다면 UpdateGroupRoleRequest로 처리 가능
                // 여기서는 이미 존재하는 "admins" 역할에 user를 추가한다고 가정(PlayFab Groups에서 자동 생성됨)

                // user(= playFabId)의 entity key 만들기
                // user -> entity key 변환 (title_player_account)
                // 보통 userId + "title_player_account" 로 entity를 구성
                var userEntityKey = new PlayFab.GroupsModels.EntityKey
                {
                    Id = await ConvertPlayFabIdToEntityId(input.playFabId),
                    Type = "title_player_account" // 보통은 이 값
                };
                if (string.IsNullOrEmpty(userEntityKey.Id))
                {
                    _logger.LogWarning("[CreateGuild] Failed to convert user PlayFabId to entityId. Adding to group might fail.");
                }

                // 6) AddMembers: 이 유저를 "admins" 역할로 추가
                var addAdminReq = new AddMembersRequest
                {
                    AuthenticationContext = serverAuthContext,
                    Group = createdGroup.Group,
                    RoleId = "admins",
                    Members = new List<PlayFab.GroupsModels.EntityKey> { userEntityKey }
                };
                var addAdminRes = await PlayFabGroupsAPI.AddMembersAsync(addAdminReq);
                if (addAdminRes.Error != null)
                {
                    _logger.LogWarning("[CreateGuild] AddMembers to admins role error: " + addAdminRes.Error.GenerateErrorReport());
                    // 실패해도 그룹 자체는 생성되었으므로, 계속 진행하거나 에러 반환할지 결정
                }
                else
                {
                    _logger.LogInformation("[CreateGuild] User added to admins role successfully.");
                }

                // 7) subadmins 역할 생성
                var createSubadminRoleReq = new CreateGroupRoleRequest
                {
                    AuthenticationContext = serverAuthContext,
                    Group = createdGroup.Group,
                    RoleId = "subadmins",
                    RoleName = "부마스터", // 역할 이름 (표시용)
                    CustomTags = new Dictionary<string, string>() // 필요 시 커스텀 태그 추가
                };
                var createSubadminRoleRes = await PlayFabGroupsAPI.CreateRoleAsync(createSubadminRoleReq);
                if (createSubadminRoleRes.Error != null)
                {
                    _logger.LogWarning("[CreateGuild] CreateGroupRole for subadmins failed: " + createSubadminRoleRes.Error.GenerateErrorReport());
                }
                else
                {
                    _logger.LogInformation("[CreateGuild] subadmins role created successfully.");
                }

                // 8) 업데이트: 기본 역할들의 RoleName 변경
                //    여기서 admins -> "길드마스터", members -> "길드원"
                //    UpdateGroupRole API를 사용하여 RoleName을 업데이트합니다.
                var updateAdminRoleReq = new UpdateGroupRoleRequest
                {
                    AuthenticationContext = serverAuthContext,
                    Group = createdGroup.Group,
                    RoleId = "admins",
                    RoleName = "길드마스터",
                    CustomTags = new Dictionary<string, string>()
                };
                var updateAdminRoleRes = await PlayFabGroupsAPI.UpdateRoleAsync(updateAdminRoleReq);
                if (updateAdminRoleRes.Error != null)
                {
                    _logger.LogWarning("[CreateGuild] UpdateGroupRole for admins failed: " + updateAdminRoleRes.Error.GenerateErrorReport());
                }
                else
                {
                    _logger.LogInformation("[CreateGuild] Admin role updated to '길드마스터'.");
                }

                var updateMemberRoleReq = new UpdateGroupRoleRequest
                {
                    AuthenticationContext = serverAuthContext,
                    Group = createdGroup.Group,
                    RoleId = "members",
                    RoleName = "길드원",
                    CustomTags = new Dictionary<string, string>()
                };
                var updateMemberRoleRes = await PlayFabGroupsAPI.UpdateRoleAsync(updateMemberRoleReq);
                if (updateMemberRoleRes.Error != null)
                {
                    _logger.LogWarning("[CreateGuild] UpdateGroupRole for members failed: " + updateMemberRoleRes.Error.GenerateErrorReport());
                }
                else
                {
                    _logger.LogInformation("[CreateGuild] Members role updated to '길드원'.");
                }

                // 9) 최종 응답
                var resp = new CreateGuildResponse
                {
                    guildId = createdGroup.Group.Id,
                    guildName = createdGroup.GroupName,
                    message = "Guild created successfully."
                };

                string debugJson = JsonConvert.SerializeObject(resp);
                _logger.LogInformation("[CreateGuild] Final response: " + debugJson);

                return new OkObjectResult(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError("[CreateGuild] Exception: " + ex.Message + "\n" + ex.StackTrace);
                return new BadRequestObjectResult("Internal server error in CreateGuild.");
            }
        }

        /// <summary>
        /// playFabId -> entityId 변환 (title_player_account)
        /// </summary>
        private async Task<string?> ConvertPlayFabIdToEntityId(string playFabId)
        {
            try
            {
                // GetUserAccountInfo (Server API)
                var accountInfoReq = new GetUserAccountInfoRequest
                {
                    PlayFabId = playFabId
                };
                var accountInfoRes = await PlayFabServerAPI.GetUserAccountInfoAsync(accountInfoReq);
                if (accountInfoRes.Error != null)
                {
                    _logger.LogWarning($"[ConvertPlayFabIdToEntityId] error: {accountInfoRes.Error.GenerateErrorReport()}");
                    return null;
                }

                // TitlePlayerAccount -> entityId
                var tpa = accountInfoRes.Result.UserInfo?.TitleInfo?.TitlePlayerAccount;
                if (tpa != null)
                {
                    return tpa.Id; // entityId
                }
                else
                {
                    _logger.LogWarning("[ConvertPlayFabIdToEntityId] title_player_account not found in user info");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("[ConvertPlayFabIdToEntityId] Exception: " + ex.Message);
                return null;
            }
        }
    }

    // -----------------------------
    // DTO
    // -----------------------------
    public class CreateGuildRequest
    {
        public string? playFabId { get; set; }
        public string? guildName { get; set; }
    }

    public class CreateGuildResponse
    {
        public string? guildId;
        public string? guildName;
        public string? message;
    }
}
