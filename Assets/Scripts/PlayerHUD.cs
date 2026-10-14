using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityTutorial.PlayerControl;

// Zdravie hráča a obrazovka smrti – celé UI sa vytvorí za behu (vytvára ho PlayerHealth).
// Ukazovateľ zdravia vľavo dole, červený záblesk pri zásahu, po smrti kamera padne na zem
// a zobrazí sa "Koniec hry" s možnosťou reštartu klávesom R.
public class PlayerHUD : MonoBehaviour
{
    [Header("Vzhľad")]
    public Color panelColor = new Color(0f, 0f, 0f, 0.6f);
    public Color lostHealthColor = new Color(1f, 0.85f, 0.6f, 0.9f);
    public float flashAlpha = 0.35f;
    public float flashFadeSpeed = 1.5f;
    [Range(0f, 1f)] public float lowHealthThreshold = 0.3f;

    [Header("Smrť")]
    public float deathFadeTime = 1.5f;
    public float cameraFallTime = 1.2f;

    // zelené obrázky ukazovateľa (z Bloodlines UI) v Assets/Resources/UI/HealthBar
    private const string BarSpritePath = "UI/HealthBar/";
    private const float BarFrameMargin = 0.04f;

    private PlayerHealth _health;
    private RectTransform _fill;
    private Image _fillImage;
    private RectTransform _lostFill;   // krémový pás, ktorý pomaly dobieha za zdravím
    private Image _lostFillImage;
    private TextMeshProUGUI _valueText;
    private Image _flash;
    private CanvasGroup _deathScreen;
    private TextMeshProUGUI _restartHint;

    private float _shownPercent = 1f;
    private float _lostPercent = 1f;
    private float _flashAmount;
    private float _deathTime = -1f;

    private Transform _camera;
    private Vector3 _camStartPos;
    private Quaternion _camStartRot;

    public void Init(PlayerHealth health)
    {
        _health = health;
        _health.Damaged += OnDamaged;
        _health.Died += OnDied;
        BuildUI();
    }

    private void OnDestroy()
    {
        if (_health == null) return;
        _health.Damaged -= OnDamaged;
        _health.Died -= OnDied;
    }

    private void Update()
    {
        if (_health == null) return;

        // ---------- ukazovateľ zdravia ----------
        float target = Mathf.Clamp01(_health.CurrentHealthPercent());
        _shownPercent = Mathf.MoveTowards(_shownPercent, target, Time.deltaTime * 2f);
        _lostPercent = _lostPercent > _shownPercent
            ? Mathf.MoveTowards(_lostPercent, _shownPercent, Time.deltaTime * 0.5f)
            : _shownPercent;

        _fillImage.fillAmount = BarFill(_shownPercent);
        _lostFillImage.fillAmount = BarFill(_lostPercent);
        _valueText.text = $"{Mathf.CeilToInt(_health.CurrentHealth)} / {Mathf.RoundToInt(_health.maxHealth)}";

        // ---------- červený záblesk; pri nízkom zdraví jemne pulzuje ----------
        _flashAmount = Mathf.MoveTowards(_flashAmount, 0f, Time.deltaTime * flashFadeSpeed);
        float pulse = 0f;
        if (!_health.IsDead && target > 0f && target < lowHealthThreshold)
            pulse = (Mathf.Sin(Time.time * 4f) * 0.5f + 0.5f) * 0.15f;
        SetAlpha(_flash, Mathf.Max(_flashAmount, pulse));

        if (_deathTime >= 0f) UpdateDeath();
    }

    private void LateUpdate()
    {
        // kamera po smrti padne na zem (PlayerController ju vtedy už neovláda)
        if (_deathTime < 0f || _camera == null) return;

        float t = Mathf.Clamp01((Time.time - _deathTime) / cameraFallTime);
        float eased = 1f - (1f - t) * (1f - t);
        Vector3 groundPos = _health.transform.position + Vector3.up * 0.3f;
        Quaternion lyingRot = _camStartRot * Quaternion.Euler(-15f, 0f, 75f);

        _camera.position = Vector3.Lerp(_camStartPos, groundPos, eased);
        _camera.rotation = Quaternion.Slerp(_camStartRot, lyingRot, eased);
    }

    private void OnDamaged(float amount)
    {
        _flashAmount = flashAlpha;
    }

    private void OnDied()
    {
        _deathTime = Time.time;
        _flashAmount = 0.6f;

        var controller = _health.GetComponent<PlayerController>();
        if (controller != null) controller.canMove = false;

        Camera cam = _health.GetComponentInChildren<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            _camera = cam.transform;
            _camStartPos = _camera.position;
            _camStartRot = _camera.rotation;
        }

