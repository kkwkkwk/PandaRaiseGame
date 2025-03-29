using System;
using UnityEngine;


[Serializable]
public class GuildManagerData
{
    public string applicantId;     // 가입 신청한 유저의 고유 ID
    public string applicantName;   // 가입 신청한 유저 닉네임
    public int applicantPower;     // 가입 신청한 유저 전투력
}
