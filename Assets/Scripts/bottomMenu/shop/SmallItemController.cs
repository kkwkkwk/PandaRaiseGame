// SmallItemController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SmallItemController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI itemNameText;    // 아이템명
    public Image itemImage;       // 아이템 이미지
    public Image currencyIconImage; // 결제 수단 아이콘
    public TextMeshProUGUI priceText;        // 일반 결제용 텍스트
    public TextMeshProUGUI wonPriceText;     // ₩ 결제용 텍스트
    public Button purchaseButton;   // 구매 버튼

    [Header("Sprites (Currency Icons)")]
    public Sprite diamondIcon;
    public Sprite goldIcon;
    public Sprite mileageIcon;
    public Sprite freeIcon;

    /// <summary>
    /// UI 세팅만 수행 (버튼 리스너 제거)
    /// </summary>
    public void Setup(string name, Sprite sprite, int price, string currencyType)
    {
        SetupInternal(name, sprite, price, currencyType);
        if (purchaseButton != null)
            purchaseButton.onClick.RemoveAllListeners();
    }

    /// <summary>
    /// 구매 기능까지 포함한 세팅.
    /// itemId: 서버 식별용 ID (여기선 itemName 사용)
    /// itemType: "Weapon","Armor","Skill","Diamond","Free","Japha","Mileage"
    /// </summary>
    public void Setup(string name, Sprite sprite, int price, string currencyType, string itemId, string itemType)
    {
        SetupInternal(name, sprite, price, currencyType);

        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick.AddListener(() =>
            {
                switch (itemType)
                {
                    case "Weapon":
                        WeaponManager.Instance?.PurchaseWeapon(itemId, currencyType);
                        break;
                    case "Armor":
                        ArmorManager.Instance?.PurchaseArmor(itemId, currencyType);
                        break;
                    case "Skill":
                        SkillManager.Instance?.PurchaseSkill(itemId, currencyType);
                        break;
                    case "Diamond":
                        DiamondManager.Instance?.PurchaseDiamond(itemId, currencyType);
                        break;
                    case "Free":
                        FreeManager.Instance?.PurchaseFree(itemId, currencyType);
                        break;
                    case "Japha":
                        JaphaManager.Instance?.PurchaseJapha(itemId, currencyType);
                        break;
                    case "Mileage":
                        MileageManager.Instance?.PurchaseMileage(itemId, currencyType);
                        break;
                    default:
                        Debug.LogWarning($"[SmallItemController] 알 수 없는 itemType: {itemType}");
                        break;
                }
            });
        }
    }

    /// <summary>
    /// UI 텍스트 및 아이콘 설정 공통 로직
    /// </summary>
    private void SetupInternal(string name, Sprite sprite, int price, string currencyType)
    {
        // 1) 아이템명 표시
        if (itemNameText != null)
            itemNameText.text = name;

        // 2) 이미지 설정
        if (itemImage != null)
            itemImage.sprite = sprite;

        // 3) 가격 텍스트 처리
        if (currencyType == "WON")
        {
            if (priceText != null) priceText.gameObject.SetActive(false);
            if (wonPriceText != null)
            {
                wonPriceText.gameObject.SetActive(true);
                wonPriceText.text = "￦ " + price;
            }
        }
        else
        {
            if (wonPriceText != null) wonPriceText.gameObject.SetActive(false);
            if (priceText != null)
            {
                priceText.gameObject.SetActive(true);
                priceText.text = (currencyType == "FREE") ? "광고보기" : price.ToString();
            }
        }

        // 4) 결제 수단 아이콘 설정
        if (currencyIconImage != null)
        {
            if (currencyType == "WON")
            {
                currencyIconImage.enabled = false;
            }
            else
            {
                Sprite icon = currencyType switch
                {
                    "DIAMOND" => diamondIcon,
                    "GC" => goldIcon,
                    "MILEAGE" => mileageIcon,
                    "FREE" => freeIcon,
                    _ => null
                };

                currencyIconImage.enabled = (icon != null);
                if (icon != null)
                    currencyIconImage.sprite = icon;
            }
        }
    }
}
