using System;
using PlayFab;

namespace CommonLibrary
{
    /// <summary>
    /// Azure Portal의 Application Settings에 등록된
    /// 환경 변수를 통해 PlayFab 설정을 주입받는 클래스
    /// </summary>
    public static class PlayFabConfig
    {
        // 환경 변수 키 (Azure Portal에서 설정)
        private const string TitleIdKey = "PlayFabTitleId";
        private const string SecretKeyKey = "PlayFabDeveloperSecretKey";

        /// <summary>
        /// Azure Functions의 환경 변수에서 TitleId, SecretKey를 읽고
        /// PlayFabSettings에 적용합니다.
        /// </summary>
        public static void Configure()
        {
            string titleId = Environment.GetEnvironmentVariable(TitleIdKey)!;
            string secretKey = Environment.GetEnvironmentVariable(SecretKeyKey)!;

            if (string.IsNullOrEmpty(titleId) || string.IsNullOrEmpty(secretKey))
            {
                // 둘 중 하나라도 누락된 경우 오류 처리
                throw new InvalidOperationException(
                    $"PlayFab 설정이 누락되었습니다. " +
                    $"환경 변수 '{TitleIdKey}'와 '{SecretKeyKey}'를 확인하세요."
                );
            }

            // PlayFab SDK 설정 반영
            PlayFabSettings.staticSettings.TitleId = titleId;
            PlayFabSettings.staticSettings.DeveloperSecretKey = secretKey;
        }
    }
}
