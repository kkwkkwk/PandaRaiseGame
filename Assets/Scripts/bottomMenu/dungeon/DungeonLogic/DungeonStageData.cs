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

    
}
