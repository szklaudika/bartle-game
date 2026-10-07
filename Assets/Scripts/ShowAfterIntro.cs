using UnityEngine;

public class ShowAfterIntro : MonoBehaviour
{
    public IntroSequenceController intro;

    public float fallbackDelay = 15f;

    private CanvasGroup canvasGroup;
    private float timer = 0f;
    private bool shown = false;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void Start()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    void Update()
    {
        if (shown) return;

        bool shouldShow = false;

        if (intro != null)
        {
            if (intro.IntroFinished)
                shouldShow = true;
        }
        else
        {
            // Fallback: use a simple timer if no IntroSequenceController reference
            timer += Time.deltaTime;
            if (timer >= fallbackDelay)
                shouldShow = true;
        }

        if (shouldShow)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            shown = true;
            enabled = false; // stop checking
        }
    }
}
