using System;
using UnityEngine;

// Peniaze a semienka hráča. Peniaze sa získavajú za porazených drevorubačov,
// minú sa v obchode (EconomyUI) na semienka, ktoré sa sadia cez TreePlanter.
// Vytvára ho PlayerHealth pri štarte hry.
[RequireComponent(typeof(EconomyUI), typeof(TreePlanter))]
public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance { get; private set; }

    [Header("Ceny")]
    public int startMoney = 0;
    public int seedPrice = 10;
    public int seedPackSize = 5;
    public int seedPackPrice = 45;

    public int Money { get; private set; }
    public int Seeds { get; private set; }
    public PlayerHealth Player { get; private set; }

    public event Action<int> MoneyChanged;   // zmena (+ zárobok, − nákup)
    public event Action<int> SeedsChanged;

    private void Awake()
    {
        Instance = this;
        Money = startMoney;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Init(PlayerHealth player) => Player = player;

    public void AddMoney(int amount, Vector3 pos, string source)
    {
        if (amount <= 0) return;
        Money += amount;
        MoneyChanged?.Invoke(amount);
        Log("money_earn", pos, source, amount);
    }

    public bool TryBuySeeds(int count, int price)
    {
        if (Money < price) return false;

        Money -= price;
        Seeds += count;
        MoneyChanged?.Invoke(-price);
        SeedsChanged?.Invoke(count);
        Log("seed_buy", PlayerPosition, "seed", count);
        return true;
    }

    public bool TryUseSeed()
    {
        if (Seeds <= 0) return false;
        Seeds--;
        SeedsChanged?.Invoke(-1);
        return true;
    }

    private Vector3 PlayerPosition => Player != null ? Player.transform.position : Vector3.zero;

    public static void Log(string eventName, Vector3 pos, string target, int value = 0)
    {
        if (FirebaseGameAnalytics.Instance != null && FirebaseGameAnalytics.Instance.IsReady)
            FirebaseGameAnalytics.Instance.LogGameplayEvent(eventName, pos, target, value);
    }
}
