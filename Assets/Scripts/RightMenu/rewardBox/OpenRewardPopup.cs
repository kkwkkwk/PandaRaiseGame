using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenRewardPopup : MonoBehaviour
{
    public void OnClickOpenRewardPanel() // 리워드 버튼 클릭
    {
        RewardPopupManager.Instance.OpenRewardPopup();
    }
}
