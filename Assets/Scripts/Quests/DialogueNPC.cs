using UnityEngine;

// NPC, s ktorým sa dá porozprávať cez Interactable (E).
// Ak dáva úlohy (completesTalkQuest), čo povie, závisí od aktuálnej úlohy v QuestManageri:
// prvý rozhovor splní "Lesník", druhý (po vyčistení potoka) pošle hráča za drevorubačom.
[RequireComponent(typeof(Interactable))]
public class DialogueNPC : MonoBehaviour
{
    public string npcId = "deer";
    public string speakerName = "Jeleň";
    public bool completesTalkQuest = true;

    [Header("1. Lesník – prvý rozhovor")]
    [TextArea(2, 4)]
    public string[] firstLines =
    {
        "Konečne niekto prišiel! Niekto ničí náš les – stromy chradnú a do potoka niekto hádže odpadky.",
        "Zvieratá odtiaľto odchádzajú a voda je každým dňom špinavšia.",
        "Prosím, pomôž nám zachrániť les!"
    };

    [Header("2. Kým hráč čistí potok")]
    [TextArea(2, 4)]
    public string[] repeatLines =
    {
        "Ďakujem, že nám pomáhaš. Pozri sa k potoku – voda je plná odpadkov."
    };

    [Header("3. Po vyčistení potoka – stromy miznú")]
    [TextArea(2, 4)]
    public string[] treesGoneLines =
    {
        "Ďakujem! Potok je zase čistý a voda konečne tečie priezračná.",
        "Ale pozri sa okolo seba – stromy miznú jeden za druhým!",
        "V lese je drevorubač s motorovou pílou a zrezáva ich všetky. Počuješ ju?",
        "Prosím, zastav ho, kým z nášho lesa nič neostane!"
    };

    [Header("4. Kým drevorubač žije")]
    [TextArea(2, 4)]
    public string[] lumberjackLines =
    {
        "Drevorubač je stále v lese. Choď za zvukom píly a zastav ho!"
    };

    [Header("5. Obnov les")]
    [TextArea(2, 4)]
    public string[] plantLines =
    {
        "Drevorubač je preč, ďakujem ti! Ale les je plný pňov.",
        "Otvor obchod (B), kúp semienka a zasaď nové stromy (F) na rovnú zem."
    };

    [Header("Po splnení všetkého")]
    [TextArea(2, 4)]
    public string[] doneLines =
    {
        "Les sa zase zelená. Zvieratá sa vracajú domov – ďakujeme ti!",
        "Si pripravený/á na väčší les? Za hranicami tohto údolia ťa čaká otvorený svet."
    };

    private Interactable _interactable;
    private QuestGlow _glow;
    private bool _talkedOnce;

    private void Awake()
    {
        _interactable = GetComponent<Interactable>();
        // ružový pulzujúci obrys, keď má hráč za NPC prísť
        if (!TryGetComponent(out _glow))
            _glow = gameObject.AddComponent<QuestGlow>();
        _glow.glowActive = completesTalkQuest;
        _interactable.playPickupAnimation = false;
        if (string.IsNullOrEmpty(_interactable.message))
            _interactable.message = $"Porozprávaj sa ({speakerName})";
    }

    private void OnEnable() => _interactable.onInteraction.AddListener(Talk);
    private void OnDisable() => _interactable.onInteraction.RemoveListener(Talk);

    private void Start()
    {
        if (completesTalkQuest && QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestsChanged += RefreshGlow;
            RefreshGlow();
        }
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestsChanged -= RefreshGlow;
    }

    private void RefreshGlow()
    {
        var stage = QuestManager.Instance.CurrentStage;
        _glow.glowActive = stage == QuestManager.Stage.TalkToDeer || stage == QuestManager.Stage.ReturnToDeer;
    }

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

        string[] lines = CurrentLines(out bool completesStage);
        ui.ShowDialogue(speakerName, lines, () =>
        {
            _talkedOnce = true;
            if (completesStage)
                QuestManager.Instance?.CompleteTalkQuest(npcId, transform.position);
            else if (completesTalkQuest && QuestManager.Instance != null &&
                     QuestManager.Instance.CurrentStage == QuestManager.Stage.Done && TutorialCompleteUI.Instance != null)
                TutorialCompleteUI.Instance.Show(); // ponuka otvoreného sveta znova
            else if (!completesTalkQuest)
                _glow.glowActive = false;
        });
    }

    private string[] CurrentLines(out bool completesStage)
    {
        completesStage = false;
        var qm = QuestManager.Instance;
        if (!completesTalkQuest || qm == null)
            return _talkedOnce ? repeatLines : firstLines;

        switch (qm.CurrentStage)
        {
            case QuestManager.Stage.TalkToDeer:
                completesStage = true;
                return firstLines;
            case QuestManager.Stage.CleanStream:
                return repeatLines;
            case QuestManager.Stage.ReturnToDeer:
                completesStage = true;
                return treesGoneLines;
            case QuestManager.Stage.KillLumberjack:
                return lumberjackLines;
            case QuestManager.Stage.PlantTrees:
                return plantLines;
            default:
                return doneLines;
        }
    }
}
