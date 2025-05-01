using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FarmPlotSeedController : MonoBehaviour
{
    [Header("UI References")]
    public Image seedImage;               // FarmSeedImage_Panel 내 Image 컴포넌트
    public Button seedButton;             // 심기용 버튼 (이미지 위에 붙여놓으면 편해요)
    public TextMeshProUGUI seedNameText;  // FarmSeedName_Panel 내 Text 컴포넌트
    public Button infoButton;             // FarmSeedInfoBtn_Panel 내 버튼

    private SeedData _data;
    private FarmPopupManager _parent;
    private int _plotIndex;

    /// <summary>
    /// data.SpriteName 을 “SeedImages/{SpriteName}” 경로로 Resources.Load 합니다.
    /// </summary>
    public void Initialize(SeedData data, FarmPopupManager parent, int plotIndex)
    {
        _data = data;
        _parent = parent;
        _plotIndex = plotIndex;

        seedNameText.text = data.Name;

        // 1) Resources 폴더에서 스프라이트 로드
        LoadSeedSprite(data.SpriteName);

        seedButton.onClick.AddListener(() =>
            _parent.OnSelectSeed(_data)
        );

        infoButton.onClick.AddListener(() =>
            _parent.OnShowSeedInfo(_data)
        );
    }

    private void LoadSeedSprite(string spriteName)
    {
        var path = $"Sprites/Farm/{spriteName}";
        Sprite s = Resources.Load<Sprite>(path);
        if (s != null)
            seedImage.sprite = s;
        else
            Debug.LogWarning($"스프라이트를 못찾음: {path}");
    }

}
