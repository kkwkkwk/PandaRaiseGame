using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

public class IdleHeartbeatManager : MonoBehaviour
{
    // 하트비트 전송 간격 (초)
    [SerializeField]
    private float heartbeatInterval = 60f;
    // 3분(180초) 이상 활동이 없으면 하트비트 전송 안함
    [SerializeField]
    private float inactivityThreshold = 180f;

    private float lastActivityTime;
    private bool isHeartbeatRunning = false;

    void Start()
    {
        // 게임 시작 시 현재 시간을 마지막 활동 시간으로 설정
        lastActivityTime = Time.time;
        StartHeartbeat();
    }

    void Update()
    {
        // 키 입력, 터치, 마우스 클릭 등을 감지하여 마지막 활동 시간을 갱신
        if (Input.anyKeyDown || Input.touchCount > 0 || Input.GetMouseButtonDown(0))
        {
            lastActivityTime = Time.time;
            // 디버그: 활동이 감지되었음을 기록
            Debug.Log("[IdleHeartbeatManager] Activity detected. lastActivityTime updated to " + lastActivityTime);
        }
    }

    /// <summary>
    /// 하트비트 전송을 시작합니다.
    /// </summary>
    public void StartHeartbeat()
    {
        if (!isHeartbeatRunning)
        {
            isHeartbeatRunning = true;
            StartCoroutine(HeartbeatRoutine());
        }
    }

    /// <summary>
    /// 하트비트 전송을 중지합니다.
    /// </summary>
    public void StopHeartbeat()
    {
        isHeartbeatRunning = false;
        StopAllCoroutines();
    }

    private IEnumerator HeartbeatRoutine()
    {
        while (isHeartbeatRunning)
        {
            float inactivityTime = Time.time - lastActivityTime;
            if (inactivityTime < inactivityThreshold)
            {
                SendHeartbeat();
            }
            else
            {
                Debug.Log("[IdleHeartbeatManager] Inactivity for " + inactivityTime + " seconds, skipping heartbeat.");
            }
            yield return new WaitForSeconds(heartbeatInterval);
        }
    }

    /// <summary>
    /// 현재 시간을 PlayFab UserData에 업데이트하여 하트비트를 전송합니다.
    /// </summary>
    private void SendHeartbeat()
    {
        string currentTime = DateTime.UtcNow.ToString("o"); // ISO 8601 형식
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "lastHeartbeat", currentTime },
                { "isOnline", "true" }
            }
        };

        PlayFabClientAPI.UpdateUserData(request, result =>
        {
            Debug.Log("[IdleHeartbeatManager] Heartbeat sent successfully at " + currentTime);
        },
        error =>
        {
            Debug.LogError("[IdleHeartbeatManager] Failed to send heartbeat: " + error.GenerateErrorReport());
        });
    }

    void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            SendHeartbeat();
            UpdateOnlineStatus(false);
            StopHeartbeat();
        }
        else
        {
            UpdateOnlineStatus(true);
            StartHeartbeat();
        }
    }

    void OnApplicationQuit()
    {
        SendHeartbeat();
        UpdateOnlineStatus(false);
        StopHeartbeat();
    }

    /// <summary>
    /// 온라인 상태를 업데이트합니다.
    /// </summary>
    private void UpdateOnlineStatus(bool isOnline)
    {
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "isOnline", isOnline ? "true" : "false" }
            }
        };

        PlayFabClientAPI.UpdateUserData(request, result =>
        {
            Debug.Log("[IdleHeartbeatManager] Online status updated to " + isOnline);
        },
        error =>
        {
            Debug.LogError("[IdleHeartbeatManager] Failed to update online status: " + error.GenerateErrorReport());
        });
    }
}
