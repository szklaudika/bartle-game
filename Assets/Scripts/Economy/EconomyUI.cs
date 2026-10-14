using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityTutorial.PlayerControl;

// UI peňazí a semienok (vpravo hore), obchod so semienkami (kláves B) a hlášky pri sadení.
// V obchode sa nakupuje myšou (kliknutím na ponuku) alebo klávesmi 1, 2; B/Esc ho zavrie.
public class EconomyUI : MonoBehaviour
{
    [Header("Vzhľad")]
    public Color panelColor = new Color(0f, 0f, 0f, 0.6f);
    public Color moneyColor = new Color(1f, 0.84f, 0.3f);
    public Color seedColor = new Color(0.45f, 0.85f, 0.45f);
    public float messageDuration = 2.5f;

    [Header("Obchod – otvorená kniha")]
    public Color coverColor = new Color(0.36f, 0.2f, 0.1f);
    public Color pageColor = new Color(0.96f, 0.91f, 0.78f);
    public Color pageBackColor = new Color(0.86f, 0.78f, 0.6f);   // rub zahnutého rohu
    public Color inkColor = new Color(0.27f, 0.16f, 0.08f);
    public Color goodInkColor = new Color(0.16f, 0.42f, 0.14f);
    public Color badInkColor = new Color(0.66f, 0.14f, 0.1f);
    public float pageFlipDuration = 0.35f;

    public bool IsShopOpen => _shopPanel != null && _shopPanel.activeSelf;

    // ľavé tlačidlo myši patrí obchodu (aj v snímke, keď sa zatvoril kliknutím) – PlayerShooting vtedy nestrieľa
    public static bool IsUsingMouse => Time.frameCount - _lastOpenFrame <= 1;
    private static int _lastOpenFrame = -10;

    private PlayerWallet _wallet;
    private TextMeshProUGUI _walletText;
    private TextMeshProUGUI _deltaText;
    private TextMeshProUGUI _promptText;
    private TextMeshProUGUI _messageText;
    private GameObject _shopPanel;
    private TextMeshProUGUI _offerSingle;
    private TextMeshProUGUI _offerPack;
    private TextMeshProUGUI _shopMessage;
    private TextMeshProUGUI _shopWallet;
    private RectTransform _flipPage;
    private Image _flipPageImage;

    private float _deltaTimer;
    private float _messageTimer;
    private float _flipTimer;

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

