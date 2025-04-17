using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SmallItemController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI itemNameText;    // 아이템명
    public Image itemImage;                 // 아이템 이미지
    public Image currencyIconImage;         // 결제 수단 아이콘
    public TextMeshProUGUI priceText;       // 가격

    [Header("Sprites (Currency Icons)")]
    public Sprite diamondIcon;
    public Sprite goldIcon;
    public Sprite mileageIcon;
    public Sprite freeIcon;
    public Sprite wonIcon;                  // 현금(Won) 아이콘

    [Header("Icon Layout Settings")]
    public Vector2 iconSize = new Vector2(32, 32);
    public Vector2 iconOffset = Vector2.zero;
    public bool preserveIconAspect = true;

    /// <summary>
    /// 프리팹 데이터 세팅
    /// </summary>
    /// <param name="name">아이템명</param>
    /// <param name="sprite">아이템 이미지</param>
    /// <param name="price">가격</param>
    /// <param name="currencyType">결제수단 ("DIAMOND","GC","MILEAGE","FREE","WON")</param>
    public void Setup(string name, Sprite sprite, int price, string currencyType)
    {
        // 1) 아이템명
        if (itemNameText != null)
            itemNameText.text = name;

        // 2) 아이템 이미지
        if (itemImage != null)
            itemImage.sprite = sprite;

        // 3) 가격 표시
        if (priceText != null)
        {
            if (currencyType == "FREE")
            {
                // 무료는 광고보기로 표시
                priceText.text = "광고보기";
            }
            else if (currencyType == "WON")
            {
                // 현금은 '\ ' 기호 붙이기
                priceText.text = "\\ " + price.ToString();
            }
            else
            {
                // 그 외는 숫자만 표시
                priceText.text = price.ToString();
            }
        }

        // 4) 결제수단 아이콘
        if (currencyIconImage != null)
        {
            Sprite icon = null;
            switch (currencyType)
            {
                case "DIAMOND": icon = diamondIcon; break;
                case "GC": icon = goldIcon; break;
                case "MILEAGE": icon = mileageIcon; break;
                case "FREE": icon = freeIcon; break;
                case "WON": icon = wonIcon; break;
            }

            if (icon != null)
            {
                currencyIconImage.enabled = true;
                currencyIconImage.sprite = icon;
                currencyIconImage.preserveAspect = preserveIconAspect;

                var rt = currencyIconImage.rectTransform;
                rt.sizeDelta = iconSize;
                rt.anchoredPosition = iconOffset;
            }
            else
            {
                currencyIconImage.enabled = false;
            }
        }
    }
}