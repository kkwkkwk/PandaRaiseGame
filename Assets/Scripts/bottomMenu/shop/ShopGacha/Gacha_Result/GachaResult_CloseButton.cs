using UnityEngine;
using UnityEngine.UI;

public class GachaResult_CloseButton : MonoBehaviour
{
    [Header("닫기 버튼")]
    public Button closeButton;

    [Header("결과 컨트롤러")]
    public GachaResultController resultController;

    private void Awake()
    {
        // Inspector에 할당되지 않았다면 동일 GameObject의 Button 컴포넌트를 자동으로 찾습니다.
        if (closeButton == null)
            closeButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
    }

    private void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnCloseClicked);
    }

    private void OnCloseClicked()
    {
        // GachaResultController의 HideResults를 호출해 결과창을 닫습니다.
        if (resultController != null)
            resultController.HideResults();
    }
}
