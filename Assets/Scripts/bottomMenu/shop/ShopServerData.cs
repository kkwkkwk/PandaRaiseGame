using System;

[System.Serializable]
public class ShopServerData
{
    public int diamond;     // 사용자 보유 다이아
    public int gold;        // 사용자 보유 골드
    public int mileage;     // 사용자 보유 마일지리
    public string[] products; // 팔고 있는 상품 목록 (이름들)

    // 필요하다면 더 추가
    public string lastLogin;
    public bool isEventOpen;
}
