using UnityEngine;

// Pomaly pulzujúci ružový obrys na questových objektoch, kým úloha nie je splnená.
// Keď sa na objekt hráč pozerá (dá sa interagovať), obrys svieti naplno.
[RequireComponent(typeof(Outline))]
public class QuestGlow : MonoBehaviour
{
    [Header("Pulzovanie")]
    public bool glowActive = true;
    public Color glowColor = new Color(1f, 0.41f, 0.78f);   // ružová
    [Tooltip("Dĺžka jedného bliknutia v sekundách")]
    public float pulsePeriod = 2f;
    [Range(0f, 1f)] public float minAlpha = 0.15f;
    public float minWidth = 2f;
    public float maxWidth = 6f;
    [Tooltip("Obrys viditeľný aj cez prekážky (pomáha objekty nájsť)")]
    public bool seeThroughWalls = false;

    [Header("Keď sa naň hráč pozerá")]
    public Color hoverColor = Color.white;
    public float hoverWidth = 7f;

    private Outline _outline;
    private Interactable _interactable;

    private void Awake()
    {
        _outline = GetComponent<Outline>();
        _interactable = GetComponent<Interactable>();
        _outline.OutlineMode = seeThroughWalls ? Outline.Mode.OutlineAll : Outline.Mode.OutlineVisible;
    }

    private void Update()
    {
        bool hovered = _interactable != null && _interactable.IsHovered;

        if (hovered)
        {
            SetOutlineEnabled(true);
            _outline.OutlineColor = hoverColor;
            _outline.OutlineWidth = hoverWidth;
        }
        else if (glowActive)
        {
            SetOutlineEnabled(true);
            float t = (Mathf.Sin(Time.time * 2f * Mathf.PI / Mathf.Max(0.1f, pulsePeriod)) + 1f) * 0.5f;
            Color c = glowColor;
            c.a = Mathf.Lerp(minAlpha, 1f, t);
            _outline.OutlineColor = c;
            _outline.OutlineWidth = Mathf.Lerp(minWidth, maxWidth, t);
        }
        else
        {
            SetOutlineEnabled(false);
        }
    }

    // Outline pri zapnutí/vypnutí prestavuje materiály, preto ho prepíname len pri zmene
    private void SetOutlineEnabled(bool value)
    {
        if (_outline.enabled != value)
            _outline.enabled = value;
    }
}
