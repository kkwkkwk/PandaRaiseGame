using UnityEngine;
using TMPro;

public class GoldUIController : MonoBehaviour
{
    [Tooltip("골드를 표시할 TextMeshProUGUI 객체")]
    public TextMeshProUGUI goldText;

    private void Update()
    {
        // 골드 값을 StageManager 등에서 받아와서 표시
        if (StageManager.Instance != null)
        {
            int currentGold = StageManager.Instance.gold;
            goldText.text = FormatGold(currentGold);
        }
    }

    // (선택) 큰 수를 줄여서 표기 (예: 3400 => 3.40K, 3400000000 => 3.40B)
    private string FormatGold(long goldValue)
    {
        //  나중에 K, M, B, T 등 다양하게 처리 하려면 또 추가하면 됨
        if (goldValue >= 1000000000)
            return (goldValue / 1000000000f).ToString("F2") + "B";
        else if (goldValue >= 1000000)
            return (goldValue / 1000000f).ToString("F2") + "M";
        else if (goldValue >= 1000)
            return (goldValue / 1000f).ToString("F2") + "K";
        else
            return goldValue.ToString();
    }
}