        // bez GameOverManagera v scéne zapíšeme prehru sami
        if (GameOverManager.Instance == null && FirebaseGameAnalytics.Instance != null && FirebaseGameAnalytics.Instance.IsReady)
        {
            FirebaseGameAnalytics.Instance.LogGameplayEvent("lose");
            FirebaseGameAnalytics.Instance.EndSession(true);
        }
    }

    private void UpdateDeath()
    {
        float t = (Time.time - _deathTime) / deathFadeTime;
        _deathScreen.alpha = Mathf.Clamp01(t);
        _restartHint.gameObject.SetActive(t >= 1f);

        if (t >= 1f && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            Restart();
    }

    private void Restart()
    {
        if (FirebaseGameAnalytics.Instance != null && FirebaseGameAnalytics.Instance.IsReady)
            FirebaseGameAnalytics.Instance.StartSession();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private static void SetAlpha(Graphic g, float a)
    {
        Color c = g.color;
        c.a = a;
        g.color = c;
    }

    // ---------- Stavba UI ----------

    private void BuildUI()
    {
        var canvasGo = new GameObject("PlayerCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60; // nad úlohami (QuestUI má 50)
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasGo.transform;

        // záblesk pri zásahu – cez celú obrazovku
        var flash = Stretch(NewRect("DamageFlash", root));
        _flash = AddImage(flash, new Color(0.8f, 0f, 0f, 0f));

        // zdravie – vľavo dole
        var panel = NewRect("Health", root);
        SetAnchors(panel, Vector2.zero, Vector2.zero, Vector2.zero);
        panel.anchoredPosition = new Vector2(30, 30);
        panel.sizeDelta = new Vector2(400, 112);
        AddImage(panel, panelColor);

        var label = NewRect("Label", panel);
        SetAnchors(label, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
        label.anchoredPosition = new Vector2(0, -8);
        label.sizeDelta = new Vector2(-36, 32);
        var labelText = AddText(label, 24, TextAlignmentOptions.Left, Color.white);
        labelText.text = "<b>Zdravie</b>";

        var value = NewRect("Value", panel);
        SetAnchors(value, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
        value.anchoredPosition = new Vector2(0, -8);
        value.sizeDelta = new Vector2(-36, 32);
        _valueText = AddText(value, 24, TextAlignmentOptions.Right, Color.white);

        // ukazovateľ v štýle Bloodlines UI (zelená verzia): prázdny rám, pod ním krémový pás straty, navrchu zelená
        var bar = NewRect("Bar", panel);
        SetAnchors(bar, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0));
        bar.anchoredPosition = new Vector2(0, 10);
        bar.sizeDelta = new Vector2(-24, 58);
        Sprite empty = Resources.Load<Sprite>(BarSpritePath + "HealthBar_Empty");
        AddImage(bar, empty != null ? Color.white : new Color(0.15f, 0.15f, 0.15f, 0.9f)).sprite = empty;

        _lostFill = Stretch(NewRect("Lost", bar));
        _lostFillImage = AddBarFill(_lostFill, "HealthBar_Lost", lostHealthColor);
        _fill = Stretch(NewRect("Fill", bar));
        _fillImage = AddBarFill(_fill, "HealthBar_Full", new Color(0.13f, 0.6f, 0.25f));

        // obrazovka smrti
        var death = Stretch(NewRect("DeathScreen", root));
        AddImage(death, new Color(0.15f, 0f, 0f, 0.75f));
        _deathScreen = death.gameObject.AddComponent<CanvasGroup>();
        _deathScreen.alpha = 0f;
        _deathScreen.blocksRaycasts = false;

        var title = NewRect("Title", death);
        SetAnchors(title, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        title.anchoredPosition = new Vector2(0, 70);
        title.sizeDelta = new Vector2(1400, 140);
        var titleText = AddText(title, 110, TextAlignmentOptions.Center, new Color(0.9f, 0.1f, 0.1f));
        titleText.fontStyle = FontStyles.Bold;
        titleText.outlineWidth = 0.15f;
        titleText.outlineColor = Color.black;
        titleText.text = "KONIEC HRY";

        var subtitle = NewRect("Subtitle", death);
        SetAnchors(subtitle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        subtitle.anchoredPosition = new Vector2(0, -20);
        subtitle.sizeDelta = new Vector2(1200, 50);
        AddText(subtitle, 34, TextAlignmentOptions.Center, Color.white).text = "Tvoje zdravie kleslo na nulu.";

        var hint = NewRect("RestartHint", death);
        SetAnchors(hint, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        hint.anchoredPosition = new Vector2(0, -110);
        hint.sizeDelta = new Vector2(1200, 44);
        _restartHint = AddText(hint, 28, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.4f));
        _restartHint.text = "Stlač R pre nový pokus";
        _restartHint.gameObject.SetActive(false);
    }

    // vrstva ukazovateľa, ktorá sa odkrýva zľava doprava; bez obrázka ostane jednofarebná
    private static Image AddBarFill(RectTransform rt, string spriteName, Color fallbackColor)
    {
        Sprite sprite = Resources.Load<Sprite>(BarSpritePath + spriteName);
        Image img = AddImage(rt, sprite != null ? Color.white : fallbackColor);
        img.sprite = sprite;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        return img;
    }

    // obrázok má po stranách rám – 0 % a 100 % zdravia sú na vnútorných okrajoch rámu
    private static float BarFill(float percent) =>
        percent <= 0f ? 0f : BarFrameMargin + percent * (1f - 2f * BarFrameMargin);

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

    private static RectTransform Stretch(RectTransform rt)
    {
        SetAnchors(rt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
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
