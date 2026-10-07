using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MainMenuNavigation : MonoBehaviour
{
    [SerializeField] private Button[] buttons;
    private int currentIndex = 0;

    private Color normalTextColor = new Color(0.76f, 0.65f, 0.57f);
    private Color highlightedTextColor = Color.white;

    void OnEnable()
    {
        if (FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // Highlight initial button
        Highlight(currentIndex);

        // Add hover + click listeners to each button
        foreach (var button in buttons)
        {
            // Hover highlight
            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = button.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter
            };
            entry.callback.AddListener((data) => { SetHighlight(button); });
            trigger.triggers.Add(entry);

            // Click highlight
            button.onClick.AddListener(() => SetHighlight(button));
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentIndex = (currentIndex + 1) % buttons.Length;
            Highlight(currentIndex);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentIndex = (currentIndex - 1 + buttons.Length) % buttons.Length;
            Highlight(currentIndex);
        }

        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKeyDown(KeyCode.Space))
        {
            buttons[currentIndex].onClick.Invoke();
        }
    }

    public void SetHighlight(Button b)
    {
        int idx = System.Array.IndexOf(buttons, b);
        if (idx >= 0)
        {
            currentIndex = idx;
            Highlight(idx);
        }
    }

    private void Highlight(int index)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            TextMeshProUGUI txt = buttons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
                txt.color = normalTextColor;
        }

        TextMeshProUGUI highlightedTxt = buttons[index].GetComponentInChildren<TextMeshProUGUI>();
        if (highlightedTxt != null)
            highlightedTxt.color = highlightedTextColor;

        buttons[index].Select();
    }
}
