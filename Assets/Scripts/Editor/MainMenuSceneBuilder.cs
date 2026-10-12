using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.Video;

// Postaví scénu hlavného menu (video v pozadí, názov hry, tlačidlá, tutoriál, nastavenia)
// a nastaví ju ako prvú scénu v Build Settings.
// Menu: Tools > Menu > Build Main Menu Scene. Dá sa spustiť opakovane (scénu prepíše).
public static class MainMenuSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameScenePath = "Assets/Scenes/Game.unity";               // New Game (zatiaľ prázdna)
    private const string TutorialScenePath = "Assets/Scenes/TerrainDemoScene.unity"; // Play Tutorial
    private const string VideoPath = "Assets/menu/Video Project 2.mp4";
    private const string GameTitle = "Forest Guardian";

    private static readonly Color TitleColor = new Color(1f, 0.96f, 0.85f);
    private static readonly Color PanelColor = new Color(0.04f, 0.09f, 0.06f, 0.88f);
    private static readonly Color ButtonColor = new Color(0.08f, 0.2f, 0.12f, 0.8f);
    private static readonly Color ButtonHoverColor = new Color(0.22f, 0.45f, 0.26f, 0.95f);
    private static readonly Color AccentColor = new Color(0.62f, 0.86f, 0.55f);

    private static TMP_FontAsset _font;
    private static Sprite _roundedSprite;

    [MenuItem("Tools/Menu/Build Main Menu Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog("Main menu", ScenePath + " already exists. Rebuild it?", "Rebuild", "Cancel"))
            return;

        _font = TMP_Settings.defaultFontAsset;
        if (_font == null)
        {
            string guid = AssetDatabase.FindAssets("t:TMP_FontAsset").FirstOrDefault();
            if (guid != null) _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
        }
        _roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        CreateGameSceneIfMissing();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---------- camera ----------
        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        var cam = camGo.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;

        // ---------- background video ----------
        VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoPath);
        if (clip == null) Debug.LogWarning("Main menu: video not found at " + VideoPath);

        var videoGo = new GameObject("BackgroundVideo", typeof(VideoPlayer));
        var video = videoGo.GetComponent<VideoPlayer>();
        video.clip = clip;
        video.playOnAwake = false;     // MainMenuController ho spustí s RenderTexture
        video.isLooping = true;
        video.audioOutputMode = VideoAudioOutputMode.None;
        video.renderMode = VideoRenderMode.RenderTexture;

        // ---------- canvas + event system ----------
        var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        Transform root = canvasGo.transform;

        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

        // video fills the screen (cropped, never stretched)
        var videoImage = NewUI<RawImage>("VideoBackground", root);
        Stretch(videoImage.rectTransform);
        videoImage.raycastTarget = false;
        var fitter = videoImage.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = clip != null && clip.height > 0 ? (float)clip.width / clip.height : 16f / 9f;

        // darken a little so the text is readable
        var shade = NewUI<Image>("Shade", root);
        Stretch(shade.rectTransform);
        shade.color = new Color(0f, 0f, 0f, 0.3f);
        shade.raycastTarget = false;

        // ---------- title ----------
        var titleRoot = NewUI<RectTransform>("Title", root);
        Place(titleRoot, new Vector2(0.5f, 1f), new Vector2(0, -220), new Vector2(1600, 220));
        var titleShadow = Text("Shadow", titleRoot, GameTitle, 140, new Color(0f, 0f, 0f, 0.6f), FontStyles.Bold);
        Stretch(titleShadow.rectTransform);
        titleShadow.rectTransform.anchoredPosition = new Vector2(6, -6);
        var title = Text("Text", titleRoot, GameTitle, 140, TitleColor, FontStyles.Bold);
        Stretch(title.rectTransform);
        title.characterSpacing = 6;
        titleShadow.characterSpacing = 6;

        var controller = canvasGo.AddComponent<MainMenuController>();
        controller.videoPlayer = video;
        controller.videoImage = videoImage;

        // ---------- main panel ----------
        var main = NewUI<RectTransform>("MainPanel", root);
        Place(main, new Vector2(0.5f, 0.5f), new Vector2(0, -120), new Vector2(460, 420));
        var layout = main.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 22;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Button newGame = MenuButton(main, "New Game", controller.NewGame);
        MenuButton(main, "Tutorial", controller.OpenTutorial);
        MenuButton(main, "Settings", controller.OpenSettings);
        MenuButton(main, "Quit", controller.QuitGame);

        // ---------- tutorial ----------
        var tutorial = Panel("TutorialPanel", root, "How to Play", new Vector2(1200, 960));
        var body = Text("Text", tutorial, TutorialText, 30, Color.white, FontStyles.Normal);
        body.alignment = TextAlignmentOptions.TopLeft;
        body.lineSpacing = 8;
        Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -420), new Vector2(1060, 580));
        // "Play Tutorial" just above "Back"
        Button playTutorial = MenuButton(tutorial, "Play Tutorial", controller.PlayTutorial);
        Place((RectTransform)playTutorial.transform, new Vector2(0.5f, 0f), new Vector2(0, 170), new Vector2(380, 80));
        BackButton(tutorial, controller.BackToMain);

        // ---------- settings ----------
        var settings = Panel("SettingsPanel", root, "Settings", new Vector2(1000, 720));
        controller.masterVolumeSlider = SliderRow(settings, "Volume", -200, 0f, 1f);
        controller.musicVolumeSlider = SliderRow(settings, "Music", -300, 0f, 1f);
        controller.mouseSensitivitySlider = SliderRow(settings, "Mouse Sensitivity", -400, 1f, 10f);

        Button fullscreen = MenuButton(settings, "Fullscreen: On", controller.ToggleFullscreen);
        Place((RectTransform)fullscreen.transform, new Vector2(0.5f, 1f), new Vector2(0, -510), new Vector2(460, 80));
        controller.fullscreenLabel = fullscreen.GetComponentInChildren<TextMeshProUGUI>();
        BackButton(settings, controller.BackToMain);

        controller.title = titleRoot.gameObject;
        controller.mainPanel = main.gameObject;
        controller.tutorialPanel = tutorial.gameObject;
        controller.settingsPanel = settings.gameObject;
        controller.mainFirstButton = newGame;
        controller.tutorialFirstButton = playTutorial;
        controller.settingsFirstButton = fullscreen;

        tutorial.gameObject.SetActive(false);
        settings.gameObject.SetActive(false);

        // ---------- save + build settings ----------
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        SetBuildScenes();

        Debug.Log("Main menu built: " + ScenePath + " (Build Settings: MainMenu, Game, TerrainDemoScene).");
    }

    // empty scene for "New Game" – only a camera and a light, so it isn't completely black
    private static void CreateGameSceneIfMissing()
    {
        if (File.Exists(GameScenePath)) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 1.6f, -10f);

        var lightGo = new GameObject("Directional Light", typeof(Light));
        var light = lightGo.GetComponent<Light>();
        light.type = LightType.Directional;
        light.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        EditorSceneManager.SaveScene(scene, GameScenePath);
    }

    // ------------------- build settings -------------------

    private static void SetBuildScenes()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true),
            new EditorBuildSettingsScene(TutorialScenePath, true)
        };
        // keep any other scenes that still exist
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.path == ScenePath || s.path == GameScenePath || s.path == TutorialScenePath || !File.Exists(s.path)) continue;
            scenes.Add(s);
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ------------------- UI helpers -------------------

    private static T NewUI<T>(string name, Transform parent) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return typeof(T) == typeof(RectTransform) ? go.GetComponent<T>() : go.AddComponent<T>();
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    private static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color color, FontStyles style)
    {
        var t = NewUI<TextMeshProUGUI>(name, parent);
        if (_font != null) t.font = _font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    private static Button MenuButton(Transform parent, string label, UnityAction onClick)
    {
        var image = NewUI<Image>(label + "Button", parent);
        image.sprite = _roundedSprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        ((RectTransform)image.transform).sizeDelta = new Vector2(460, 80);

        var element = image.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = 80;
        element.preferredWidth = 460;

        var button = image.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = ButtonHoverColor;
        colors.selectedColor = ButtonHoverColor;
        colors.pressedColor = AccentColor;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        UnityEventTools.AddPersistentListener(button.onClick, onClick);

        var text = Text("Label", image.transform, label, 40, Color.white, FontStyles.Bold);
        Stretch(text.rectTransform);
        return button;
    }

    private static RectTransform Panel(string name, Transform parent, string heading, Vector2 size)
    {
        var bg = NewUI<Image>(name, parent);
        bg.sprite = _roundedSprite;
        bg.type = Image.Type.Sliced;
        bg.color = PanelColor;
        var rt = bg.rectTransform;
        Place(rt, new Vector2(0.5f, 0.5f), Vector2.zero, size);

        var h = Text("Heading", rt, heading, 64, AccentColor, FontStyles.Bold);
        Place(h.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(size.x - 80, 90));
        return rt;
    }

    private static Button BackButton(RectTransform panel, UnityAction onClick)
    {
        Button back = MenuButton(panel, "Back", onClick);
        Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0, 70), new Vector2(300, 80));
        return back;
    }

    private static Slider SliderRow(RectTransform panel, string label, float y, float min, float max)
    {
        var text = Text(label + "Label", panel, label, 34, Color.white, FontStyles.Normal);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        Place(text.rectTransform, new Vector2(0.5f, 1f), new Vector2(-230, y), new Vector2(380, 60));

        var resources = new DefaultControls.Resources
        {
            standard = _roundedSprite,
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd")
        };
        GameObject go = DefaultControls.CreateSlider(resources);
        go.name = label + "Slider";
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(panel, false);
        Place((RectTransform)go.transform, new Vector2(0.5f, 1f), new Vector2(200, y), new Vector2(420, 30));

        var slider = go.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = max;

        // forest colours
        slider.fillRect.GetComponent<Image>().color = AccentColor;
        slider.handleRect.GetComponent<Image>().color = TitleColor;
        return slider;
    }

    // <pos=50%> starts the second column
    private const string TutorialText =
        "<b><color=#9EDC8C>Your goal</color></b>\n" +
        "Protect the forest! Stop the tree cutters, help the people you meet " +
        "with their quests, earn coins, buy seeds and plant new trees.\n\n" +
        "<b><color=#9EDC8C>Controls</color></b>\n" +
        "<b>W A S D</b>  –  move<pos=50%><b>Mouse</b>  –  look around\n" +
        "<b>Shift</b>  –  run<pos=50%><b>Ctrl</b>  –  crouch\n" +
        "<b>Space</b>  –  jump<pos=50%><b>Left click</b>  –  cast a fireball\n" +
        "<b>E</b>  –  talk / interact<pos=50%><b>F</b>  –  plant a seed\n" +
        "<b>I</b>  –  open the book<pos=50%><b>V</b>  –  first / third person\n" +
        "<b>Esc</b>  –  pause menu";
}
