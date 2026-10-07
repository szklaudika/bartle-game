using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerShooting : MonoBehaviour
{
    public Camera cam;
    public GameObject bulletPrefab;
    public Transform shootPoint;
    public Animator playerAnimator;  // Reference to PlayerController's Animator

    [Header("Shooting Settings")]
    public float fireCooldown = 2f;  // Total cooldown between shots
    private bool canShoot = true;

    [Header("Animation Timing")]
    public string fireballTriggerName = "Fireball"; // Animator trigger
    public float fireballSpawnDelay = 1.2f;         // Delay before fireball spawns (seconds)

    [Header("Audio")]
    public AudioSource audioSource;      // Existing AudioSource on the player
    public AudioClip shootSound;         // Fireball casting sound
    public float shootVolume = 1f;

    void Start()
    {
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
        if (Mouse.current.leftButton.wasPressedThisFrame && canShoot)
        {
            StartCoroutine(ShootFireballRoutine());
        }
    }

    private IEnumerator ShootFireballRoutine()
    {
        canShoot = false;

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

        canShoot = true;
    }

    private void Shoot()
    {
        // Ray from the center of the screen
        Ray ray = cam.ScreenPointToRay(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            targetPoint = hit.point;
        else
            targetPoint = ray.GetPoint(100f);

        Vector3 dir = (targetPoint - shootPoint.position).normalized;
        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

        Instantiate(bulletPrefab, shootPoint.position, rot);

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