        AnimatePageFlip();
    }

    // pri otvorení obchodu sa pravá strana otočí cez chrbát knihy na ľavú
    private void AnimatePageFlip()
    {
        if (_flipTimer <= 0f) return;
        _flipTimer -= Time.unscaledDeltaTime;

        float t = 1f - Mathf.Clamp01(_flipTimer / pageFlipDuration);
        float eased = t * t * (3f - 2f * t);
        float scaleX = Mathf.Cos(eased * Mathf.PI); // 1 → -1: strana sa preklopí okolo chrbta
        _flipPage.localScale = new Vector3(scaleX, 1f, 1f);
        // v polovici otočenia je strana k nám bokom – stmav ju, aby to vyzeralo ako ohyb
        _flipPageImage.color = Color.Lerp(pageBackColor * 0.8f, pageColor, Mathf.Abs(scaleX));

        if (_flipTimer <= 0f) _flipPage.gameObject.SetActive(false);
    }

    private void StartPageFlip()
    {
        _flipTimer = pageFlipDuration;
        _flipPage.localScale = Vector3.one;
        _flipPage.gameObject.SetActive(true);
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
            // pohyb a kurzor teraz riadi niečo iné (pauza, dialóg, smrť)
            if (IsShopOpen)
            {
                CloseShop(restoreControls: false);
                // dialóg kurzor nerieši – schovaj ho, aby sa po rozhovore dalo zas rozhliadať
                if (QuestUI.Instance != null && QuestUI.Instance.IsDialogueOpen && !PauseMenuController.IsPaused)
                {
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
            return;
        }

        if (IsShopOpen) _lastOpenFrame = Time.frameCount;

        if (kb.bKey.wasPressedThisFrame || (IsShopOpen && kb.escapeKey.wasPressedThisFrame))
        {
            if (IsShopOpen) CloseShop(restoreControls: true);
            else OpenShop();
            return;
        }

        if (!IsShopOpen) return;

        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
            Buy(1, _wallet.seedPrice);
        else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
            Buy(_wallet.seedPackSize, _wallet.seedPackPrice);
    }

    // počas obchodu je kurzor voľný a hráčka stojí – kamera sa neotáča za myšou
    private void OpenShop()
    {
        _shopPanel.SetActive(true);
        _shopMessage.text = "";
        Refresh();
        StartPageFlip();
        _lastOpenFrame = Time.frameCount;

        EnsureEventSystem();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SetPlayerCanMove(false);

        if (QuestManager.Instance != null)
            QuestManager.Instance.NotifyShopOpened();
    }

    private void CloseShop(bool restoreControls)
    {
        _shopPanel.SetActive(false);
        _lastOpenFrame = Time.frameCount;
        if (!restoreControls) return;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        SetPlayerCanMove(true);
    }

    private void SetPlayerCanMove(bool canMove)
    {
        if (_wallet.Player != null && _wallet.Player.TryGetComponent(out PlayerController controller))
            controller.canMove = canMove;
    }

    // kliknutia na tlačidlá potrebujú EventSystem – ak v scéne chýba, vytvor ho
    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        DontDestroyOnLoad(go);
    }

    private void Buy(int count, int price)
    {
        if (_wallet.TryBuySeeds(count, price))
            _shopMessage.text = $"<color=#{ColorUtility.ToHtmlStringRGB(goodInkColor)}>Kúpené: {SeedWord(count)}</color>";
        else
            _shopMessage.text = $"<color=#{ColorUtility.ToHtmlStringRGB(badInkColor)}>Nemáš dosť peňazí – poraz drevorubača</color>";
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
            $"Peniaze: <color=#{gold}>{_wallet.Money}</color>\n" +
            $"Semienka: <color=#{ColorUtility.ToHtmlStringRGB(seedColor)}>{_wallet.Seeds}</color>\n" +
            "<size=21><color=#FFFFFF88>B – obchod   F – zasadiť</color></size>";

        Color faded = inkColor;
        faded.a = 0.35f;
        _offerSingle.text = $"<b>[1]</b>  1 semienko\n<size=24><i>{_wallet.seedPrice} peňazí</i></size>";
        _offerSingle.color = _wallet.Money >= _wallet.seedPrice ? inkColor : faded;
        _offerPack.text = $"<b>[2]</b>  {SeedWord(_wallet.seedPackSize)}\n<size=24><i>{_wallet.seedPackPrice} peňazí – výhodné balenie</i></size>";
        _offerPack.color = _wallet.Money >= _wallet.seedPackPrice ? inkColor : faded;
        _shopWallet.text = $"Peniaze: <b>{_wallet.Money}</b>\nSemienka: <b>{_wallet.Seeds}</b>";
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
        canvasGo.AddComponent<GraphicRaycaster>(); // kliknutia myšou na ponuku v obchode
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasGo.transform;

        // peniaze a semienka – vpravo hore
        // rám a písmo v štýle Bloodlines UI (zelená verzia), bez nich ostane jednoduchý tmavý panel
        var wallet = NewRect("Wallet", root);
        SetAnchors(wallet, Vector2.one, Vector2.one, Vector2.one);
        wallet.anchoredPosition = new Vector2(-30, -30);
        Sprite frame = Resources.Load<Sprite>("UI/HUD/Panel");
        var walletImage = AddImage(wallet, frame != null ? Color.white : panelColor);
        if (frame != null)
        {
            wallet.sizeDelta = new Vector2(360, 156);
            walletImage.sprite = frame;
            walletImage.type = Image.Type.Sliced;
            walletImage.pixelsPerUnitMultiplier = 0.7f;
        }
        else
        {
            wallet.sizeDelta = new Vector2(320, 120);
        }
        var walletText = NewRect("Text", wallet);
        Stretch(walletText);
        walletText.offsetMin = frame != null ? new Vector2(38, 26) : new Vector2(20, 10);
        walletText.offsetMax = frame != null ? new Vector2(-38, -28) : new Vector2(-20, -10);
        _walletText = AddText(walletText, 30, TextAlignmentOptions.TopLeft, new Color(0.86f, 0.86f, 0.82f));
        _walletText.lineSpacing = -6;
        TMP_FontAsset hudFont = Resources.Load<TMP_FontAsset>("UI/Fonts/MedievalSharp SDF");
        if (hudFont != null) _walletText.font = hudFont;

        var delta = NewRect("Delta", wallet);
        SetAnchors(delta, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 1));
        delta.sizeDelta = new Vector2(-40, 40);
        _deltaText = AddText(delta, 32, TextAlignmentOptions.Right, moneyColor);
        _deltaText.fontStyle = FontStyles.Bold;
        _deltaText.outlineWidth = 0.2f;
        _deltaText.outlineColor = Color.black;
        if (hudFont != null) _deltaText.font = hudFont;
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

        BuildShopBook(root);
    }

    // obchod – otvorená kniha v strede obrazovky: kožený obal, dve strany, tieň pri chrbte a zahnutý roh
    private void BuildShopBook(Transform root)
    {
        const float pageW = 520f, pageH = 640f;
        Color faded = inkColor;
        faded.a = 0.6f;

        var shop = NewRect("Shop", root);
        SetAnchors(shop, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        shop.sizeDelta = new Vector2(pageW * 2f, pageH);

        // tieň pod knihou, obal a okraje listov (hrúbka knihy)
        var shadow = NewRect("Shadow", shop);
        Stretch(shadow);
        shadow.offsetMin = new Vector2(-34, -48);
        shadow.offsetMax = new Vector2(34, 14);
        AddImage(shadow, new Color(0f, 0f, 0f, 0.35f));

        var cover = NewRect("Cover", shop);
        Stretch(cover);
        cover.offsetMin = new Vector2(-28, -32);
        cover.offsetMax = new Vector2(28, 22);
        AddImage(cover, coverColor);

        var edges = NewRect("PageEdges", shop);
        Stretch(edges);
        edges.offsetMin = new Vector2(-10, -14);
        edges.offsetMax = new Vector2(10, 4);
        AddImage(edges, pageBackColor);

        RectTransform left = Page("LeftPage", shop, new Vector2(1f, 0.5f), new Vector2(pageW, pageH));
        RectTransform right = Page("RightPage", shop, new Vector2(0f, 0.5f), new Vector2(pageW, pageH));
        SpineShadow(left, rightSide: true);
        SpineShadow(right, rightSide: false);
        PageCurl(right, 80f);

        // ľavá strana – názov, popis a peniaze
        var title = PageText(left, "Title", -60, 60, 40, TextAlignmentOptions.Center, inkColor);
        title.fontStyle = FontStyles.Bold;
        title.text = "Obchod so semienkami";

        PageText(left, "Line", -124, 20, 22, TextAlignmentOptions.Center, faded).text = "~ • ~";

        PageText(left, "Description", -160, 200, 26, TextAlignmentOptions.Top, inkColor).text =
            "<i>Zasaď semienko a vyrastie z neho nový strom.\n\nKaždý strom pomôže lesu, ktorý drevorubač zničil.</i>";

        _shopWallet = PageText(left, "Wallet", -430, 80, 28, TextAlignmentOptions.Center, inkColor);

        // pravá strana – ponuka
        var offersTitle = PageText(right, "OffersTitle", -60, 50, 32, TextAlignmentOptions.Center, inkColor);
        offersTitle.fontStyle = FontStyles.Bold;
        offersTitle.text = "Ponuka";

        _offerSingle = PageButton(right, "Offer1", -130, 96, 30, TextAlignmentOptions.Left, inkColor,
                                  () => Buy(1, _wallet.seedPrice));
        _offerPack = PageButton(right, "Offer2", -240, 96, 30, TextAlignmentOptions.Left, inkColor,
                                () => Buy(_wallet.seedPackSize, _wallet.seedPackPrice));
        _shopMessage = PageText(right, "ShopMessage", -360, 70, 24, TextAlignmentOptions.Center, inkColor);

        PageButton(right, "Close", -530, 50, 24, TextAlignmentOptions.Center, faded,
                   () => CloseShop(restoreControls: true)).text = "Zavrieť  <size=20>(B / Esc)</size>";

        // strana, ktorá sa pri otvorení preklopí z pravej strany na ľavú (otáča sa okolo chrbta)
        _flipPage = NewRect("FlipPage", shop);
        SetAnchors(_flipPage, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f));
        _flipPage.sizeDelta = new Vector2(pageW, pageH);
        _flipPageImage = _flipPage.gameObject.AddComponent<Image>();
        _flipPageImage.color = pageColor;
        _flipPageImage.raycastTarget = false;
        _flipPage.gameObject.SetActive(false);

        _shopPanel = shop.gameObject;
        _shopPanel.SetActive(false);
    }

    private RectTransform Page(string name, RectTransform book, Vector2 pivot, Vector2 size)
    {
        var page = NewRect(name, book);
        SetAnchors(page, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pivot);
        page.sizeDelta = size;
        AddImage(page, pageColor);
        return page;
    }

    // tmavnutie strany smerom k chrbtu knihy (niekoľko pruhov nad sebou = jednoduchý prechod)
    private static void SpineShadow(RectTransform page, bool rightSide)
    {
        float[] widths = { 70f, 40f, 18f };
        foreach (float w in widths)
        {
            var strip = NewRect("SpineShadow", page);
            float x = rightSide ? 1f : 0f;
            SetAnchors(strip, new Vector2(x, 0f), new Vector2(x, 1f), new Vector2(x, 0.5f));
            strip.sizeDelta = new Vector2(w, 0f);
            AddImage(strip, new Color(0f, 0f, 0f, 0.07f));
        }
    }

    // zahnutý pravý dolný roh: roh strany je "preč" (vidno obal) a nad ním leží preložený rub strany
    private void PageCurl(RectTransform page, float size)
    {
        var corner = NewRect("PageCurl", page);
        SetAnchors(corner, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));
        corner.sizeDelta = new Vector2(size, size);
        corner.gameObject.AddComponent<RectMask2D>();

        float diagonal = size * Mathf.Sqrt(2f);
        // štvorce otočené o 45° – maska z nich nechá len trojuholníky
        Diamond(corner, "Gap", new Vector2(1f, 0f), diagonal, coverColor);
        Diamond(corner, "Flap", new Vector2(0f, 1f), diagonal, pageBackColor);

        // ohyb – tenká tmavá čiara po uhlopriečke
        var fold = NewRect("Fold", corner);
        SetAnchors(fold, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        fold.sizeDelta = new Vector2(diagonal, 3f);
        fold.localRotation = Quaternion.Euler(0f, 0f, 45f);
        AddImage(fold, new Color(0f, 0f, 0f, 0.3f));
    }

    private static void Diamond(RectTransform parent, string name, Vector2 anchor, float size, Color color)
    {
        var rt = NewRect(name, parent);
        SetAnchors(rt, anchor, anchor, new Vector2(0.5f, 0.5f));
        rt.sizeDelta = new Vector2(size, size);
        rt.localRotation = Quaternion.Euler(0f, 0f, 45f);
        AddImage(rt, color);
    }

    // klikateľný riadok na strane – pri prejdení myšou sa jemne podfarbí
    private TextMeshProUGUI PageButton(RectTransform page, string name, float y, float height, float size,
                                       TextAlignmentOptions align, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var rt = NewRect(name, page);
        SetAnchors(rt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
        rt.anchoredPosition = new Vector2(0, y);
        rt.sizeDelta = new Vector2(-80, height);

        var background = rt.gameObject.AddComponent<Image>();
        background.color = new Color(inkColor.r, inkColor.g, inkColor.b, 1f);

        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0f);
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.12f);
        colors.selectedColor = new Color(1f, 1f, 1f, 0f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.25f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        var label = NewRect("Text", rt);
        Stretch(label);
        label.offsetMin = new Vector2(20, 0);
        label.offsetMax = new Vector2(-20, 0);
        return AddText(label, size, align, color);
    }

    // text na strane knihy s okrajmi, y je odsadenie od horného okraja strany
    private static TextMeshProUGUI PageText(RectTransform page, string name, float y, float height, float size,
                                            TextAlignmentOptions align, Color color)
    {
        var rt = NewRect(name, page);
        SetAnchors(rt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
        rt.anchoredPosition = new Vector2(0, y);
        rt.sizeDelta = new Vector2(-120, height);
        return AddText(rt, size, align, color);
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

    private static Image AddImage(RectTransform rt, Color color)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
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
