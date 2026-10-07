using UnityEngine;

// Odpadok pri potoku – po stlačení E zmizne a pripočíta sa do úlohy "Vyčisti potok".
[RequireComponent(typeof(Interactable))]
public class TrashPickup : MonoBehaviour
{
    public AudioClip pickupSound;
    [Tooltip("Z akej vzdialenosti (v metroch) sa dá odpadok zdvihnúť.")]
    public float pickupDistance = 10f;

    private Interactable _interactable;
    private bool _collected;

    private void Awake()
    {
        _interactable = GetComponent<Interactable>();
        _interactable.interactDistance = pickupDistance;
        // ružový pulzujúci obrys, kým odpadok nie je zdvihnutý
        if (!TryGetComponent(out QuestGlow _))
            gameObject.AddComponent<QuestGlow>();
        if (string.IsNullOrEmpty(_interactable.message))
            _interactable.message = "Zdvihni odpadok";
    }

    private void OnEnable() => _interactable.onInteraction.AddListener(Collect);
    private void OnDisable() => _interactable.onInteraction.RemoveListener(Collect);

    private void Collect()
    {
        if (_collected) return;
        _collected = true;

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        QuestManager.Instance?.CollectTrash(name, transform.position);
        Destroy(gameObject);
    }
}
