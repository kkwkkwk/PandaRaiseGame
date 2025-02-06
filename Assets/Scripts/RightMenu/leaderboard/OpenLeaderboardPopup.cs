using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenLeaderboardPopup : MonoBehaviour
{
    public void OnClickOpenLeaderboardPanel() // 설정 버튼 클릭
    {
        LeaderboardPopupManager.Instance.OpenLeaderboardPopup();
    }
}
