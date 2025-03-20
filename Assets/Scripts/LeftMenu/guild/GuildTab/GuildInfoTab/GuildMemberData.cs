using System;

[Serializable]
public class GuildMemberData
{
    public string userName;     // 길드원 이름
    public string userClass;    // 길드원 계급
    public long userPower;       // 길드원 전투력
    public bool isOnline;       // 접속 여부
}
