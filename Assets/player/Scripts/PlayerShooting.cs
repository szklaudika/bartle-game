using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityTutorial.PlayerControl;

public class PlayerShooting : MonoBehaviour
{
    public Camera cam;
    public GameObject bulletPrefab;
    public Transform shootPoint;
    public Animator playerAnimator;  // Reference to PlayerController's Animator

    [Header("Shooting Settings")]
    public float fireCooldown = 2f;  // Total cooldown between shots
    private bool canShoot = true;
    private PlayerController playerController;

    [Header("Animation Timing")]
    public string fireballTriggerName = "Fireball"; // Animator trigger
    public float fireballSpawnDelay = 1.2f;         // Delay before fireball spawns (seconds)

    [Header("Audio")]
    public AudioSource audioSource;      // Existing AudioSource on the player
    public AudioClip shootSound;         // Fireball casting sound
    public float shootVolume = 1f;

    void Start()
    {
        playerController = GetComponent<PlayerController>();

        // Ensure animator exists
        if (playerAnimator == null)
        {
            playerAnimator = GetComponentInChildren<Animator>();
            if (playerAnimator == null)
                Debug.LogWarning("Player Animator not found on this GameObject or its children!");
        }

        // Auto-grab existing AudioSource on player
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            Debug.LogWarning("No AudioSource found on the Player!");
    }

    void Update()
    {
        // v otvorenom obchode ľavé tlačidlo nakupuje, nestrieľa; ani keď hráčka stojí (dialóg, okno s výberom)
        bool frozen = playerController != null && !playerController.canMove;
        if (Mouse.current.leftButton.wasPressedThisFrame && canShoot && !EconomyUI.IsUsingMouse && !frozen)
        {
            StartCoroutine(ShootFireballRoutine());
        }
    }

    private IEnumerator ShootFireballRoutine()
    {
        canShoot = false;
        if (playerController != null)
            playerController.SetCasting(true);

        // 🔊 Play sound IMMEDIATELY when the attack begins
        if (audioSource != null && shootSound != null)
            audioSource.PlayOneShot(shootSound, shootVolume);

        // Trigger the fireball animation
        if (playerAnimator != null)
            playerAnimator.SetTrigger(fireballTriggerName);

        // Wait for the animation to reach the spawn frame
        yield return new WaitForSeconds(fireballSpawnDelay);

        // Spawn fireball
        Shoot();

        // Wait for remaining cooldown
        yield return new WaitForSeconds(fireCooldown - fireballSpawnDelay);

        if (playerController != null)
            playerController.SetCasting(false);
        canShoot = true;
    }

    private void OnDisable()
    {
        // the coroutine stops with the component – don't leave the camera in casting mode
        canShoot = true;
        if (playerController != null)
            playerController.SetCasting(false);
    }

    private void Shoot()
    {
        // Ray from the center of the screen
        Ray ray = cam.ScreenPointToRay(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));

        // shootPoint rides on the camera; in third person move it back to the first-person spot
        Vector3 spawnPos = shootPoint.position;
        if (playerController != null)
        {
            ray.origin += ray.direction * playerController.CameraPullback;
            spawnPos -= playerController.CameraOffset;
        }

        Vector3 targetPoint = ray.GetPoint(100f);
        RaycastHit hit = default;

        // nearest hit that isn't the player herself (her collider can block the camera ray)
        float nearest = float.MaxValue;
        foreach (RaycastHit h in Physics.RaycastAll(ray, 100f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.transform.IsChildOf(transform) || h.distance >= nearest) continue;
            nearest = h.distance;
            hit = h;
            targetPoint = h.point;
        }

        Vector3 dir = (targetPoint - spawnPos).normalized;
        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

        Instantiate(bulletPrefab, spawnPos, rot);

        // Log shooting 
        if (FirebaseGameAnalytics.Instance != null && FirebaseGameAnalytics.Instance.IsReady)
        {
            FirebaseGameAnalytics.Instance.LogGameplayEvent(
                "shoot",
                transform.position,
                hit.transform != null ? hit.transform.name : "None",
                0
            );

        }
    }
}
