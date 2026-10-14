using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Farba textu tlačidla podľa stavu: zelený text na tmavom tlačidle,
// svetlý text, keď je nad ním myš alebo je vybrané klávesnicou.
[RequireComponent(typeof(Button))]
public class MenuButtonLabel : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public Color normalColor = new Color(0.45f, 0.85f, 0.45f);
    public Color activeColor = new Color(0.92f, 1f, 0.9f);

    private TextMeshProUGUI _label;
    private bool _hovered;
    private bool _selected;

    private void Awake() => _label = GetComponentInChildren<TextMeshProUGUI>(true);

    private void OnEnable()
    {
        // panel sa mohol skryť, kým bola myš nad tlačidlom
        _hovered = false;
        _selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        Refresh();
    }

    public void OnPointerEnter(PointerEventData eventData) { _hovered = true; Refresh(); }
    public void OnPointerExit(PointerEventData eventData) { _hovered = false; Refresh(); }
    public void OnSelect(BaseEventData eventData) { _selected = true; Refresh(); }
    public void OnDeselect(BaseEventData eventData) { _selected = false; Refresh(); }

    public void Refresh()
    {
        if (_label != null)
            _label.color = _hovered || _selected ? activeColor : normalColor;
    }
}
