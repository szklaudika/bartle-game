using UnityEngine;
using UnityEngine.Events;
//using UnityTutorial.Logging;
using UnityTutorial.PlayerControl;

[RequireComponent(typeof(Outline))]
public class Interactable : MonoBehaviour
{
    private Outline outline;

    [Header("Unlock Settings")]
    public string message;                  // Správa, ktorú môžeš vypísať pri interakcii
    public string ingredientNameToUnlock;   // Názov ingrediencie, ktorá sa má odomknúť
    public BookIngredientManager bookIngredientManager; // Reference na BookIngredientManager (priradené cez Inspector alebo automaticky)

    public UnityEvent onInteraction;        // Event, ktorý sa spustí pri interakcii (napr. animácia pickup)

    private void OnEnable()
    {
        // Inicializácia Outline a BookIngredientManager
        if (outline == null)
            outline = GetComponent<Outline>();

        DisableOutline();

        if (bookIngredientManager == null)
        {
            // Skús nájsť BookIngredientManager v scéne
            bookIngredientManager = FindObjectOfType<BookIngredientManager>();
            if (bookIngredientManager == null)
                Debug.LogWarning("BookIngredientManager NOT FOUND IN SCENE. Prirad ho cez Inspector alebo aktivuj panel.");
        }
    }

    public void Interact()
{
    Debug.Log($"INTERACT called on: {gameObject.name}");

    // Display message if set
    if (!string.IsNullOrEmpty(message))
        Debug.Log("Message: " + message);

    // Unlock ingredient if applicable
    if (bookIngredientManager != null && !string.IsNullOrEmpty(ingredientNameToUnlock))
    {
        bookIngredientManager.UnlockIngredient(ingredientNameToUnlock);
        Debug.Log($"INTERACTION → Unlocking ingredient: {ingredientNameToUnlock}");
    }
    else if (string.IsNullOrEmpty(ingredientNameToUnlock))
    {
        Debug.LogWarning("ingredientNameToUnlock is empty!");
    }

    // Play pickup animation on the player
    GameObject player = GameObject.FindGameObjectWithTag("Player");
    if (player != null)
    {
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.GetComponentInChildren<Animator>()?.SetTrigger("Pickup");
            Debug.Log("Pickup animation triggered on player!");
        }
        else
        {
            Debug.LogError("PlayerController not found on Player!");
        }
    }
    else
    {
        Debug.LogError("Player object not found in scene!");
    }

    // Trigger any additional events (e.g., sound, effects)
    onInteraction.Invoke();

    // Enable outline briefly for visual feedback
    EnableOutline();
}

    public void DisableOutline()
    {
        if (outline != null)
            outline.enabled = false;
    }

    public void EnableOutline()
    {
        if (outline != null)
            outline.enabled = true;
    }
}
