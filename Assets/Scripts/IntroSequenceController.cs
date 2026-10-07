using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class DialogueEntry
{
    [Tooltip("Text to display")] public string text;
    [Tooltip("Time in seconds to show this dialogue (0 = during initial black screen)")] public float displayTime;
    [Tooltip("Duration of the fade transition (seconds)")] public float fadeDuration = 0.5f;
}

public class IntroSequenceController : MonoBehaviour
{
    [Header("Intro Audio")]
    public AudioSource introAudioSource;   
    public AudioClip introMusic;
    public AudioClip secondMusic;
    public float audioVolume = 0.6f;

    public bool IntroFinished => introFinished;

    [Header("References")]
    public Camera introCamera;
    public SplinePath path;
    public GameObject player;
    public Transform spawnPoint;
    public Camera gameplayCamera;
    public TutorialManager tutorialManager;

    [Header("UI")]
    public TextMeshProUGUI dialogueText;
    public CanvasGroup dialogueCanvasGroup;

    [Header("Full Screen Fade")]
    public CanvasGroup fullScreenFadeCanvasGroup;
    public float initialHoldDuration = 2f, initialFadeDuration = 1f, finalFadeDuration = 1f, finalHoldDuration = 0.5f;

    [Header("Post-Processing")]
    public UnityEngine.Rendering.Volume postProcessVolume;
    private UnityEngine.Rendering.Universal.DepthOfField depthOfField;

    [Header("Timing")]
    public float introDuration = 10f;
    public float lookAheadAmount = 0.05f;
    public List<DialogueEntry> dialogueEntries = new List<DialogueEntry>();

    private Coroutine introCoroutine;
    private Coroutine currentDialogueFade;
    private bool introFinished = false;
    private bool isPaused = false;

    private FreeCamera cachedFreeCam;

    void Start()
    {
        if (introCamera)
        {
            introCamera.gameObject.SetActive(true);
            introCamera.targetDisplay = 0;
        }

        if (gameplayCamera)
        {
            cachedFreeCam = gameplayCamera.GetComponent<FreeCamera>();
            if (cachedFreeCam) cachedFreeCam.enabled = false;
            gameplayCamera.gameObject.SetActive(false);
            gameplayCamera.targetDisplay = 0;
        }

        if (player) player.SetActive(false);
        if (fullScreenFadeCanvasGroup) fullScreenFadeCanvasGroup.alpha = 1f;

        if (postProcessVolume) postProcessVolume.profile.TryGet(out depthOfField);

        if (introCamera && path)
        {
            introCamera.transform.position = path.GetPoint(0f);
            introCamera.transform.LookAt(path.GetPoint(lookAheadAmount));
        }

        if (introAudioSource != null && introMusic != null)
            StartCoroutine(PlayIntroAudioSequence());

        introCoroutine = StartCoroutine(PlayIntro());
    }

    public void PauseIntro() => isPaused = true;
    public void ResumeIntro() => isPaused = false;

    IEnumerator PlayIntroAudioSequence()
    {
        introAudioSource.clip = introMusic;
        introAudioSource.volume = audioVolume;
        introAudioSource.loop = false;
        introAudioSource.Play();

        yield return new WaitForSeconds(introAudioSource.clip.length);

        if (secondMusic != null)
        {
            introAudioSource.clip = secondMusic;
            introAudioSource.volume = audioVolume;
            introAudioSource.loop = false;
            introAudioSource.Play();
            yield return new WaitForSeconds(introAudioSource.clip.length);
        }

        if (GameMusicManager.instance != null)
            GameMusicManager.instance.PlayMusic();
    }

