using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DungeonStageData", menuName = "Dungeon/Dungeon Stage Data")]
public class DungeonStageData : ScriptableObject
{
    [Header("층 번호 (1~10)")]
    [Tooltip("이 데이터가 대응하는 던전 층 번호")]
    public int floorNumber;

    [Header("플레이어 스폰 위치")]
    [Tooltip("던전 시작 시 플레이어를 소환할 위치들 (로컬 좌표 배열)")]
    public Vector3[] playerSpawnPositions;

    [Header("보스 설정")]
    [Tooltip("이 층에서 소환할 보스 프리팹")]
    public GameObject bossPrefab;
    [Tooltip("보스를 소환할 위치들 (로컬 좌표 배열)")]
    public Vector3[] bossSpawnPositions;

    [Header("성공 보상")]
    [Tooltip("해당 층 보스를 처치했을 때 지급할 보상 목록")]
    public List<RewardEntry> successRewards = new List<RewardEntry>();

    [Header("실패 최소 보상")]
    [Tooltip("플레이어가 이 층을 깰 수 없었을 때(한 층도 못 깼을 때 포함) 지급할 최소 보상 목록(층마다. 던전리워드매니져쪽에 있는 건 기본. 추후에 층마다 기본 지급 보상 달라질까봐 일단 필드만 구현 해놨음)")]
    public List<RewardEntry> failureRewards = new List<RewardEntry>();

    [Serializable]
    public class RewardEntry
    {
        [Tooltip("보상 타입 (예: \"GOLD\", \"DM\", \"EXP\")")]
        public string rewardType;
        [Tooltip("지급량")]
        public int amount;
    }
}
