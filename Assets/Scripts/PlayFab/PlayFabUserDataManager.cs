using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class PlayFabUserDataManager : MonoBehaviour
{

    private string defaultCurrency = "GC"; // 기본 지급할 가상 화폐 코드
    private int defaultCurrencyAmount = 1000; // 기본 지급할 금액
    private int defaultLevel = 1; // 초기 레벨
    private int defaultExp = 0; // 초기 경험치

    void Start()
    {
        // LoginPlayFabUser에서 로그인 성공 시 이벤트를 구독
        LoginPlayFabUser.OnPlayFabLoginSuccessEvent += CheckUserData;
    }

    /// <summary>
    /// 사용자의 데이터가 존재하는지 확인하는 함수
    /// (데이터가 없으면 신규 유저로 등록)
    /// </summary>
    private void CheckUserData(string playFabId)
    {
        Debug.Log("🛠 사용자 데이터 확인 중...");

        var request = new GetUserDataRequest
        {
            PlayFabId = playFabId
        };

        PlayFabClientAPI.GetUserData(request, result =>
        {
            // 사용자의 데이터가 없는 경우 (신규 유저)
            if (result.Data == null || result.Data.Count == 0)
            {
                Debug.Log("사용자 데이터 없음 → 신규 유저 등록");
                RegisterNewUserData();// 신규 유저 초기 데이터 설정
            }
            else
            {
                Debug.Log("기존 사용자 데이터 확인됨.");
            }
        }, error =>
        {
            Debug.LogError("사용자 데이터 확인 실패: " + error.GenerateErrorReport());
        });
    }

    /// <summary>
    /// 신규 사용자 등록 (초기 레벨 및 재화 설정)
    /// </summary>
    private void RegisterNewUserData()
    {
        var request = new UpdateUserDataRequest
        {
            
            Data = new Dictionary<string, string>
            {
                { "level", defaultLevel.ToString() },   // 기본 레벨 1 설정
                { "exp", defaultExp.ToString() }        // 기본 경험치 0 설정
            }
        };

        // PlayFab 서버에 사용자 데이터 저장 요청
        PlayFabClientAPI.UpdateUserData(request, result => 
        {
            Debug.Log("신규 사용자 데이터 등록 완료!");
            GiveDefaultCurrency();                      // 기본 재화 지급
        }, error =>
        {
            Debug.LogError("사용자 데이터 등록 실패: " + error.GenerateErrorReport());
        });
    }

    /// <summary>
    /// 기본 게임 머니 지급
    /// </summary>
    private void GiveDefaultCurrency()
    {
        var request = new AddUserVirtualCurrencyRequest
        {
            VirtualCurrency = defaultCurrency,  // PlayFab에서 설정한 화폐 코드 사용
            Amount = defaultCurrencyAmount      // 기본 지급할 재화량
        };

        PlayFabClientAPI.AddUserVirtualCurrency(request, result => // PlayFab API를 사용하여 가상 화폐 지급 요청
        {
            Debug.Log($"초기 재화 지급 완료! 지급 금액: {defaultCurrencyAmount} {defaultCurrency}");
        }, error =>
        {
            Debug.LogError("초기 재화 지급 실패: " + error.GenerateErrorReport());
        });
    }

    void OnDestroy()
    {
        // 이벤트 해제 (메모리 누수 방지)
        LoginPlayFabUser.OnPlayFabLoginSuccessEvent -= CheckUserData;
    }
}
