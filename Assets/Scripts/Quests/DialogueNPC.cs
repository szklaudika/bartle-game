using UnityEngine;

// NPC, s ktorým sa dá porozprávať cez Interactable (E). Prvý rozhovor splní úlohu "Lesník".
[RequireComponent(typeof(Interactable))]
public class DialogueNPC : MonoBehaviour
{
    public string npcId = "deer";
    public string speakerName = "Jeleň";
    public bool completesTalkQuest = true;

    [TextArea(2, 4)]
    public string[] firstLines =
    {
        "Konečne niekto prišiel! Niekto ničí náš les – stromy chradnú a do potoka niekto hádže odpadky.",
        "Zvieratá odtiaľto odchádzajú a voda je každým dňom špinavšia.",
        "Prosím, pomôž nám zachrániť les!"
    };

    [TextArea(2, 4)]
    public string[] repeatLines =
    {
        "Ďakujem, že nám pomáhaš. Pozri sa k potoku – voda je plná odpadkov."
    };

    private Interactable _interactable;
    private QuestGlow _glow;
    private bool _talkedOnce;

    private void Awake()
    {
        _interactable = GetComponent<Interactable>();
        // ružový pulzujúci obrys, kým sa s NPC hráč neporozpráva
        if (!TryGetComponent(out _glow))
            _glow = gameObject.AddComponent<QuestGlow>();
        _glow.glowActive = completesTalkQuest;
        _interactable.playPickupAnimation = false;
        if (string.IsNullOrEmpty(_interactable.message))
            _interactable.message = $"Porozprávaj sa ({speakerName})";
    }

    private void OnEnable() => _interactable.onInteraction.AddListener(Talk);
    private void OnDisable() => _interactable.onInteraction.RemoveListener(Talk);

    private void Talk()
    {
        var ui = QuestUI.Instance;
        if (ui == null)
        {
            Debug.LogWarning("DialogueNPC: v scéne chýba QuestManager/QuestUI.");
            return;
        }
        if (ui.IsDialogueOpen) return;

        bool firstTime = !_talkedOnce;
        QuestManager.Instance?.LogDialogueStarted(npcId, transform.position, firstTime);

        ui.ShowDialogue(speakerName, firstTime ? firstLines : repeatLines, () =>
        {
            _talkedOnce = true;
            _glow.glowActive = false;
            if (firstTime && completesTalkQuest)
                QuestManager.Instance?.CompleteTalkQuest(npcId, transform.position);
        });
    }
}
