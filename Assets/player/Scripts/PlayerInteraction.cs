using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public float playerReach = 3.0f;          // predvolený dosah (ak objekt nemá vlastný)
    public float maxInteractDistance = 30f;   // najďalej, kam lúč vôbec pozerá
    public float aimAssistRadius = 0.25f;     // hrubší lúč – ľahšie sa trafí malý objekt z diaľky
    public Camera playerCamera;

    Interactable currentInteractable;

    void Awake()
    {
        // ak kamera nie je priradená, použi kameru hráča (alebo hlavnú kameru)
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>(true);
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    void Update()
    {
        // počas dialógu (a v snímke, keď sa zavrel) sa nedá interagovať
        QuestUI ui = QuestUI.Instance;
        if (ui != null && (ui.IsDialogueOpen || ui.LastDialogueClosedFrame == Time.frameCount))
        {
            DisableCurrentInteractable();
            return;
        }

        CheckInteraction();

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.eKey.wasPressedThisFrame && currentInteractable != null)
        {
            currentInteractable.Interact();
        }
    }

    void CheckInteraction()
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        float rayLength = Mathf.Max(playerReach, maxInteractDistance);
        RaycastHit hit;
        bool hitSomething = aimAssistRadius > 0f
            ? Physics.SphereCast(ray, aimAssistRadius, out hit, rayLength)
            : Physics.Raycast(ray, out hit, rayLength);

        if (hitSomething)
        {
            Interactable newInteractable = hit.collider.GetComponentInParent<Interactable>();
            float reach = newInteractable != null && newInteractable.interactDistance > 0f
                ? newInteractable.interactDistance
                : playerReach;

            if (newInteractable != null && newInteractable.enabled && hit.distance <= reach)
            {
                if (currentInteractable != null && newInteractable != currentInteractable)
                    currentInteractable.DisableOutline();

                SetNewCurrentInteractable(newInteractable);
                return;
            }
        }

        DisableCurrentInteractable();
    }

    void SetNewCurrentInteractable(Interactable newInteractable)
    {
        currentInteractable = newInteractable;
        currentInteractable.EnableOutline();

        if (HUDController.instance != null)
            HUDController.instance.EnableInteractionText(currentInteractable.message);
        else if (QuestUI.Instance != null)
            QuestUI.Instance.ShowPrompt(currentInteractable.message);
    }

    void DisableCurrentInteractable()
    {
        if (HUDController.instance != null)
            HUDController.instance.DisableInteractionText();
        if (QuestUI.Instance != null)
            QuestUI.Instance.HidePrompt();

        if (currentInteractable != null)
        {
            currentInteractable.DisableOutline();
            currentInteractable = null;
        }
    }
}
