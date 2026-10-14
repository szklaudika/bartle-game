using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Okná hlavného menu v štýle Bloodlines UI (zelená verzia obrázkov v Assets/Resources/UI/Settings):
// - nastavenia: rám s nadpisom a krížikom, riadky "Volume: 70%" so segmentovým posuvníkom a reproduktorom,
//   tlačidlá Fullscreen, Reset a Apply – postaví sa za behu do existujúceho panelu nastavení,
// - Nová hra: výber medzi otvoreným svetom (ForestDefender) a tutoriálom (TerrainDemoScene).
public static class SettingsPanelBuilder
{
    private const string Path = "UI/Settings/";

    public static readonly Color Green = new Color(0.2f, 0.68f, 0.3f);
    public static readonly Color LightGreen = new Color(0.88f, 1f, 0.85f); // text tlačidla pri prejdení myšou
    private static readonly Color LabelColor = new Color(0.86f, 0.86f, 0.82f);

    // vnútorný rám je široký 640 (okno 780 − 2 × 70), obsah má od neho 40 na každej strane
    private const float ContentHalfWidth = 280f;

    private const float DefaultVolume = 1f;
    private const float DefaultMusic = 0.6f;
    private const float DefaultSensitivity = 5f;

    private static TMP_FontAsset _titleFont;
    private static TMP_FontAsset _textFont;

