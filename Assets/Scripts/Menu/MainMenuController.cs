using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

// Hlavné menu: video v pozadí, Nová hra / Tutoriál / Nastavenia / Koniec.
// Nová hra ponúkne otvorený svet (ForestDefender) alebo tutoriál s úlohami (TerrainDemoScene).
// Scénu postaví Tools > Menu > Build Main Menu Scene.
public class MainMenuController : MonoBehaviour
{
    public const string MasterVolumeKey = "MasterVolume";
    public const string MusicVolumeKey = "MusicVolume";          // číta ho aj GameMusicManager
    public const string MouseSensitivityKey = "MouseSensitivity"; // číta ho PlayerController

    [Header("Scenes")]
    public string gameSceneName = "ForestDefender";       // Nová hra → otvorený svet
    public string tutorialSceneName = "TerrainDemoScene"; // Nová hra → tutoriál (aj tlačidlo v paneli tutoriálu)

    [Header("Panels")]
    public GameObject title;            // skrytý, keď je otvorený tutoriál/nastavenia
    public GameObject mainPanel;
    public GameObject tutorialPanel;
    public GameObject settingsPanel;

    [Header("Background video")]
    public VideoPlayer videoPlayer;
    public RawImage videoImage;
    [Tooltip("Koľko z videa odstrihnúť vľavo aj vpravo (0.025 = 2.5 % šírky na každej strane)")]
    [Range(0f, 0.2f)] public float videoCropSides = 0.025f;
    [Tooltip("Koľko z videa odstrihnúť hore aj dole")]
    [Range(0f, 0.2f)] public float videoCropTopBottom = 0.01f;

    [Header("Buttons")]
    [Tooltip("Hrúbka zeleného rámu tlačidiel – menšie číslo = hrubší rám")]
    [Range(0.3f, 2f)] public float buttonBorderScale = 0.75f;
    public TMP_FontAsset buttonFont;     // gotické ManufacturingConsent SDF (Bloodlines UI) – nadpisy a tlačidlá
    public TMP_FontAsset textFont;       // MedievalSharp SDF (Bloodlines UI) – popisy v nastaveniach
    [Range(0.5f, 2f)] public float buttonFontScale = 1.15f;

    [Header("Settings")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider mouseSensitivitySlider;
    public TextMeshProUGUI fullscreenLabel;

    [Header("Music")]
    public AudioClip menuMusic;          // Assets/menu/syouki_takahashi-midnight-forest-184304.mp3 (Pixabay)
    public float musicFadeInTime = 3f;

    [Header("Sounds (optional)")]
    public AudioSource uiAudioSource;
    public AudioClip clickSound;

    [Header("First selected buttons (keyboard / gamepad)")]
    public Button mainFirstButton;
    public Button tutorialFirstButton;
    public Button settingsFirstButton;

    private RenderTexture _videoTexture;
    private GameObject _newGamePanel;     // výber otvorený svet / tutoriál (vytvára SettingsPanelBuilder)
    private Button _newGameFirstButton;
    private AudioSource _music;
    private float _musicVolume;
    private float _musicFade;

    private void Start()
    {
        // návrat z hry: odomkni myš a pusti čas
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SetUpVideo();
        StartMusic();
        LoadFonts();
        StyleButtons();
        if (settingsPanel != null)
            SettingsPanelBuilder.Build(this, (RectTransform)settingsPanel.transform, buttonFont, textFont);
        if (mainPanel != null)
            _newGamePanel = SettingsPanelBuilder.BuildNewGamePanel(this, mainPanel.transform.parent, buttonFont, textFont,
                                                                   out _newGameFirstButton);
        LoadSettings();
        ShowPanel(mainPanel, mainFirstButton);
    }

    private void Update()
    {
        // Esc zatvorí tutoriál / nastavenia
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && !mainPanel.activeSelf)
            BackToMain();

        // hudba sa pri štarte pomaly zosilní; posuvník Music volume ju mení hneď
        if (_music != null)
        {
            _musicFade = Mathf.MoveTowards(_musicFade, 1f, Time.unscaledDeltaTime / Mathf.Max(0.01f, musicFadeInTime));
            _music.volume = _musicVolume * _musicFade;
        }
    }

