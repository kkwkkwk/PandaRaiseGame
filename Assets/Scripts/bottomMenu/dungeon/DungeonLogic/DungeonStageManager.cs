using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 던전 씬 전환 및 관리 매니저
/// </summary>
public class DungeonStageManager : MonoBehaviour
{
    /// <summary>
    /// 싱글톤 인스턴스
    /// </summary>
    public static DungeonStageManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnBossCleared()
    {
        int nextFloor = DungeonManager.Instance.MaxClearedFloor + 1;
        StartCoroutine(DungeonManager.Instance.UpdateClearedFloorCoroutine(nextFloor));
        // 그 뒤에 실제 스테이지 이동이나 보상 지급 처리...
    }

    /// <summary>
    /// 지정된 던전 층으로 씬 전환을 수행합니다.
    /// </summary>
    /// <param name="floor">이동할 던전 층 번호</param>
    public void LoadDungeonStage(int floor)
    {
        Debug.Log($"[DungeonStageManager] Loading dungeon floor {floor}");
        // TODO: 실제 던전 씬 로드 로직을 여기에 구현하세요.
        // 예: UnityEngine.SceneManagement.SceneManager.LoadScene($"DungeonFloor{floor}");
    }
}
