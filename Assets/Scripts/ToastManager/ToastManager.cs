using System.Collections;
using UnityEngine;
using TMPro;

public class ToastManager : MonoBehaviour
{
    public static ToastManager Instance { get; private set; }

    [Header("Toast UI")]
    public GameObject toastPanel;      // 토스트 전체 패널
    public TextMeshProUGUI toastText;  // 메시지를 표시할 TMP 텍스트

    [Header("Display Settings")]
    public float displayDuration = 1f; // 토스트가 표시될 시간(초)

    private Coroutine currentCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // 시작 시 토스트 패널을 끄기
        if (toastPanel != null)
        {
            toastPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 메시지를 표시하고 일정 시간 후 자동으로 숨기는 함수
    /// </summary>
    public void ShowToast(string message)
    {
        // 만약 이미 다른 토스트가 표시 중이라면, 그 코루틴을 중단하고 새로 표시
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        currentCoroutine = StartCoroutine(CoShowToast(message));
    }

    private IEnumerator CoShowToast(string message)
    {
        toastText.text = message;
        toastPanel.SetActive(true);

        // 1) Panel alpha = 1 (시작)
        CanvasGroup cg = toastPanel.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;

        // 2) 정해진 시간만큼 대기
        yield return new WaitForSeconds(displayDuration);

        // 3) 서서히 alpha값을 0으로
        float fadeTime = 1f; // Fade가 걸릴 시간
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeTime);
            if (cg != null) cg.alpha = 1f - t;
            yield return null;
        }

        toastPanel.SetActive(false);
        currentCoroutine = null;
    }

}
