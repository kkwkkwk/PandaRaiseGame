using System;

[Serializable]
public class GuildMissionData
{
    public string missionContent;

    public string guildMissionID;       // 길드 미션 ID
    public string guildMissionReward;   // 길드 미션 보상정보
    public bool isCleared;              // 클리어 여부
}