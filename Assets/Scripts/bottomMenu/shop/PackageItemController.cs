using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopPackageCardUI : MonoBehaviour
{
    [Header("상품 아이콘")]
    [Tooltip("상품 이미지 표시용 UI Image")]
    public Image packageIconImage;

    [Header("패키지 제목")]
    [Tooltip("패키지 제목을 보여줄 TextMeshProUGUI")]
    public TextMeshProUGUI packageTitleText;

    [Header("패키지 설명(여러 줄)")]
    [Tooltip("각 줄마다 드래그해서 연결하세요")]
    public TextMeshProUGUI[] descriptionTexts;

    [Header("가격 표시")]
    [Tooltip("가격 및 결제 수단을 보여줄 TextMeshProUGUI")]
    public TextMeshProUGUI priceText;

    [Header("구매 버튼")]
    public Button buyButton;

    // 서버 식별용 패키지 ID
    private string packageId;

    /// <summary>
    /// 패키지 정보를 UI에 적용합니다.
    /// </summary>
    /// <param name="iconSprite">상품 아이콘 스프라이트</param>
    /// <param name="packageId">서버 식별용 패키지 ID</param>
    /// <param name="packageTitle">패키지 제목</param>
    /// <param name="packageDescLines">각 줄 설명 문자열 배열</param>
    /// <param name="price">가격</param>
    /// <param name="currencyType">결제 수단 기호 (예: "₩", "$" 등)</param>
    public void Setup(Sprite iconSprite, string packageId, string packageTitle, string[] packageDescLines, int price, string currencyType)
    {
        this.packageId = packageId;

        // 1) 아이콘
        if (packageIconImage != null)
        {
            if (iconSprite != null)
            {
                packageIconImage.sprite = iconSprite;
                packageIconImage.gameObject.SetActive(true);
            }
            else packageIconImage.gameObject.SetActive(false);
        }

        // 2) 제목
        if (packageTitleText != null)
            packageTitleText.text = packageTitle;

        // 3) 설명 배열
        for (int i = 0; i < descriptionTexts.Length; i++)
        {
            var descField = descriptionTexts[i];
            if (descField == null) continue;
            if (i < packageDescLines.Length)
            {
                descField.text = packageDescLines[i];
                descField.gameObject.SetActive(true);
            }
            else descField.gameObject.SetActive(false);
        }

        // 4) 가격 표시
        if (priceText != null)
        {
            if (currencyType == "₩")
                priceText.text = "\\" + price;
            else priceText.text = $"{currencyType}{price}";
        }

        // 5) 버튼 이벤트 연결
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyButtonClicked);
        }
    }

    private void OnBuyButtonClicked()
    {
        Debug.Log($"[ShopPackageCardUI] 구매 요청: ID={packageId}, 제목={packageTitleText.text}");
        // SpecialManager의 PurchaseSpecial 메소드 호출
        SpecialManager.Instance?.PurchaseSpecial(packageId);
    }
}