    private void StartMusic()
    {
        if (menuMusic == null) return;
        _musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.6f);
        _music = gameObject.AddComponent<AudioSource>();
        _music.clip = menuMusic;
        _music.loop = true;
        _music.playOnAwake = false;
        _music.spatialBlend = 0f; // 2D – rovnako hlasno všade
        _music.volume = 0f;
        _music.Play();
    }

    private void OnDestroy()
    {
        if (_videoTexture != null) _videoTexture.Release();
    }

    // ------------------- BUTTONS -------------------

    // Nová hra: okno s výberom – otvorený svet alebo tutoriál
    public void NewGame()
    {
        if (_newGamePanel == null)
        {
            StartOpenWorld();
            return;
        }
        PlayClick();
        ShowPanel(_newGamePanel, _newGameFirstButton);
    }

    public void StartOpenWorld() => LoadGameScene(gameSceneName);

    public void PlayTutorial() => LoadGameScene(tutorialSceneName);

    private void LoadGameScene(string sceneName)
    {
        PlayClick();
        PlayerPrefs.SetString("SavedScene", sceneName);
        PlayerPrefs.Save();
        SceneManager.LoadScene(sceneName);
    }

    public void OpenTutorial()
    {
        PlayClick();
        ShowPanel(tutorialPanel, tutorialFirstButton);
    }

    public void OpenSettings()
    {
        PlayClick();
        ShowPanel(settingsPanel, settingsFirstButton);
    }

    public void BackToMain()
    {
        PlayClick();
        PlayerPrefs.Save();
        ShowPanel(mainPanel, mainFirstButton);
    }

    public void QuitGame()
    {
        PlayClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ToggleFullscreen()
    {
        PlayClick();
        Screen.fullScreen = !Screen.fullScreen;
        // Screen.fullScreen sa zmení až v ďalšom snímku, preto text podľa novej hodnoty
        UpdateFullscreenLabel(!Screen.fullScreen);
    }

    // ------------------- SETTINGS -------------------

    private void LoadSettings()
    {
        Bind(masterVolumeSlider, MasterVolumeKey, 1f, v => AudioListener.volume = v);
        Bind(musicVolumeSlider, MusicVolumeKey, 0.6f, v =>
        {
            _musicVolume = v;
            if (GameMusicManager.instance != null) GameMusicManager.instance.SetVolume(v);
        });
        Bind(mouseSensitivitySlider, MouseSensitivityKey, 5f, null);
        AudioListener.volume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        UpdateFullscreenLabel(Screen.fullScreen);
    }

    private static void Bind(Slider slider, string key, float defaultValue, System.Action<float> apply)
    {
        if (slider == null) return;
        slider.SetValueWithoutNotify(PlayerPrefs.GetFloat(key, defaultValue));
        slider.onValueChanged.AddListener(v =>
        {
            PlayerPrefs.SetFloat(key, v);
            apply?.Invoke(v);
        });
    }

    private void UpdateFullscreenLabel(bool fullscreen)
    {
        if (fullscreenLabel != null)
            fullscreenLabel.text = fullscreen ? "Fullscreen: On" : "Fullscreen: Off";
    }

    // ------------------- HELPERS -------------------

    private void ShowPanel(GameObject panel, Button first)
    {
        mainPanel.SetActive(panel == mainPanel);
        tutorialPanel.SetActive(panel == tutorialPanel);
        settingsPanel.SetActive(panel == settingsPanel);
        if (_newGamePanel != null) _newGamePanel.SetActive(panel == _newGamePanel);
        if (title != null) title.SetActive(panel == mainPanel);

        if (first != null) first.Select();
    }

    private void SetUpVideo()
    {
        if (videoPlayer == null || videoImage == null) return;

        VideoClip clip = videoPlayer.clip;
        int width = clip != null ? (int)clip.width : 1920;
        int height = clip != null ? (int)clip.height : 1080;
        _videoTexture = new RenderTexture(width, height, 0);
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = _videoTexture;
        videoImage.texture = _videoTexture;

        // video má po stranách čierne pruhy priamo v zázname – odstrihni okraje
        float cx = Mathf.Clamp(videoCropSides, 0f, 0.2f);
        float cy = Mathf.Clamp(videoCropTopBottom, 0f, 0.2f);
        videoImage.uvRect = new Rect(cx, cy, 1f - 2f * cx, 1f - 2f * cy);
        if (videoImage.TryGetComponent(out AspectRatioFitter fitter))
            fitter.aspectRatio = width * (1f - 2f * cx) / (height * (1f - 2f * cy));

        videoPlayer.isLooping = true;
        videoPlayer.Play();
    }

    // písma z Bloodlines UI – kópie v Assets/Resources/UI/Fonts, aby nezáviseli od odkazu v scéne
    private void LoadFonts()
    {
        if (buttonFont == null) buttonFont = Resources.Load<TMP_FontAsset>("UI/Fonts/ManufacturingConsent SDF");
        if (textFont == null) textFont = Resources.Load<TMP_FontAsset>("UI/Fonts/MedievalSharp SDF");
    }

    // tlačidlá v štýle Bloodlines UI (zelená verzia, Assets/Resources/UI/Buttons):
    // tmavé so zeleným rámom, pri prejdení myšou / výbere vyplnené zelenou
    private void StyleButtons()
    {
        Sprite normal = Resources.Load<Sprite>("UI/Buttons/Button_Normal");
        Sprite active = Resources.Load<Sprite>("UI/Buttons/Button_Active");
        Sprite disabled = Resources.Load<Sprite>("UI/Buttons/Button_Disabled");
        if (normal == null || active == null) return; // obrázky chýbajú – ostane pôvodný vzhľad

        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button.targetGraphic is not Image image) continue;

            image.sprite = normal;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = buttonBorderScale;
            image.color = Color.white;

            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = active,
                selectedSprite = active,
                pressedSprite = active,
                disabledSprite = disabled != null ? disabled : normal
            };

            // gotické písmo z Bloodlines UI; farba textu sa mení so stavom tlačidla
            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null && buttonFont != null)
            {
                label.font = buttonFont;
                label.fontStyle = FontStyles.Normal;
                label.fontSize *= buttonFontScale;
            }
            if (!button.TryGetComponent(out MenuButtonLabel _))
                button.gameObject.AddComponent<MenuButtonLabel>();
        }
    }

    private void PlayClick()
    {
        if (uiAudioSource != null && clickSound != null)
            uiAudioSource.PlayOneShot(clickSound);
    }
}
