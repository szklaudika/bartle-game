using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityTutorial.PlayerControl;

// Po splnení poslednej úlohy tutoriálu ponúkne prechod do otvoreného sveta (ForestDefender).
// Okno je v štýle Bloodlines UI (obrázky z Assets/Resources/UI/Settings), vytvára ho QuestUI.
public class TutorialCompleteUI : MonoBehaviour
{
    public string openWorldSceneName = "ForestDefender";
    [Tooltip("Ako dlho po splnení úlohy sa okno ukáže – najprv dobehne oznámenie o splnení")]
    public float showDelay = 3.5f;

    private static readonly Color Green = new Color(0.2f, 0.68f, 0.3f);
    private static readonly Color LightGreen = new Color(0.88f, 1f, 0.85f);
    private static readonly Color TextColor = new Color(0.86f, 0.86f, 0.82f);

    public static TutorialCompleteUI Instance { get; private set; }

    private QuestManager _quests;
    private GameObject _window;
    private bool _shown;

    private void Awake() => Instance = this;

    // jeleň po rozhovore na konci okno ponúkne znova (ak hráč predtým zvolil "Zostať tu")
    public void Show()
    {
        if (_window != null && !_window.activeSelf) SetOpen(true);
    }

    private void Start()
    {
        _quests = QuestManager.Instance;
        if (_quests == null) return;
        _quests.OnQuestsChanged += CheckDone;
        BuildUI();
    }

    private void OnDestroy()
    {
        if (_quests != null) _quests.OnQuestsChanged -= CheckDone;
        if (Instance == this) Instance = null;
    }

    private void CheckDone()
    {
        if (_shown || _quests.CurrentStage != QuestManager.Stage.Done) return;
        _shown = true;
        if (FirebaseGameAnalytics.Instance != null && FirebaseGameAnalytics.Instance.IsReady)
            FirebaseGameAnalytics.Instance.LogGameplayEvent("tutorial_complete");
        StartCoroutine(ShowLater());
    }

    private IEnumerator ShowLater()
    {
        yield return new WaitForSecondsRealtime(showDelay);
        // počkaj, kým hráč nedohovorí alebo nemá pauzu
        while (PauseMenuController.IsPaused || (QuestUI.Instance != null && QuestUI.Instance.IsDialogueOpen))
            yield return null;
        SetOpen(true);
    }

    private void SetOpen(bool open)
    {
        _window.SetActive(open);
        Cursor.visible = open;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        if (open) SetPlayerCanMove(false);
        else StartCoroutine(ReleasePlayerNextFrame());
    }

    // kliknutie na "Zostať tu" by v tej istej snímke ešte vystrelilo ohnivú guľu
    private IEnumerator ReleasePlayerNextFrame()
    {
        yield return null;
        SetPlayerCanMove(true);
    }

    private static void SetPlayerCanMove(bool canMove)
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null) player.canMove = canMove;
    }

    private void StartOpenWorld()
    {
        PlayerPrefs.SetString("SavedScene", openWorldSceneName);
        PlayerPrefs.Save();
        SceneManager.LoadScene(openWorldSceneName);
    }

    // ---------- Stavba UI ----------

    private void BuildUI()
    {
        TMP_FontAsset titleFont = Resources.Load<TMP_FontAsset>("UI/Fonts/ManufacturingConsent SDF");
        TMP_FontAsset textFont = Resources.Load<TMP_FontAsset>("UI/Fonts/MedievalSharp SDF");

        var canvasGo = new GameObject("TutorialCompleteCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 70; // nad všetkým ostatným UI
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // stmavenie hry za oknom
        var dim = NewUI<Image>("Dim", canvasGo.transform);
        dim.color = new Color(0f, 0f, 0f, 0.55f);
        Stretch(dim.rectTransform);
        _window = dim.gameObject;

        var panel = NewUI<Image>("Window", dim.transform);
        panel.sprite = Resources.Load<Sprite>("UI/Settings/Panel");
        panel.type = Image.Type.Sliced;
        panel.pixelsPerUnitMultiplier = 0.45f;
        if (panel.sprite == null) panel.color = new Color(0.04f, 0.09f, 0.06f, 0.95f);
        var rt = panel.rectTransform;
        Place(rt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860, 520));

        var title = Text(rt, "Title", "Tutoriál dokončený!", titleFont, 76, Green);
        Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -100), new Vector2(760, 100));

        var body = Text(rt, "Body",
            "Les je zachránený – potok je čistý, drevorubač je preč a rastú nové stromy.\n\n" +
            "Teraz môžeš vstúpiť do otvoreného sveta a žiť v lese po svojom.",
            textFont, 30, TextColor);
        Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -255), new Vector2(700, 190));

        Button start = MakeButton(rt, "OpenWorld", "Otvorený svet", "Button3", titleFont, StartOpenWorld);
        Place((RectTransform)start.transform, new Vector2(0.5f, 0f), new Vector2(-165, 85), new Vector2(300, 80));

        Button stay = MakeButton(rt, "Stay", "Zostať tu", "Button2", titleFont, () => SetOpen(false));
        Place((RectTransform)stay.transform, new Vector2(0.5f, 0f), new Vector2(165, 85), new Vector2(300, 80));

        _window.SetActive(false);
    }

    private static Button MakeButton(RectTransform parent, string name, string text, string style,
                                     TMP_FontAsset font, UnityEngine.Events.UnityAction onClick)
    {
        var image = NewUI<Image>(name, parent);
        image.sprite = Resources.Load<Sprite>($"UI/Settings/{style}_Normal");
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 0.6f;
        if (image.sprite == null) image.color = new Color(0.08f, 0.2f, 0.12f);

        var button = image.gameObject.AddComponent<Button>();
        if (image.sprite != null)
        {
            button.transition = Selectable.Transition.SpriteSwap;
            Sprite hover = Resources.Load<Sprite>($"UI/Settings/{style}_Hover");
            button.spriteState = new SpriteState
            {
                highlightedSprite = hover,
                selectedSprite = hover,
                pressedSprite = Resources.Load<Sprite>($"UI/Settings/{style}_Pressed")
            };
        }
        button.onClick.AddListener(onClick);

        var label = Text(image.rectTransform, "Label", text, font, 42, Green);
        Stretch(label.rectTransform);

        var colors = button.gameObject.AddComponent<MenuButtonLabel>();
        colors.normalColor = Green;
        colors.activeColor = LightGreen;
        colors.Refresh();
        return button;
    }

    private static TextMeshProUGUI Text(RectTransform parent, string name, string text, TMP_FontAsset font, float size, Color color)
    {
        var t = NewUI<TextMeshProUGUI>(name, parent);
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    private static T NewUI<T>(string name, Transform parent) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.AddComponent<T>();
    }

    private static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
