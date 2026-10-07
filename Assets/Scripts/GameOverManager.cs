using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;

    [Header("UI Elements")]
    public GameObject gameOverPanel;
    public float fadeDuration = 1f;

    private Image panelImage;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true); // temporarily enable to get Image
            panelImage = gameOverPanel.GetComponent<Image>();
            if (panelImage == null)
                Debug.LogError("GameOver panel needs an Image component for fading!");

            // Start fully transparent
            panelImage.color = new Color(panelImage.color.r, panelImage.color.g, panelImage.color.b, 0f);
            gameOverPanel.SetActive(false);
        }
    }

    public void ShowGameOver()
    {
        // End session
        if (FirebaseGameAnalytics.Instance != null && FirebaseGameAnalytics.Instance.IsReady)
        {
            FirebaseGameAnalytics.Instance.LogGameplayEvent("lose");
            FirebaseGameAnalytics.Instance.EndSession(true); 
            Debug.Log("[GameOverManager] Firebase session ended (gameOver=true).");
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            EnableUIControl(); // <<< important
            StartCoroutine(FadeInGameOver());
        }
    }

    private IEnumerator FadeInGameOver()
    {
        float timer = 0f;
        Color baseColor = panelImage.color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01(timer / fadeDuration);
            panelImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            yield return null;
        }

        panelImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
    }

    private void EnableUIControl()
    {
        // Cursor for clicking button
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Allow EventSystem to receive navigation/clicks
        EventSystem.current.sendNavigationEvents = true;
    }

    public void RestartGame()
    {
        // new run = new session
        if (FirebaseGameAnalytics.Instance != null && FirebaseGameAnalytics.Instance.IsReady)
        {
            FirebaseGameAnalytics.Instance.StartSession();
            Debug.Log("[GameOverManager] Firebase session started from RestartGame().");
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
