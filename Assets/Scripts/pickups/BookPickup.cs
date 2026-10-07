using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BookPickup : MonoBehaviour
{
    public float pickupRange = 2f;
    public GameObject pressETipUI;
    public GameObject book;
    public Text infoText; // Optional UI text to show messages

    [SerializeField] private string itemId = "book";   // 🔹 ezt tettük hozzá

    private Transform player;
    private bool pickedUp = false;

    void Start()
    {
        if (pressETipUI != null)
            pressETipUI.SetActive(false);

        TryFindPlayer();

        // Optional: clear the info text at the start
        if (infoText != null)
            infoText.text = "";
    }

    void TryFindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            Debug.Log("Player FOUND!");
        }
    }

    void Update()
    {
        if (!pickedUp)
        {
            // If we still didn't find player, keep trying
            if (player == null)
            {
                TryFindPlayer();
                return;
            }

            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= pickupRange)
            {
                if (pressETipUI != null)
                    pressETipUI.SetActive(true);

                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    Pickup();
                }
            }
            else
            {
                if (pressETipUI != null)
                    pressETipUI.SetActive(false);
            }
        }

        // Handle book opening attempt
        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            if (!pickedUp)
            {
                if (infoText != null)
                {
                    infoText.text = "First find the green glowing book!";
                }
                Debug.Log("Player tried to open the book but hasn't picked it up yet.");
            }
            else
            {
                // Your existing book open logic can go here
                Debug.Log("Book opened!");
            }
        }
    }

    void Pickup()
    {
        pickedUp = true;

        if (pressETipUI != null)
            pressETipUI.SetActive(false);

        // 🔥 pickup anim
        if (player != null)
        {
            Animator anim = player.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetTrigger("Pickup");
                Debug.Log("Pickup animation triggered.");
            }
            else
            {
                Debug.LogError("NO Animator FOUND on player or its children!");
            }
        }

        // Firebase log
        if (FirebaseGameAnalytics.Instance != null && FirebaseGameAnalytics.Instance.IsReady)
        {
            FirebaseGameAnalytics.Instance.LogItemPickup(itemId);
            FirebaseGameAnalytics.Instance.LogGameplayEvent(
                "collect",
                transform.position,
                itemId    
            );

            Debug.Log("[BookPickup] Logged item pickup + collect: " + itemId);
        }
        else
        {
            Debug.LogWarning("[BookPickup] FirebaseGameAnalytics not ready, item not logged.");
        }

        // Activate the book you carry
        if (book != null)
            book.SetActive(true);
        else
            Debug.LogError("BOOK object not assigned!");

        // Notify UI Manager
        BookUIManager uiManager = FindObjectOfType<BookUIManager>();
        if (uiManager != null)
            uiManager.hasPickedUpBook = true;

        // Delay destruction so animation can trigger properly
        Destroy(gameObject, 0.1f);
    }
}
