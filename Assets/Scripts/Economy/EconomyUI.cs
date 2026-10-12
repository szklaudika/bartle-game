using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// UI peňazí a semienok (vpravo hore), obchod so semienkami (kláves B) a hlášky pri sadení.
// Obchod sa ovláda len klávesnicou (1, 2, B/Esc), aby kliknutie nevystrelilo ohnivú guľu.
public class EconomyUI : MonoBehaviour
{
    [Header("Vzhľad")]
    public Color panelColor = new Color(0f, 0f, 0f, 0.6f);
    public Color moneyColor = new Color(1f, 0.84f, 0.3f);
    public Color disabledColor = new Color(1f, 1f, 1f, 0.35f);
    public float messageDuration = 2.5f;

    public bool IsShopOpen => _shopPanel != null && _shopPanel.activeSelf;

    private PlayerWallet _wallet;
    private TextMeshProUGUI _walletText;
    private TextMeshProUGUI _deltaText;
    private TextMeshProUGUI _promptText;
    private TextMeshProUGUI _messageText;
    private GameObject _shopPanel;
    private TextMeshProUGUI _offerSingle;
    private TextMeshProUGUI _offerPack;
    private TextMeshProUGUI _shopMessage;

    private float _deltaTimer;
    private float _messageTimer;

    private void Awake() => BuildUI();

    private void Start()
    {
        // pri AddComponent<PlayerWallet> vzniká tento komponent skôr než peňaženka – preto až v Start
        _wallet = GetComponent<PlayerWallet>();
        _wallet.MoneyChanged += OnMoneyChanged;
        _wallet.SeedsChanged += _ => Refresh();
        Refresh();
    }

    private void OnDestroy()
    {
        if (_wallet != null) _wallet.MoneyChanged -= OnMoneyChanged;
    }

    private void Update()
    {
        if (_wallet == null) return;
        HandleShopInput();

        // "+25" pri peniazoch – vystúpi nahor a zmizne
        if (_deltaTimer > 0f)
        {
            _deltaTimer -= Time.deltaTime;
            float t = 1f - _deltaTimer / 1.5f;
            ((RectTransform)_deltaText.transform).anchoredPosition = new Vector2(0, -12 + t * 20f);
            SetAlpha(_deltaText, 1f - t * t);
        }

        if (_messageTimer > 0f)
        {
            _messageTimer -= Time.deltaTime;
            SetAlpha(_messageText, Mathf.Clamp01(_messageTimer / 0.5f));
        }
    }

    // ---------- Obchod ----------

    private void HandleShopInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        PlayerHealth player = _wallet.Player;
        bool blocked = player == null || player.IsDead || PauseMenuController.IsPaused ||
                       (QuestUI.Instance != null && QuestUI.Instance.IsDialogueOpen);
        if (blocked)
        {
            if (IsShopOpen) _shopPanel.SetActive(false);
            return;
        }

        if (kb.bKey.wasPressedThisFrame || (IsShopOpen && kb.escapeKey.wasPressedThisFrame))
        {
            _shopPanel.SetActive(!IsShopOpen);
            _shopMessage.text = "";
            Refresh();
            return;
        }

