using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

public class GoogleConfigLoader : MonoBehaviour
{
    private static string webClientId;

    public static string GetWebClientId()
    {
        return webClientId;
    }

    public static IEnumerator LoadWebClientId()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "google_config.json");

        Debug.Log("📁 JSON 경로: " + path); // 🔍 디버깅용 로그

        // ✅ Android의 경우 `jar:file://`로 경로 수정 필요
        if (Application.platform == RuntimePlatform.Android)
        {
            path = "jar:file://assets/google_config.json" + path;
        }

        using (UnityWebRequest request = UnityWebRequest.Get(path))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("❌ JSON 파일을 불러오는데 실패했습니다: " + request.error);
                yield break;
            }

            string json = request.downloadHandler.text;
            GoogleConfig config = JsonUtility.FromJson<GoogleConfig>(json);

            if (config != null)
            {
                webClientId = config.webClientId;
                Debug.Log("✅ WebClientId 로드 성공: " + webClientId);
            }
            else
            {
                Debug.LogError("❌ JSON 파싱 실패");
            }
        }
    }
}

[System.Serializable]
public class GoogleConfig
{
    public string webClientId;
}
