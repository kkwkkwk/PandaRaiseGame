using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public class AddCurrency : MonoBehaviour
{
    // 게임머니(골드)를 플레이어에게 추가하는 함수
    public void AddCurrencyToPlayer(string currencyCode, int amount)
    {
        // PlayFab에 게임 머니(골드) 추가 요청 생성
        var request = new AddUserVirtualCurrencyRequest
        {
            VirtualCurrency = currencyCode, // PlayFab에서 설정한 화폐 코드 (예: "GC" = 골드 코인)
            Amount = amount // 추가할 금액
        };

        // PlayFab API를 사용하여 게임 머니(골드) 추가 요청 전송
        PlayFabClientAPI.AddUserVirtualCurrency(request, OnCurrencyAddedSuccess, OnCurrencyAddedFailure);
    }

    // 게임 머니(골드) 추가 성공 시 호출되는 함수
    private void OnCurrencyAddedSuccess(ModifyUserVirtualCurrencyResult result)
    {
        Debug.Log($"게임 머니(골드) 추가 성공! 현재 {result.VirtualCurrency} 잔액: {result.Balance}");
    }

    // 게임 머니(골드) 추가 실패 시 호출되는 함수
    private void OnCurrencyAddedFailure(PlayFabError error)
    {
        Debug.LogError($"게임 머니(골드) 추가 실패! 에러: {error.GenerateErrorReport()}");
    }
}


/*
 
이런식으로 사용하면 됨

public class InAppPurchaseManager : MonoBehaviour
{
    private PlayFabCurrencyManager currencyManager;

    void Start()
    {
        currencyManager = FindObjectOfType<PlayFabCurrencyManager>();
    }

    public void OnPurchaseSuccess()
    {
        // 인앱 결제 성공 시 1000 골드 지급
        currencyManager.AddCurrencyToPlayer("GC", 1000);
    }
}

 
 */