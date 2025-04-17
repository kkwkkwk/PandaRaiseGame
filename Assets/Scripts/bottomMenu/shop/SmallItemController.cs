using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SmallItemController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI itemNameText;          // 아이템명
    public Image itemImage;                       // 아이템 이미지
    public Image currencyIconImage;               // 결제 수단 아이콘
    public TextMeshProUGUI priceText;             // 일반 결제용 텍스트
    public TextMeshProUGUI wonPriceText;          // 현금 결제용 텍스트 (₩)

    [Header("Sprites (Currency Icons)")]
    public Sprite diamondIcon;
    public Sprite goldIcon;
    public Sprite mileageIcon;
    public Sprite freeIcon;

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
        {
            itemImage.sprite = sprite;
            Debug.Log($"[SmallItemController] ItemImage.sprite set to {(sprite != null ? sprite.name : "null")}");
        }

        // 3) 가격 텍스트 처리
        if (priceText != null && wonPriceText != null)
        {
            if (currencyType == "WON")
            {
                priceText.gameObject.SetActive(false);
                wonPriceText.gameObject.SetActive(true);
                wonPriceText.text = "￦ " + price;
            }
            else
            {
                wonPriceText.gameObject.SetActive(false);
                priceText.gameObject.SetActive(true);

                if (currencyType == "FREE")
                    priceText.text = "광고보기";
                else
                    priceText.text = price.ToString();
            }
        }

        // 4) 결제수단 아이콘 설정
        if (currencyIconImage != null)
        {
            if (currencyType == "WON")
            {
                currencyIconImage.enabled = false;
                Debug.Log("[SmallItemController] WON 결제 방식이므로 아이콘 비활성화");
            }
            else
            {
                Sprite icon = null;
                switch (currencyType)
                {
                    case "DIAMOND": icon = diamondIcon; break;
                    case "GC": icon = goldIcon; break;
                    case "MILEAGE": icon = mileageIcon; break;
                    case "FREE": icon = freeIcon; break;
                }

                if (icon != null)
                {
                    currencyIconImage.enabled = true;
                    currencyIconImage.sprite = icon;

                    Debug.Log($"[SmallItemController] CurrencyIcon set: {currencyType} → {icon.name}");
                }
                else
                {
                    currencyIconImage.enabled = false;
                    Debug.LogWarning($"[SmallItemController] No icon found for currencyType '{currencyType}'");
                }
            }
        }
    }
}