        if (!IsShopOpen) return;

        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
            Buy(1, _wallet.seedPrice);
        else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
            Buy(_wallet.seedPackSize, _wallet.seedPackPrice);
    }

    private void Buy(int count, int price)
    {
        if (_wallet.TryBuySeeds(count, price))
            _shopMessage.text = $"<color=#{ColorUtility.ToHtmlStringRGB(moneyColor)}>Kúpené: {SeedWord(count)}</color>";
        else
            _shopMessage.text = "<color=#FF6B6B>Nemáš dosť peňazí – poraz drevorubača</color>";
    }

    // ---------- Verejné pre TreePlanter ----------

    public void SetPlantPrompt(string text)
    {
        bool show = !string.IsNullOrEmpty(text);
        if (_promptText.gameObject.activeSelf != show) _promptText.gameObject.SetActive(show);
        if (show) _promptText.text = text;
    }

    public void ShowMessage(string text)
    {
        _messageText.text = text;
        _messageTimer = messageDuration;
        SetAlpha(_messageText, 1f);
    }

    // ---------- Obnova textov ----------

    private void OnMoneyChanged(int delta)
    {
        _deltaText.text = delta > 0 ? $"+{delta}" : delta.ToString();
        _deltaText.color = delta > 0 ? moneyColor : new Color(1f, 0.42f, 0.42f);
        _deltaTimer = 1.5f;
        Refresh();
    }

    private void Refresh()
    {
        string gold = ColorUtility.ToHtmlStringRGB(moneyColor);
        _walletText.text =
            $"Peniaze: <color=#{gold}><b>{_wallet.Money}</b></color>\n" +
            $"Semienka: <b>{_wallet.Seeds}</b>\n" +
            "<size=20><color=#FFFFFF99>B – obchod   F – zasadiť</color></size>";

        _offerSingle.text = $"[1]  1 semienko – {_wallet.seedPrice} peňazí";
        _offerSingle.color = _wallet.Money >= _wallet.seedPrice ? Color.white : disabledColor;
        _offerPack.text = $"[2]  {SeedWord(_wallet.seedPackSize)} – {_wallet.seedPackPrice} peňazí";
        _offerPack.color = _wallet.Money >= _wallet.seedPackPrice ? Color.white : disabledColor;
    }

    private static string SeedWord(int n) =>
        n == 1 ? "1 semienko" : n is >= 2 and <= 4 ? $"{n} semienka" : $"{n} semienok";

    private static void SetAlpha(Graphic g, float a)
    {
        Color c = g.color;
        c.a = a;
        g.color = c;
    }

    // ---------- Stavba UI ----------

    private void BuildUI()
    {
        var canvasGo = new GameObject("EconomyCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 55; // nad úlohami, pod obrazovkou smrti (PlayerHUD má 60)
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasGo.transform;

        // peniaze a semienka – vpravo hore
        var wallet = NewRect("Wallet", root);
        SetAnchors(wallet, Vector2.one, Vector2.one, Vector2.one);
        wallet.anchoredPosition = new Vector2(-30, -30);
        wallet.sizeDelta = new Vector2(320, 120);
        AddImage(wallet, panelColor);
        var walletText = NewRect("Text", wallet);
        Stretch(walletText);
        walletText.offsetMin = new Vector2(20, 10);
        walletText.offsetMax = new Vector2(-20, -10);
        _walletText = AddText(walletText, 28, TextAlignmentOptions.TopLeft, Color.white);

        var delta = NewRect("Delta", wallet);
        SetAnchors(delta, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 1));
        delta.sizeDelta = new Vector2(-40, 40);
        _deltaText = AddText(delta, 32, TextAlignmentOptions.Right, moneyColor);
        _deltaText.fontStyle = FontStyles.Bold;
        _deltaText.outlineWidth = 0.2f;
        _deltaText.outlineColor = Color.black;
        SetAlpha(_deltaText, 0f);

        // nápoveda pri sadení – dole v strede (nad nápovedou úloh)
        var prompt = NewRect("PlantPrompt", root);
        SetAnchors(prompt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
        prompt.anchoredPosition = new Vector2(0, 330);
        prompt.sizeDelta = new Vector2(900, 50);
        _promptText = AddText(prompt, 30, TextAlignmentOptions.Center, Color.white);
        _promptText.outlineWidth = 0.2f;
        _promptText.outlineColor = Color.black;
        _promptText.gameObject.SetActive(false);

        var message = NewRect("Message", root);
        SetAnchors(message, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
        message.anchoredPosition = new Vector2(0, 385);
        message.sizeDelta = new Vector2(1100, 50);
        _messageText = AddText(message, 30, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.4f));
        _messageText.outlineWidth = 0.2f;
        _messageText.outlineColor = Color.black;
        SetAlpha(_messageText, 0f);

        // obchod – v strede obrazovky
        var shop = NewRect("Shop", root);
        SetAnchors(shop, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        shop.sizeDelta = new Vector2(620, 330);
        AddImage(shop, new Color(0f, 0f, 0f, 0.8f));

        var title = Row("Title", shop, -24, 50);
        var titleText = AddText(title, 38, TextAlignmentOptions.Center, moneyColor);
        titleText.fontStyle = FontStyles.Bold;
        titleText.text = "Obchod so semienkami";

        var subtitle = Row("Subtitle", shop, -76, 34);
        AddText(subtitle, 22, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.7f)).text =
            "Zasaď semienko a vyrastie z neho nový strom.";

        _offerSingle = AddText(Row("Offer1", shop, -130, 40), 30, TextAlignmentOptions.Left, Color.white);
        _offerPack = AddText(Row("Offer2", shop, -178, 40), 30, TextAlignmentOptions.Left, Color.white);
        _shopMessage = AddText(Row("ShopMessage", shop, -232, 36), 26, TextAlignmentOptions.Center, Color.white);

        var close = Row("Close", shop, -280, 30);
        AddText(close, 22, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.6f)).text = "B / Esc – zavrieť";

        _shopPanel = shop.gameObject;
        _shopPanel.SetActive(false);
    }

    // riadok cez celú šírku panelu, y je odsadenie od horného okraja
    private static RectTransform Row(string name, RectTransform parent, float y, float height)
    {
        var rt = NewRect(name, parent);
        SetAnchors(rt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
        rt.anchoredPosition = new Vector2(0, y);
        rt.sizeDelta = new Vector2(-80, height);
        return rt;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = pivot;
    }

    private static void Stretch(RectTransform rt)
    {
        SetAnchors(rt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void AddImage(RectTransform rt, Color color)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    private static TextMeshProUGUI AddText(RectTransform rt, float size, TextAlignmentOptions align, Color color)
    {
        var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.alignment = align;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }
}
