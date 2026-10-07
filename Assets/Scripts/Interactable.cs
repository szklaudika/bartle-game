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
    public bool playPickupAnimation = true; // vypni napr. pri rozhovore s NPC
    [Tooltip("Z akej vzdialenosti sa dá s objektom interagovať. 0 = použije sa Player Reach z PlayerInteraction.")]
    public float interactDistance = 0f;

    public bool IsHovered { get; private set; } // hráč sa na objekt práve pozerá
    private QuestGlow questGlow;                // ak existuje, obrys riadi QuestGlow
    private QuestGlow Glow => questGlow != null ? questGlow : (questGlow = GetComponent<QuestGlow>());

    private void OnEnable()
    {
        // Inicializácia Outline a BookIngredientManager
        if (outline == null)
            outline = GetComponent<Outline>();

        DisableOutline();

        if (bookIngredientManager == null && !string.IsNullOrEmpty(ingredientNameToUnlock))
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

    // Play pickup animation on the player
    if (playPickupAnimation)
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
            pc.GetComponentInChildren<Animator>()?.SetTrigger("Pickup");
        else
            Debug.LogError("PlayerController not found in scene!");
    }

    // Trigger any additional events (e.g., sound, effects)
    onInteraction.Invoke();

    // Enable outline briefly for visual feedback
    EnableOutline();
}

    public void DisableOutline()
    {
        IsHovered = false;
        if (Glow == null && outline != null)
            outline.enabled = false;
    }

    public void EnableOutline()
    {
        IsHovered = true;
        if (Glow == null && outline != null)
            outline.enabled = true;
    }
}
