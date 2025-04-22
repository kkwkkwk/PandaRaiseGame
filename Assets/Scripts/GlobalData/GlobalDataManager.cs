using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

/// <summary>
/// 게임 시작 시 한 번만 PlayFab에서 가상통화 잔액을 읽어와 GlobalData에 저장합니다.
/// </summary>
public class GlobalDataManager : MonoBehaviour
{
    public static GlobalDataManager Instance { get; private set; }

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

    /// <summary>
    /// PlayFab GetUserInventory 호출 후 결과를 GlobalData.VirtualCurrencyBalances에 저장합니다.
    /// </summary>
    public IEnumerator Initialize()
    {
        bool done = false;
        Debug.Log("[GlobalDataManager] Fetching user inventory…");

        PlayFabClientAPI.GetUserInventory(
            new GetUserInventoryRequest(),
            result =>
            {
                // PlayFab에서 온 가상화폐 잔액을 딕셔너리에 복사
                GlobalData.VirtualCurrencyBalances = new Dictionary<string, int>(result.VirtualCurrency);
                Debug.Log($"[GlobalDataManager] Loaded {GlobalData.VirtualCurrencyBalances.Count} currency entries");
                done = true;
            },
            error =>
            {
                Debug.LogError($"[GlobalDataManager] GetUserInventory failed: {error.GenerateErrorReport()}");
                // 실패해도 진행하되, 빈 딕셔너리로 남겨둡니다.
                GlobalData.VirtualCurrencyBalances = new Dictionary<string, int>();
                done = true;
            }
        );

        // 완료될 때까지 대기
        while (!done) yield return null;
    }
}