    public static bool Build(MainMenuController menu, RectTransform panel, TMP_FontAsset titleFont, TMP_FontAsset textFont)
    {
        Sprite frame = Load("Panel");
        if (frame == null) return false; // obrázky chýbajú – ostane pôvodné okno
        _titleFont = titleFont;
        _textFont = textFont;

        // pôvodný obsah (zo staviteľa scény) preč
        for (int i = panel.childCount - 1; i >= 0; i--)
            Object.Destroy(panel.GetChild(i).gameObject);

        panel.sizeDelta = new Vector2(780, 1040);
        var background = panel.GetComponent<Image>();
        background.sprite = frame;
        background.type = Image.Type.Sliced;
        background.pixelsPerUnitMultiplier = 0.45f;
        background.color = Color.white;

        // nadpis a krížik
        var title = Text(panel, "Title", "Settings", _titleFont, 84, Green);
        Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -95), new Vector2(600, 110));

        Button close = IconButton(panel, "Close", Load("Close"), Load("CloseHover"), menu.BackToMain);
        Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-95, -95), new Vector2(64, 64));

        // vnútorný rám okolo nastavení
        var inner = NewUI<Image>("InnerFrame", panel);
        inner.sprite = Load("InnerFrame");
        inner.type = Image.Type.Sliced;
        inner.pixelsPerUnitMultiplier = 0.6f;
        inner.color = new Color(1f, 1f, 1f, 0.5f);
        inner.raycastTarget = false;
        var innerRt = inner.rectTransform;
        innerRt.anchorMin = Vector2.zero;
        innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(70, 60);
        innerRt.offsetMax = new Vector2(-70, -185);

        // posuvníky
        menu.masterVolumeSlider = SliderRow(panel, "Volume", -245, 0f, 1f, DefaultVolume, true, v => $"Volume: {Percent(v)}");
        menu.musicVolumeSlider = SliderRow(panel, "Music", -405, 0f, 1f, DefaultMusic, true, v => $"Music volume: {Percent(v)}");
        menu.mouseSensitivitySlider = SliderRow(panel, "Sensitivity", -565, 1f, 10f, DefaultSensitivity, false, v => $"Mouse sensitivity: {v:0.0}");

        // celá obrazovka
        Button fullscreen = TextButton(panel, "Fullscreen", "Fullscreen: On", "Button2", menu.ToggleFullscreen);
        Place((RectTransform)fullscreen.transform, new Vector2(0.5f, 1f), new Vector2(0, -770), new Vector2(420, 72));
        menu.fullscreenLabel = fullscreen.GetComponentInChildren<TextMeshProUGUI>();
        menu.settingsFirstButton = fullscreen;

        // Reset a Apply – nastavenia sa ukladajú hneď, Apply ich uloží a zavrie okno
        Button reset = TextButton(panel, "Reset", "Reset", "Button2", () =>
        {
            menu.masterVolumeSlider.value = DefaultVolume;
            menu.musicVolumeSlider.value = DefaultMusic;
            menu.mouseSensitivitySlider.value = DefaultSensitivity;
        });
        Place((RectTransform)reset.transform, new Vector2(0.5f, 1f), new Vector2(-145, -880), new Vector2(260, 80));

        Button apply = TextButton(panel, "Apply", "Apply", "Button3", menu.BackToMain);
        Place((RectTransform)apply.transform, new Vector2(0.5f, 1f), new Vector2(145, -880), new Vector2(260, 80));
        return true;
    }

    // ---------- okno Nová hra: otvorený svet alebo tutoriál ----------

    public static GameObject BuildNewGamePanel(MainMenuController menu, Transform root, TMP_FontAsset titleFont,
                                               TMP_FontAsset textFont, out Button firstButton)
    {
        firstButton = null;
        Sprite frame = Load("Panel");
        if (frame == null) return null;
        _titleFont = titleFont;
        _textFont = textFont;

        var background = NewUI<Image>("NewGamePanel", root);
        background.sprite = frame;
        background.type = Image.Type.Sliced;
        background.pixelsPerUnitMultiplier = 0.45f;
        var panel = background.rectTransform;
        Place(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 640));

        var title = Text(panel, "Title", "New Game", _titleFont, 84, Green);
        Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -95), new Vector2(700, 110));

        Button close = IconButton(panel, "Close", Load("Close"), Load("CloseHover"), menu.BackToMain);
        Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-95, -95), new Vector2(64, 64));

        var inner = NewUI<Image>("InnerFrame", panel);
        inner.sprite = Load("InnerFrame");
        inner.type = Image.Type.Sliced;
        inner.pixelsPerUnitMultiplier = 0.6f;
        inner.color = new Color(1f, 1f, 1f, 0.5f);
        inner.raycastTarget = false;
        Stretch(inner.rectTransform, new Vector2(70, 60), new Vector2(-70, -185));

        // tenká čiara medzi možnosťami
        var divider = NewUI<Image>("Divider", panel);
        divider.color = new Color(0.55f, 0.55f, 0.55f, 0.35f);
        divider.raycastTarget = false;
        Place(divider.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -385), new Vector2(2, 320));

        firstButton = Choice(panel, -205, "Open World",
            "Explore the forest freely and live your own story in an open world.",
            menu.StartOpenWorld);
        Choice(panel, 205, "Tutorial",
            "Learn the basics through easy quests: talk to the deer, clean the stream, stop the lumberjack and plant new trees.",
            menu.PlayTutorial);

        background.gameObject.SetActive(false);
        return background.gameObject;
    }

    // stĺpec s nadpisom, popisom a tlačidlom Start
    private static Button Choice(RectTransform panel, float x, string heading, string description, UnityAction onStart)
    {
        var head = Text(panel, heading + "Heading", heading, _titleFont, 56, Green);
        Place(head.rectTransform, new Vector2(0.5f, 1f), new Vector2(x, -250), new Vector2(360, 70));

        var body = Text(panel, heading + "Description", description, _textFont, 28, LabelColor);
        body.alignment = TextAlignmentOptions.Top;
        body.lineSpacing = -4;
        Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(x, -365), new Vector2(340, 150));

        Button start = TextButton(panel, heading + "Start", "Start", "Button3", onStart);
        Place((RectTransform)start.transform, new Vector2(0.5f, 1f), new Vector2(x, -505), new Vector2(260, 76));
        return start;
    }

    private static string Percent(float v) => v <= 0.001f ? "off" : $"{Mathf.RoundToInt(v * 100f)}%";

    // ---------- riadok: popis, posuvník a reproduktor ----------

    private static Slider SliderRow(RectTransform panel, string name, float y, float min, float max, float defaultValue,
                                    bool withMute, System.Func<float, string> format)
    {
        var label = Text(panel, name + "Label", "", _textFont, 40, LabelColor);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        Place(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(ContentHalfWidth * 2f, 50));

        // medzera pod popisom – rúčka posuvníka presahuje nad lištu a nesmie zasahovať do textu
        float barY = y - 82f;
        // s reproduktorom: lišta + medzera 30 + ikona 70 vyplnia celú šírku obsahu
        float barWidth = withMute ? ContentHalfWidth * 2f - 100f : ContentHalfWidth * 2f;
        float barX = -ContentHalfWidth + barWidth / 2f;
        Slider slider = SegmentSlider(panel, name + "Slider", new Vector2(barX, barY), barWidth, min, max);
        slider.onValueChanged.AddListener(v => label.text = format(v));

        if (withMute)
        {
            Sprite on = Load("SoundOn"), off = Load("SoundOff");
            float remembered = defaultValue;
            Button mute = IconButton(panel, name + "Mute", on, null, () =>
            {
                if (slider.value > min) { remembered = slider.value; slider.value = min; }
                else slider.value = remembered > min ? remembered : defaultValue;
            });
            Place((RectTransform)mute.transform, new Vector2(0.5f, 1f), new Vector2(ContentHalfWidth - 35f, barY), new Vector2(70, 70));
            var icon = (Image)mute.targetGraphic;
            slider.onValueChanged.AddListener(v => icon.sprite = v > min ? on : off);
        }

        // text a ikona sa obnovia aj po načítaní uložených hodnôt
        slider.gameObject.AddComponent<SliderLabelSync>().Init(slider);
        return slider;
    }

    private static Slider SegmentSlider(RectTransform panel, string name, Vector2 pos, float width, float min, float max)
    {
        var root = NewUI<RectTransform>(name, panel);
        Place(root, new Vector2(0.5f, 1f), pos, new Vector2(width, 64));

        var background = NewUI<Image>("Background", root);
        background.sprite = Load("SliderEmpty");
        Stretch(background.rectTransform, Vector2.zero, Vector2.zero);

        var fillArea = NewUI<RectTransform>("Fill Area", root);
        Stretch(fillArea, Vector2.zero, Vector2.zero);
        var fill = NewUI<Image>("Fill", fillArea);
        fill.sprite = Load("SliderFull");
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.raycastTarget = false;
        Stretch(fill.rectTransform, Vector2.zero, Vector2.zero);

        // rúčka – červené (tu zelené) kosoštvorce, presahuje nad a pod posuvník
        var handleArea = NewUI<RectTransform>("Handle Slide Area", root);
        Stretch(handleArea, new Vector2(10, 0), new Vector2(-10, 0));
        var handle = NewUI<Image>("Handle", handleArea);
        handle.sprite = Load("SliderHandle");
        handle.rectTransform.sizeDelta = new Vector2(30, 26); // presahuje o 13 hore aj dole

        var slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.transition = Selectable.Transition.None;
        slider.navigation = new Navigation { mode = Navigation.Mode.Automatic };
        return slider;
    }

    // ---------- tlačidlá ----------

    private static Button TextButton(RectTransform parent, string name, string text, string style, UnityAction onClick)
    {
        var image = NewUI<Image>(name + "Button", parent);
        image.sprite = Load(style + "_Normal");
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 0.6f;

        var button = image.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState
        {
            highlightedSprite = Load(style + "_Hover"),
            selectedSprite = Load(style + "_Hover"),
            pressedSprite = Load(style + "_Pressed")
        };
        button.onClick.AddListener(onClick);

        var label = Text(image.rectTransform, "Label", text, _titleFont, 44, Green);
        Stretch(label.rectTransform, Vector2.zero, Vector2.zero);

        // rám pri prejdení myšou zosvetlie, text tiež
        var colors = button.gameObject.AddComponent<MenuButtonLabel>();
        colors.normalColor = Green;
        colors.activeColor = LightGreen;
        colors.Refresh();
        return button;
    }

    private static Button IconButton(RectTransform parent, string name, Sprite normal, Sprite hover, UnityAction onClick)
    {
        var image = NewUI<Image>(name, parent);
        image.sprite = normal;
        image.preserveAspect = true;

        var button = image.gameObject.AddComponent<Button>();
        if (hover != null)
        {
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { highlightedSprite = hover, selectedSprite = hover, pressedSprite = hover };
        }
        else
        {
            var c = button.colors;
            c.highlightedColor = new Color(0.8f, 1f, 0.8f);
            c.selectedColor = Color.white;
            c.pressedColor = new Color(0.6f, 0.75f, 0.6f);
            button.colors = c;
        }
        button.onClick.AddListener(onClick);
        return button;
    }

    // ---------- pomocné ----------

    private static Sprite Load(string name) => Resources.Load<Sprite>(Path + name);

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
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return typeof(T) == typeof(RectTransform) ? go.GetComponent<T>() : go.AddComponent<T>();
    }

    private static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }
}
