using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SmallItemController : MonoBehaviour
{
    /* ──────────────── UI 참조 ──────────────── */
    [Header("UI References")]
    public TextMeshProUGUI itemNameText;          // 아이템명
    public Image itemImage;                       // 아이템 이미지
    public Image currencyIconImage;               // 결제수단 아이콘
    public TextMeshProUGUI priceText;             // 일반 결제 텍스트
    public TextMeshProUGUI wonPriceText;          // 현금(₩) 텍스트
    public Button purchaseButton;                 // 구매 버튼

    /* ──────────────── 통화 아이콘 ──────────────── */
    [Header("Sprites (Currency Icons)")]
    public Sprite diamondIcon;
    public Sprite goldIcon;
    public Sprite mileageIcon;
    public Sprite freeIcon;

    /* ==============================================================  
       1) 공통 UI 세팅  
    ============================================================== */
    public void Setup(string name, Sprite sprite, int price, string currencyType)
    {
        SetupInternal(name, sprite, price, currencyType);
        purchaseButton?.onClick.RemoveAllListeners();
    }

    /* ==============================================================  
       2-a) 무기 가챠 전용 오버로드  
    ============================================================== */
    public void Setup(string name, Sprite sprite, int price, string currencyType,
                      WeaponItemData itemData, string itemType)
    {
        SetupInternal(name, sprite, price, currencyType);
        if (purchaseButton == null) return;

        purchaseButton.onClick.RemoveAllListeners();
        purchaseButton.onClick.AddListener(() =>
        {
            ShopWeaponManager.Instance?.PurchaseWeapon(itemData, currencyType);
        });
    }

    /* ==============================================================  
       2-b) 방어구 가챠 전용 오버로드  
    ============================================================== */
    public void Setup(string name, Sprite sprite, int price, string currencyType,
                      ArmorItemData itemData, string itemType)
    {
        SetupInternal(name, sprite, price, currencyType);
        if (purchaseButton == null) return;

        purchaseButton.onClick.RemoveAllListeners();
        purchaseButton.onClick.AddListener(() =>
        {
            ShopArmorManager.Instance?.PurchaseArmor(itemData, currencyType);
        });
    }

    /* ==============================================================  
       2-c) 스킬 가챠 전용 오버로드  
    ============================================================== */
    public void Setup(string name, Sprite sprite, int price, string currencyType,
                      SkillItemData itemData, string itemType)
    {
        SetupInternal(name, sprite, price, currencyType);
        if (purchaseButton == null) return;

        purchaseButton.onClick.RemoveAllListeners();
        purchaseButton.onClick.AddListener(() =>
        {
            ShopSkillManager.Instance?.PurchaseSkill(itemData, currencyType);
        });
    }

    /* ==============================================================  
       2-d) 나머지(다이아·프리 등 재화) 기존 string 기반 오버로드  
    ============================================================== */
    public void Setup(string name, Sprite sprite, int price,
                      string currencyType, string itemId, string itemType)
    {
        SetupInternal(name, sprite, price, currencyType);
        if (purchaseButton == null) return;

        purchaseButton.onClick.RemoveAllListeners();
        purchaseButton.onClick.AddListener(() =>
        {
            switch (itemType)
            {
                case "Diamond":
                    ShopDiamondManager.Instance?.PurchaseDiamond(itemId, currencyType);
                    break;
                case "Free":
                    ShopFreeManager.Instance?.PurchaseFree(itemId, currencyType);
                    break;
                case "Japha":
                    ShopJaphwaManager.Instance?.PurchaseJapha(itemId, currencyType);
                    break;
                case "Mileage":
                    ShopMileageManager.Instance?.PurchaseMileage(itemId, currencyType);
                    break;
                case "Special":
                    ShopSpecialManager.Instance?.PurchaseSpecial(itemId);
                    break;
                default:
                    Debug.LogWarning($"[SmallItemController] 알 수 없는 itemType: {itemType}");
                    break;
            }
        });
    }

    /* ==============================================================  
       3) 내부 공통 UI 세팅  
    ============================================================== */
    private void SetupInternal(string name, Sprite sprite, int price, string currencyType)
    {
        // ① 아이템명
        if (itemNameText != null)
            itemNameText.text = name;

        // ② 아이콘
        if (itemImage != null)
            itemImage.sprite = sprite;

        // ③ 가격 표시
        if (currencyType == "WON")
        {
            // 현금 UI
            if (priceText != null)
                priceText.gameObject.SetActive(false);
            if (wonPriceText != null)
            {
                wonPriceText.gameObject.SetActive(true);
                wonPriceText.text = $"￦ {price}";
            }
        }
        else
        {
            // 기타 화폐 UI
            if (wonPriceText != null)
                wonPriceText.gameObject.SetActive(false);
            if (priceText != null)
            {
                priceText.gameObject.SetActive(true);
                priceText.text = currencyType == "FREE" ? "광고보기" : price.ToString();
            }
        }

        // ④ 통화 아이콘
        if (currencyIconImage == null) return;
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
            currencyIconImage.enabled = icon != null;
            if (icon != null)
                currencyIconImage.sprite = icon;
        }
    }
}
