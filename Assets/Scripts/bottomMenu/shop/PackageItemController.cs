// PackageItemController.cs         ← 파일명과 동일해야 합니다!
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PackageItemController : MonoBehaviour
{
    /* ──────────────── 인스펙터 필드 ──────────────── */
    [Header("상품 아이콘")]
    public Image packageIconImage;

    [Header("패키지 제목")]
    public TextMeshProUGUI packageTitleText;

    [Header("패키지 설명(여러 줄)")]
    public TextMeshProUGUI[] descriptionTexts;

    [Header("가격 표시")]
    public TextMeshProUGUI priceText;

    [Header("구매 버튼")]
    public Button buyButton;

    /* ──────────────── 내부 상태 ──────────────── */
    private string packageId;   // 서버 식별용 ID

    /* ==============================================================
       Setup : 서버에서 받은 패키지 데이터를 UI에 반영
    ============================================================== */
    public void Setup(Sprite iconSprite,
                      string packageId,
                      string packageTitle,
                      string[] packageDescLines,
                      int price,
                      string currencyType)
    {
        this.packageId = packageId;

        /* 1) 아이콘 */
        if (packageIconImage != null)
        {
            bool hasIcon = iconSprite != null;
            packageIconImage.gameObject.SetActive(hasIcon);
            if (hasIcon) packageIconImage.sprite = iconSprite;
        }

        /* 2) 제목 */
        if (packageTitleText != null)
            packageTitleText.text = packageTitle;

        /* 3) 설명(최대 라인 수만큼 처리) */
        for (int i = 0; i < descriptionTexts.Length; i++)
        {
            if (descriptionTexts[i] == null) continue;

            bool hasLine = i < packageDescLines.Length;
            descriptionTexts[i].gameObject.SetActive(hasLine);
            if (hasLine) descriptionTexts[i].text = packageDescLines[i];
        }

        /* 4) 가격 표시 */
        if (priceText != null)
            priceText.text = currencyType == "₩"
                           ? $"\\{price}"          // 원화 기호 앞에 \ 추가
                           : $"{currencyType}{price}";

        /* 5) 구매 버튼 이벤트 */
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyButtonClicked);
        }
    }

    /* ==============================================================
       구매 버튼 콜백
    ============================================================== */
    private void OnBuyButtonClicked()
    {
        Debug.Log($"[PackageItemController] 구매 요청 → ID={packageId}, 제목={packageTitleText?.text}");
        ShopSpecialManager.Instance?.PurchaseSpecial(packageId);
    }
}
