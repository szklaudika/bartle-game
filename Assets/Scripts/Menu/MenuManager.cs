using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Menu Sounds")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip clickSound;

    [Header("Background Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip backgroundMusic;

    [Header("Volume Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Continue Button")]
    [SerializeField] private Button continueButton;

    const string SAVE_KEY = "SavedScene";

    void Start()
    {
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        
        if (continueButton != null)
            continueButton.gameObject.SetActive(PlayerPrefs.HasKey(SAVE_KEY));

        float savedMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.6f);
        float savedSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);

        if (musicSlider != null)
        {
            musicSlider.value = savedMusicVolume;
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = savedSFXVolume;
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        if (musicSource != null) musicSource.volume = savedMusicVolume;
        if (uiAudioSource != null) uiAudioSource.volume = savedSFXVolume;

        PlayMenuMusic();
    }

    // ------------------- MENU ACTIONS -------------------

    public void StartGame()
    {
        PlayClickSound();
        PlayerPrefs.SetString(SAVE_KEY, "Scene"); // uloží scénu
        SceneManager.LoadScene("Scene");
    }

    public void ContinueGame()
    {
        PlayClickSound();

        // Firebase session start
        if (FirebaseGameAnalytics.Instance != null && FirebaseGameAnalytics.Instance.IsReady)
        {
            FirebaseGameAnalytics.Instance.StartSession();
            Debug.Log("[MenuManager] Firebase session started from menu.");
        }
        else
        {
            Debug.LogWarning("[MenuManager] FirebaseGameAnalytics not ready when starting game.");
        }

        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string sceneToLoad = PlayerPrefs.GetString(SAVE_KEY);
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    public void QuitGame()
    {
        PlayClickSound();
        Application.Quit();
    }

    public void OpenSettings()
    {
        PlayClickSound();
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        PlayClickSound();
        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    // ------------------- SOUND -------------------

    public void PlayClickSound()
    {
        if (uiAudioSource != null && clickSound != null)
            uiAudioSource.PlayOneShot(clickSound, sfxSlider != null ? sfxSlider.value : 1f);
    }

    private void PlayMenuMusic()
    {
        if (musicSource == null || backgroundMusic == null) return;

        musicSource.clip = backgroundMusic;
        musicSource.loop = true;
        musicSource.Play();
    }

    // ------------------- VOLUME -------------------

    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
            musicSource.volume = volume;

        PlayerPrefs.SetFloat("MusicVolume", volume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        if (uiAudioSource != null)
            uiAudioSource.volume = volume;

        PlayerPrefs.SetFloat("SFXVolume", volume);
        PlayerPrefs.Save();
    }
}
