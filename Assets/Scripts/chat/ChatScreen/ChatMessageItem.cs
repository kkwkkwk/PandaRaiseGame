using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 채팅 프리팹(메시지 아이템)용 전용 스크립트
/// </summary>
public class ChatMessageItem : MonoBehaviour
{
    [Header("TextMeshPro References")]
    [Tooltip("채팅 보낸 사람 이름 표시용 TMP 텍스트 (프리팹 내 '이름 패널')")]
    public TextMeshProUGUI nameText;

    [Tooltip("채팅 메시지 내용 표시용 TMP 텍스트 (프리팹 내 '채팅 패널')")]
    public TextMeshProUGUI contentText;

    /// <summary>
    /// 채팅 내용 설정
    /// </summary>
    /// <param name="senderName">보낸 사람 이름</param>
    /// <param name="message">메시지 내용</param>
    public void SetMessage(string senderName, string message)
    {
        if (nameText != null)
        {
            nameText.text = senderName;
        }
        else
        {
            Debug.LogWarning("[ChatMessageItem] nameText 참조가 없음.");
        }

        if (contentText != null)
        {
            contentText.text = message;
        }
        else
        {
            Debug.LogWarning("[ChatMessageItem] contentText 참조가 없음.");
        }
    }
}
