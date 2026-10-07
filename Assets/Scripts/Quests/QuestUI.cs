using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityTutorial.PlayerControl;

// Vytvorí si celé UI za behu: zoznam úloh, nápovedu "(E)", dialógové okno a oznámenie o splnení úlohy.
public class QuestUI : MonoBehaviour
{
    public static QuestUI Instance { get; private set; }

    [Header("Vzhľad")]
    public Color panelColor = new Color(0f, 0f, 0f, 0.6f);
    public Color speakerColor = new Color(1f, 0.85f, 0.4f);
    public Color doneColor = new Color(0.56f, 0.89f, 0.53f);
    public float toastDuration = 3f;

    public bool IsDialogueOpen { get; private set; }
    public int LastDialogueClosedFrame { get; private set; } = -1;

    private QuestManager _questManager;
    private TextMeshProUGUI _objectivesText;
    private TextMeshProUGUI _promptText;
    private TextMeshProUGUI _toastText;
    private GameObject _toastPanel;
    private GameObject _dialoguePanel;
    private TextMeshProUGUI _speakerText;
    private TextMeshProUGUI _bodyText;

    private string[] _lines;
    private int _lineIndex;
    private int _openedFrame;
    private Action _onDialogueFinished;
    private Coroutine _toastRoutine;

    private void Awake()
    {
        Instance = this;
        BuildUI();
    }

    private void Start()
    {
        _questManager = GetComponent<QuestManager>();
        _questManager.OnQuestsChanged += RefreshObjectives;
        _questManager.OnQuestCompleted += title => ShowToast($"Úloha splnená: {title}");
        RefreshObjectives();
    }

    private void Update()
    {
        if (!IsDialogueOpen || Time.frameCount == _openedFrame) return;

        var kb = Keyboard.current;
        if (kb != null && (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
            NextLine();
    }

    // ---------- Úlohy ----------

    private void RefreshObjectives()
    {
        var qm = _questManager;
        var sb = new StringBuilder("<b>Úlohy</b>\n");
        sb.AppendLine(ObjectiveLine(qm.talkQuestTitle, qm.TalkedToDeer));
        sb.Append(ObjectiveLine($"{qm.trashQuestTitle} – {qm.trashCounterLabel}: {qm.TrashCollected}/{qm.TrashTotal}", qm.StreamCleaned));
        _objectivesText.text = sb.ToString();
    }

    private string ObjectiveLine(string text, bool done) =>
        done ? $"<color=#{ColorUtility.ToHtmlStringRGB(doneColor)}><s>• {text}</s></color>" : $"• {text}";

    // ---------- Nápoveda (E) ----------

    public void ShowPrompt(string text)
    {
        _promptText.text = text + " (E)";
        _promptText.gameObject.SetActive(true);
    }

    public void HidePrompt() => _promptText.gameObject.SetActive(false);

    // ---------- Dialóg ----------

    public void ShowDialogue(string speaker, string[] lines, Action onFinished)
    {
        if (lines == null || lines.Length == 0)
        {
            onFinished?.Invoke();
            return;
        }

        _lines = lines;
        _lineIndex = 0;
        _onDialogueFinished = onFinished;
        _openedFrame = Time.frameCount;
        IsDialogueOpen = true;

        _speakerText.text = speaker;
        _bodyText.text = _lines[0];
        _dialoguePanel.SetActive(true);
        HidePrompt();
        SetPlayerCanMove(false);
    }

    private void NextLine()
    {
        _lineIndex++;
        if (_lineIndex < _lines.Length)
        {
            _bodyText.text = _lines[_lineIndex];
            return;
        }

        _dialoguePanel.SetActive(false);
        IsDialogueOpen = false;
        LastDialogueClosedFrame = Time.frameCount;
        SetPlayerCanMove(true);

        var callback = _onDialogueFinished;
        _onDialogueFinished = null;
        callback?.Invoke();
    }

    private static void SetPlayerCanMove(bool canMove)
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null) player.canMove = canMove;
    }

