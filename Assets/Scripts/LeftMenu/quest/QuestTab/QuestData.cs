using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하나의 퀘스트 정보(혹은 문자열 정보)를 담는 클래스.
/// </summary>
public class QuestData
{
    // 퀘스트(또는 항목) 내용(문자열)만 보관.
    public string questInfo;

    /// <summary>
    /// 생성자에서 내용을 받는다.
    /// </summary>
    /// <param name="info">퀘스트(또는 항목)의 실제 텍스트</param>
    public QuestData(string info)
    {
        questInfo = info;
    }
}
