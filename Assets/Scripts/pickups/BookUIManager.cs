using UnityEngine;
using UnityEngine.EventSystems;
using UnityTutorial.PlayerControl;
using TMPro;
using System.Collections;

public class BookUIManager : MonoBehaviour
{
    [Header("References")]
    public GameObject bookUIPanel;
    public PlayerController playerController;
    public TMP_Text infoText; // TMP UI Text to show messages
    [HideInInspector]
    public bool hasPickedUpBook = false; // Set by BookPickup

    private bool isOpen = false;
    private Coroutine messageCoroutine;

    void Start()
    {
        if (infoText != null)
            infoText.gameObject.SetActive(false); // Hide it at start
    }

    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current.iKey.wasPressedThisFrame)
        {
            if (!hasPickedUpBook)
            {
                ShowInfoMessage("First find the green glowing book!");
                Debug.Log("Player tried to open the book but hasn't picked it up yet.");
                return;
            }

            ToggleBookUI();
        }
    }

    void ToggleBookUI()
    {
        isOpen = !isOpen;

        if (bookUIPanel != null)
            bookUIPanel.SetActive(isOpen);

        if (isOpen)
        {
            if (playerController != null)
                playerController.canMove = false;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            EventSystem.current.sendNavigationEvents = true;
        }
        else
        {
            if (playerController != null)
                playerController.canMove = true;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    // Display message temporarily
    void ShowInfoMessage(string message, float duration = 2f)
    {
        if (infoText == null) return;

        infoText.text = message;
        infoText.gameObject.SetActive(true);

        // Stop previous coroutine if running
        if (messageCoroutine != null)
            StopCoroutine(messageCoroutine);

        messageCoroutine = StartCoroutine(HideMessageAfterDelay(duration));
    }

    IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (infoText != null)
            infoText.gameObject.SetActive(false);
    }
}