    // ---------- Oznámenie ----------

    public void ShowToast(string text)
    {
        if (_toastRoutine != null) StopCoroutine(_toastRoutine);
        _toastRoutine = StartCoroutine(ToastRoutine(text));
    }

    private IEnumerator ToastRoutine(string text)
    {
        _toastText.text = text;
        _toastPanel.SetActive(true);
        yield return new WaitForSecondsRealtime(toastDuration);
        _toastPanel.SetActive(false);
    }

    // ---------- Stavba UI ----------

    private void BuildUI()
    {
        var canvasGo = new GameObject("QuestCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasGo.transform;

        // zoznam úloh – vľavo hore
        var objectives = NewRect("Objectives", root);
        SetAnchors(objectives, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
        objectives.anchoredPosition = new Vector2(30, -30);
        objectives.sizeDelta = new Vector2(620, 0);
        AddImage(objectives, panelColor);
        AddAutoHeightLayout(objectives, fitWidth: false);
        _objectivesText = AddText(NewRect("Text", objectives), 26, TextAlignmentOptions.TopLeft, Color.white);

        // oznámenie – hore v strede
        var toast = NewRect("Toast", root);
        SetAnchors(toast, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
        toast.anchoredPosition = new Vector2(0, -120);
        AddImage(toast, panelColor);
        AddAutoHeightLayout(toast, fitWidth: true);
        _toastText = AddText(NewRect("Text", toast), 34, TextAlignmentOptions.Center, speakerColor);
        _toastText.fontStyle = FontStyles.Bold;
        _toastPanel = toast.gameObject;
        _toastPanel.SetActive(false);

        // nápoveda (E) – dole v strede
        var prompt = NewRect("Prompt", root);
        SetAnchors(prompt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
        prompt.anchoredPosition = new Vector2(0, 280);
        prompt.sizeDelta = new Vector2(900, 50);
        _promptText = AddText(prompt, 30, TextAlignmentOptions.Center, Color.white);
        _promptText.outlineWidth = 0.2f;
        _promptText.outlineColor = Color.black;
        _promptText.gameObject.SetActive(false);

        // dialógové okno – dole
        var dialogue = NewRect("Dialogue", root);
        SetAnchors(dialogue, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
        dialogue.anchoredPosition = new Vector2(0, 40);
        dialogue.sizeDelta = new Vector2(1100, 220);
        AddImage(dialogue, panelColor);

        var speaker = NewRect("Speaker", dialogue);
        SetAnchors(speaker, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
        speaker.anchoredPosition = new Vector2(0, -14);
        speaker.sizeDelta = new Vector2(-48, 44);
        _speakerText = AddText(speaker, 30, TextAlignmentOptions.Left, speakerColor);
        _speakerText.fontStyle = FontStyles.Bold;

        var body = NewRect("Body", dialogue);
        SetAnchors(body, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        body.offsetMin = new Vector2(24, 44);
        body.offsetMax = new Vector2(-24, -62);
        _bodyText = AddText(body, 28, TextAlignmentOptions.TopLeft, Color.white);

        var hint = NewRect("Hint", dialogue);
        SetAnchors(hint, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0));
        hint.anchoredPosition = new Vector2(-20, 10);
        hint.sizeDelta = new Vector2(400, 32);
        AddText(hint, 20, TextAlignmentOptions.Right, new Color(1, 1, 1, 0.6f)).text = "E – pokračovať";

        _dialoguePanel = dialogue.gameObject;
        _dialoguePanel.SetActive(false);
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = pivot;
    }

    private static void AddImage(RectTransform rt, Color color)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    // panel, ktorého výška (a voliteľne šírka) sa prispôsobí textu
    private static void AddAutoHeightLayout(RectTransform rt, bool fitWidth)
    {
        var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 14, 14);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = rt.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        if (fitWidth) fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static TextMeshProUGUI AddText(RectTransform rt, float size, TextAlignmentOptions align, Color color)
    {
        var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.alignment = align;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }
}