    IEnumerator PlayIntro()
    {
        // Phase 1: Initial Black Screen
        if (dialogueEntries.Count > 0)
        {
            dialogueText.text = dialogueEntries[0].text;
            currentDialogueFade = StartCoroutine(FadeCanvasGroup(dialogueCanvasGroup, 0f, 1f, 1f));
        }

        float elapsedHold = 0f;
        while (elapsedHold < initialHoldDuration)
        {
            if (!isPaused)
                elapsedHold += Time.deltaTime;
            yield return null;
        }

        if (dialogueEntries.Count > 0)
        {
            if (currentDialogueFade != null) StopCoroutine(currentDialogueFade);
            yield return FadeCanvasGroup(dialogueCanvasGroup, 1f, 0f, 0.5f);
        }

        // Phase 2: Fade from Black
        if (fullScreenFadeCanvasGroup)
        {
            float timer = 0f;
            while (timer < initialFadeDuration)
            {
                if (!isPaused)
                    timer += Time.deltaTime;
                fullScreenFadeCanvasGroup.alpha = 1f - (timer / initialFadeDuration);
                yield return null;
            }
            fullScreenFadeCanvasGroup.alpha = 0f;
        }

        // Phase 3: Camera Path & Dialogue
        float mainElapsed = 0f;
        int dialogueIndex = 1;
        string lastDialogueText = "";

        while (mainElapsed < introDuration)
        {
            if (!isPaused)
            {
                mainElapsed += Time.deltaTime;
                float t = mainElapsed / introDuration;

                Vector3 cameraPos = path.GetPoint(t);
                introCamera.transform.position = cameraPos;
                introCamera.transform.LookAt(path.GetPoint(Mathf.Clamp01(t + lookAheadAmount)));

                // Dialogue
                if (dialogueIndex < dialogueEntries.Count)
                {
                    var entry = dialogueEntries[dialogueIndex];
                    if (mainElapsed >= entry.displayTime && lastDialogueText != entry.text)
                    {
                        if (currentDialogueFade != null) StopCoroutine(currentDialogueFade);
                        if (dialogueCanvasGroup.alpha > 0f) yield return FadeCanvasGroup(dialogueCanvasGroup, 1f, 0f, 0.3f);
                        dialogueText.text = entry.text;
                        lastDialogueText = entry.text;
                        currentDialogueFade = StartCoroutine(FadeCanvasGroup(dialogueCanvasGroup, 0f, 1f, entry.fadeDuration));
                        dialogueIndex++;
                    }
                }

                if (depthOfField != null) depthOfField.focalLength.value = Mathf.Lerp(10f, 150f, t);
            }
            yield return null;
        }

        // Phase 4: Final Fade to Black
        if (currentDialogueFade != null) StopCoroutine(currentDialogueFade);
        if (dialogueCanvasGroup.alpha > 0f) yield return FadeCanvasGroup(dialogueCanvasGroup, 1f, 0f, 0.5f);
        if (fullScreenFadeCanvasGroup) yield return FadeCanvasGroup(fullScreenFadeCanvasGroup, 0f, 1f, finalFadeDuration);
        yield return new WaitForSeconds(finalHoldDuration);

        // Phase 5: Transition to gameplay
        if (player)
        {
            player.transform.position = spawnPoint.position;
            player.transform.rotation = spawnPoint.rotation;
            player.SetActive(true);
        }

        if (gameplayCamera)
        {
            gameplayCamera.gameObject.SetActive(true);
            gameplayCamera.enabled = true;
        }
        if (cachedFreeCam) cachedFreeCam.enabled = true;

        yield return null;

        if (introCamera) introCamera.gameObject.SetActive(false);
        if (fullScreenFadeCanvasGroup)
            yield return FadeCanvasGroup(fullScreenFadeCanvasGroup, 1f, 0f, initialFadeDuration);

        if (postProcessVolume) postProcessVolume.enabled = false;
        introFinished = true;

        if (tutorialManager != null) tutorialManager.StartTutorial();

        if (GameMusicManager.instance != null) GameMusicManager.instance.PlayMusic();
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
    {
        if (cg == null || duration <= 0f) yield break;
        float timer = 0f;
        while (timer < duration)
        {
            if (!isPaused) timer += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            yield return null;
        }
        cg.alpha = endAlpha;
    }
}
