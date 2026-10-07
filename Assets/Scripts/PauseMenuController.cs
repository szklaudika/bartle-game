using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityTutorial.PlayerControl;   // needed for PlayerController

public class PauseMenuController : MonoBehaviour
{
    [Header("Video Background")]
    public VideoPlayer pauseBackgroundVideo;      // video on the pause panel

    [Header("Player Control")]
    public PlayerController playerController;     // needed to disable camera/movement

    [Header("References")]
    public GameObject menuRoot; // PausePanel
    public IntroSequenceController introController;

    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.P;
    public bool pauseAudioListener = true; // <- no longer used in SetPaused, can delete if you want

    public static bool IsPaused { get; set; }

    void Start()
    {
        if (menuRoot != null)
            menuRoot.SetActive(false);

        SetPaused(false, immediate: true);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            TogglePause();
    }

    public void TogglePause() => SetPaused(!IsPaused);

    public void ContinueGame() => SetPaused(false);

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void SetPaused(bool pause, bool immediate = false)
    {
        IsPaused = pause;

        if (menuRoot != null)
            menuRoot.SetActive(pause);

        // Freeze gameplay, but NOT audio
        Time.timeScale = pause ? 0f : 1f;

        // 🔇 REMOVED: this was muting music & clicks
        // if (pauseAudioListener)
        //     AudioListener.pause = pause;

        Cursor.visible = pause;
        Cursor.lockState = pause ? CursorLockMode.None : CursorLockMode.Locked;

        // Freeze/unfreeze player camera & movement
        if (playerController != null)
            playerController.canMove = !pause;

        // Play or pause the background video
        if (pauseBackgroundVideo != null)
        {
            if (pause) pauseBackgroundVideo.Play();
            else pauseBackgroundVideo.Pause();
        }

        if (introController != null)
        {
            if (pause) introController.PauseIntro();
            else introController.ResumeIntro();
        }
    }

    void OnDestroy()
    {
        if (IsPaused) SetPaused(false, immediate: true);
    }

    // RETURN TO MAIN MENU
    public void ReturnToMainMenu()
    {
        PlayerPrefs.SetString("SavedScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();

        Time.timeScale = 1f;

        // Safety reset, fine to keep
        AudioListener.pause = false;
        IsPaused = false;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (menuRoot != null)
            menuRoot.SetActive(false);

        SceneManager.LoadScene("MainMenu");
    }
}
