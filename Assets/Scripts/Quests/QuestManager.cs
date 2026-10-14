using System;
using UnityEngine;

// Úlohy idú za sebou: jeleň → potok → jeleň (stromy miznú) → drevorubač → obchod a sadenie.
// Drží stav úloh a loguje udalosti do Firebase pre Bartle analýzu.
[RequireComponent(typeof(QuestUI))]
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    public enum Stage { TalkToDeer, CleanStream, ReturnToDeer, KillLumberjack, PlantTrees, Done }

    [Header("Názvy úloh")]
    public string talkQuestTitle = "Lesník: Porozprávaj sa s jeleňom pri vode";
    public string trashQuestTitle = "Vyčisti potok";
    public string trashCounterLabel = "Odpadky";
    public string returnQuestTitle = "Vráť sa k jeleňovi";
    public string lumberjackQuestTitle = "Zastav drevorubača";
    public string plantQuestTitle = "Obnov les";
    public string openShopLabel = "Otvor obchod (B)";
    public string buySeedsLabel = "Kúp semienka";
    public string plantTreesLabel = "Zasaď stromy (F)";

    [Header("Obnov les")]
    public int treesToPlant = 3;
    [Tooltip("Peniaze od jeleňa za zastavenie drevorubača – aby hráč mal na semienka")]
    public int lumberjackQuestReward = 25;

    public Stage CurrentStage { get; private set; } = Stage.TalkToDeer;

    public int TrashCollected { get; private set; }
    public int TrashTotal { get; private set; }
    public bool StreamCleaned => TrashTotal > 0 && TrashCollected >= TrashTotal;

    public int LumberjacksKilled { get; private set; }
    public int LumberjacksTotal { get; private set; }

    public bool ShopOpened { get; private set; }
    public int SeedsBought { get; private set; }
    public int TreesPlanted { get; private set; }

    public event Action OnQuestsChanged;
    public event Action<string> OnQuestCompleted; // názov splnenej úlohy
    public event Action<string> OnQuestStarted;   // názov novej úlohy

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        // počty sa zistia automaticky podľa objektov v scéne
        TrashTotal = FindObjectsByType<TrashPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        LumberjacksTotal = FindObjectsByType<TreeCutterAI>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
    }

    public string StageTitle(Stage stage) => stage switch
    {
        Stage.TalkToDeer => talkQuestTitle,
        Stage.CleanStream => trashQuestTitle,
        Stage.ReturnToDeer => returnQuestTitle,
        Stage.KillLumberjack => lumberjackQuestTitle,
        Stage.PlantTrees => plantQuestTitle,
        _ => ""
    };

    // ---------- Socializer: rozhovory s jeleňom ----------

    public void LogDialogueStarted(string npcId, Vector3 pos, bool firstTime)
    {
        Log("npc_talk", pos, npcId, firstTime ? 1 : 0);
    }

    public void CompleteTalkQuest(string npcId, Vector3 pos)
    {
        if (CurrentStage == Stage.TalkToDeer)
        {
            Log("quest_complete", pos, "forest_keeper");
            Advance();
        }
        else if (CurrentStage == Stage.ReturnToDeer)
        {
            Log("quest_complete", pos, "deer_return");
            Advance();
        }
    }

    // ---------- Achiever: Vyčisti potok ----------

    public void CollectTrash(string itemName, Vector3 pos)
    {
        if (CurrentStage != Stage.CleanStream || StreamCleaned) return;
        TrashCollected++;

        Log("trash_collect", pos, itemName, TrashCollected);

        if (StreamCleaned)
        {
            Log("quest_complete", pos, "clean_stream", TrashTotal);
            Advance();
        }
        else
        {
            OnQuestsChanged?.Invoke();
        }
    }

    // ---------- Killer: Zastav drevorubača ----------

    public void LumberjackKilled(string enemyName, Vector3 pos)
    {
        // počíta sa aj drevorubač porazený skôr – úloha sa potom splní hneď, ako začne
        LumberjacksKilled++;
        if (CurrentStage == Stage.KillLumberjack)
            CheckLumberjackQuest(pos);
        OnQuestsChanged?.Invoke();
    }

    private void CheckLumberjackQuest(Vector3 pos)
    {
        if (LumberjacksKilled < LumberjacksTotal) return;

        Log("quest_complete", pos, "stop_lumberjack", LumberjacksKilled);
        if (PlayerWallet.Instance != null)
            PlayerWallet.Instance.AddMoney(lumberjackQuestReward, pos, "quest_reward");
        Advance();
    }

    // ---------- Obnov les: obchod, semienka, sadenie ----------

    public void NotifyShopOpened()
    {
        if (CurrentStage != Stage.PlantTrees || ShopOpened) return;
        ShopOpened = true;
        OnQuestsChanged?.Invoke();
    }

    public void NotifySeedsBought(int count)
    {
        if (CurrentStage != Stage.PlantTrees) return;
        SeedsBought += count;
        OnQuestsChanged?.Invoke();
    }

    public void NotifyTreePlanted(Vector3 pos)
    {
        if (CurrentStage != Stage.PlantTrees) return;
        TreesPlanted++;

        if (ShopOpened && SeedsBought > 0 && TreesPlanted >= treesToPlant)
        {
            Log("quest_complete", pos, "replant_forest", TreesPlanted);
            Advance();
        }
        else
        {
            OnQuestsChanged?.Invoke();
        }
    }

    // ---------- Priebeh ----------

    private void Advance()
    {
        OnQuestCompleted?.Invoke(StageTitle(CurrentStage));
        CurrentStage++;

        // drevorubač už mohol byť porazený skôr
        if (CurrentStage == Stage.KillLumberjack && LumberjacksKilled >= LumberjacksTotal)
        {
            OnQuestsChanged?.Invoke();
            CheckLumberjackQuest(Vector3.zero);
            return;
        }

        if (CurrentStage != Stage.Done)
            OnQuestStarted?.Invoke(StageTitle(CurrentStage));
        OnQuestsChanged?.Invoke();
    }

    private static void Log(string eventName, Vector3 pos, string target, int value = 0)
    {
        if (FirebaseGameAnalytics.Instance != null)
            FirebaseGameAnalytics.Instance.LogGameplayEvent(eventName, pos, target, value);
    }
}
