using System.Collections.Generic;

namespace Shop
{
    /// <summary>
    /// 가챠 등급별 가중치(확률) 테이블
    /// 합계는 자유롭게 설정 가능하나, 현재 예시는 10000 기준입니다.
    /// </summary>
    public static class GachaRankWeight
    {
        public static readonly Dictionary<string, int> RankWeights = new Dictionary<string, int>
        {
            ["common"] = 5000,  // 50.00%
            ["uncommon"] = 2500,  // 25.00%
            ["rare"] = 1500,  // 15.00%
            ["unique"] = 600,  //  6.00%
            ["epic"] = 300,  //  3.00%
            ["legendary"] = 100   //  1.00%
        };
    }
}