using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InteractionPromptUI : MonoBehaviour
{
    public Text text;
    public float fadeDuration = 0.15f;

    CanvasGroup canvasGroup;
    Coroutine tempCoroutine;
    Coroutine fadeCoroutine;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (text == null) text = GetComponentInChildren<Text>();

        // Start hidden
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// Immediately show the prompt (fade in).
    /// </summary>
    public void Show(string message)
    {
        if (text != null) text.text = message;
        StartFade(1f);
    }

    /// <summary>
    /// Immediately hide the prompt (fade out).
    /// </summary>
    public void Hide()
    {
        StartFade(0f);
    }

    /// <summary>
    /// Show a temporary message: fade in, wait, fade out. Cancels any existing temporary/fade coroutines.
    /// </summary>
    public void ShowTemporary(string message, float duration)
    {
        if (tempCoroutine != null) StopCoroutine(tempCoroutine);
        tempCoroutine = StartCoroutine(ShowTemporaryCoroutine(message, duration));
    }

    /// <summary>
    /// Forcefully hide right now and cancel any running coroutines.
    /// Use this when you want to immediately clear any existing prompt before showing a new one.
    /// </summary>
    public void HideImmediate()
    {
        if (tempCoroutine != null) { StopCoroutine(tempCoroutine); tempCoroutine = null; }
        if (fadeCoroutine != null) { StopCoroutine(fadeCoroutine); fadeCoroutine = null; }
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    IEnumerator ShowTemporaryCoroutine(string message, float duration)
    {
        if (text != null) text.text = message;

        // Fade in
        yield return StartFadeCoroutine(1f);

        // Wait while visible
        yield return new WaitForSeconds(duration);

        // Fade out
        yield return StartFadeCoroutine(0f);

        tempCoroutine = null;
    }

    void StartFade(float target)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeToCoroutine(target));
    }

    IEnumerator StartFadeCoroutine(float target)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeToCoroutine(target));
        yield return fadeCoroutine;
    }

    IEnumerator FadeToCoroutine(float target)
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        float start = canvasGroup.alpha;
        float t = 0f;
        float duration = Mathf.Max(0.0001f, fadeDuration);
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        canvasGroup.alpha = target;
        canvasGroup.interactable = target > 0f;
        canvasGroup.blocksRaycasts = target > 0f;
        fadeCoroutine = null;
    }
}