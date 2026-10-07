using System;
using UnityEngine;

// Drží stav úloh (Lesník + Vyčisti potok) a loguje udalosti do Firebase pre Bartle analýzu.
[RequireComponent(typeof(QuestUI))]
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Názvy úloh")]
    public string talkQuestTitle = "Lesník: Porozprávaj sa s jeleňom pri vode";
    public string trashQuestTitle = "Vyčisti potok";
    public string trashCounterLabel = "Odpadky";

    public bool TalkedToDeer { get; private set; }
    public int TrashCollected { get; private set; }
    public int TrashTotal { get; private set; }
    public bool StreamCleaned => TrashTotal > 0 && TrashCollected >= TrashTotal;

    public event Action OnQuestsChanged;
    public event Action<string> OnQuestCompleted; // názov splnenej úlohy

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        // počet odpadkov sa zistí automaticky podľa TrashPickup objektov v scéne
        TrashTotal = FindObjectsByType<TrashPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
    }

    // ---------- Socializer: Lesník ----------

    public void LogDialogueStarted(string npcId, Vector3 pos, bool firstTime)
    {
        Log("npc_talk", pos, npcId, firstTime ? 1 : 0);
    }

    public void CompleteTalkQuest(string npcId, Vector3 pos)
    {
        if (TalkedToDeer) return;
        TalkedToDeer = true;

        Log("quest_complete", pos, "forest_keeper");
        OnQuestCompleted?.Invoke(talkQuestTitle);
        OnQuestsChanged?.Invoke();
    }

    // ---------- Achiever: Vyčisti potok ----------

    public void CollectTrash(string itemName, Vector3 pos)
    {
        if (StreamCleaned) return;
        TrashCollected++;

        Log("trash_collect", pos, itemName, TrashCollected);

        if (StreamCleaned)
        {
            Log("quest_complete", pos, "clean_stream", TrashTotal);
            OnQuestCompleted?.Invoke(trashQuestTitle);
        }
        OnQuestsChanged?.Invoke();
    }

    private static void Log(string eventName, Vector3 pos, string target, int value = 0)
    {
        if (FirebaseGameAnalytics.Instance != null)
            FirebaseGameAnalytics.Instance.LogGameplayEvent(eventName, pos, target, value);
    }
}
