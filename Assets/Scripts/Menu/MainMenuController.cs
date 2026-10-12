using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

// Hlavné menu: video v pozadí, Nová hra / Tutoriál / Nastavenia / Koniec.
// Nová hra spustí scénu Game, tutoriálová úroveň je TerrainDemoScene.
// Scénu postaví Tools > Menu > Build Main Menu Scene.
public class MainMenuController : MonoBehaviour
{
    public const string MasterVolumeKey = "MasterVolume";
    public const string MusicVolumeKey = "MusicVolume";          // číta ho aj GameMusicManager
    public const string MouseSensitivityKey = "MouseSensitivity"; // číta ho PlayerController

    [Header("Scenes")]
    public string gameSceneName = "Game";                 // Nová hra
    public string tutorialSceneName = "TerrainDemoScene"; // tlačidlo v paneli tutoriálu

    [Header("Panels")]
    public GameObject title;            // skrytý, keď je otvorený tutoriál/nastavenia
    public GameObject mainPanel;
    public GameObject tutorialPanel;
    public GameObject settingsPanel;

    [Header("Background video")]
    public VideoPlayer videoPlayer;
    public RawImage videoImage;

    [Header("Settings")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider mouseSensitivitySlider;
    public TextMeshProUGUI fullscreenLabel;

    [Header("Sounds (optional)")]
    public AudioSource uiAudioSource;
    public AudioClip clickSound;

    [Header("First selected buttons (keyboard / gamepad)")]
    public Button mainFirstButton;
    public Button tutorialFirstButton;
    public Button settingsFirstButton;

    private RenderTexture _videoTexture;

    private void Start()
    {
        // návrat z hry: odomkni myš a pusti čas
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SetUpVideo();
        LoadSettings();
        ShowPanel(mainPanel, mainFirstButton);
    }

    private void Update()
    {
        // Esc zatvorí tutoriál / nastavenia
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && !mainPanel.activeSelf)
            BackToMain();
    }

    private void OnDestroy()
    {
        if (_videoTexture != null) _videoTexture.Release();
    }

    // ------------------- BUTTONS -------------------

    public void NewGame()
    {
        PlayClick();
        PlayerPrefs.SetString("SavedScene", gameSceneName);
        PlayerPrefs.Save();
        SceneManager.LoadScene(gameSceneName);
    }

    public void PlayTutorial()
    {
        PlayClick();
        SceneManager.LoadScene(tutorialSceneName);
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
        videoPlayer.isLooping = true;
        videoPlayer.Play();
    }

    private void PlayClick()
    {
        if (uiAudioSource != null && clickSound != null)
            uiAudioSource.PlayOneShot(clickSound);
    }
}
