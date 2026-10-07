using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    public float playerReach = 3.0f;
    public Camera playerCamera;

    Interactable currentInteractable;

    void Update()
    {
        CheckInteraction();

        if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null)
        {
            // Make sure the object still exists
            if (currentInteractable.gameObject != null)
            {
                currentInteractable.Interact();
            }
            else
            {
                currentInteractable = null;
            }
        }
    }

    void CheckInteraction()
    {
        RaycastHit hit;
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out hit, playerReach))
        {
            if (hit.collider.CompareTag("Interactable")) 
            {
                Interactable newInteractable = hit.collider.GetComponent<Interactable>();

                if (newInteractable == null) return;

                if (currentInteractable != null && newInteractable != currentInteractable)
                {
                    currentInteractable.DisableOutline();
                }

                if (newInteractable.enabled)
                {
                    SetNewCurrentInteractable(newInteractable);
                }
                else
                {
                    DisableCurrentInteractable();
                }
            }
            else
            {
                DisableCurrentInteractable();
            }
        }
        else
        {
            DisableCurrentInteractable();
        }
    }

    void SetNewCurrentInteractable(Interactable newInteractable)
    {
        currentInteractable = newInteractable;

        if (currentInteractable != null)
        {
            currentInteractable.EnableOutline();
            if (HUDController.instance != null)
                HUDController.instance.EnableInteractionText(currentInteractable.message);
        }
    }

    void DisableCurrentInteractable()
    {
        if (HUDController.instance != null)
            HUDController.instance.DisableInteractionText();

        if (currentInteractable != null)
        {
            currentInteractable.DisableOutline();
            currentInteractable = null;
        }
    }
}
