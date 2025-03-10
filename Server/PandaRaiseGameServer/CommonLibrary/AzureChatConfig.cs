using System;

namespace CommonLibrary
{
    public static class AzureChatConfig
    {
        private const string EndpointKey = "WebPubSubEndpoint";
        private const string AccessKeyKey = "WebPubSubAccessKey";

        public static string GetEndpoint()
        {
            string endpoint = Environment.GetEnvironmentVariable(EndpointKey)!;
            if (string.IsNullOrEmpty(endpoint))
            {
                throw new InvalidOperationException(
                    $"Web PubSub 엔드포인트 환경 변수 '{EndpointKey}'가 설정되지 않았습니다."
                );
            }
            return endpoint;
        }

        public static string GetAccessKey()
        {
            string accessKey = Environment.GetEnvironmentVariable(AccessKeyKey)!;
            if (string.IsNullOrEmpty(accessKey))
            {
                throw new InvalidOperationException(
                    $"Web PubSub 액세스 키 환경 변수 '{AccessKeyKey}'가 설정되지 않았습니다."
                );
            }
            return accessKey;
        }
    }
}
