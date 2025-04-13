public static class NumberFormatter
{
    /// <summary>
    /// 큰 숫자를 k, M, B, T, ... 등으로 축약해주는 함수
    /// 예: 1,234 -> 1.23k, 2,000,000 -> 2M
    /// </summary>
    /// <param name="value">축약할 숫자 (예: 골드)</param>
    /// <returns>"1.23k", "2M" 등 문자열</returns>
    public static string FormatBigNumber(long value)
    {
        // 0 이하일 경우 그냥 그대로 표시 (필요 시 "0"으로 처리)
        if (value < 1000)
            return value.ToString();

        double doubleValue = value;
        string[] suffixes = { "", "K", "M", "B", "AA", "AB", "AC", "AD" };
        // 필요하다면 더 많은 접미사를 추가할 수도 있음

        int index = 0;
        while (doubleValue >= 1000 && index < suffixes.Length - 1)
        {
            doubleValue /= 1000;
            index++;
        }

        // 소수점 2자리까지만 표기 ("0.##" 사용)
        return doubleValue.ToString("0.##") + suffixes[index];
    }
}
