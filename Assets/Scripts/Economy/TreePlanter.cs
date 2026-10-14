using UnityEngine;
using UnityEngine.InputSystem;
using UnityTutorial.PlayerControl;

// Sadenie semienok: hráč sa pozrie na zem pred sebou a stlačí F.
public class TreePlanter : MonoBehaviour
{
    public float plantRange = 12f;
    public float minTreeSpacing = 3f;    // minimálna vzdialenosť od iných stromov a kameňov
    [Range(0f, 1f)] public float maxSlope = 0.75f; // normal.y – strmšia zem sa nedá osadiť
    public float growTime = 45f;

    private PlayerWallet _wallet;
    private EconomyUI _ui;
    private Camera _camera;

    private void Start()
    {
        _wallet = GetComponent<PlayerWallet>();
        _ui = GetComponent<EconomyUI>();
    }

    private void Update()
    {
        if (_wallet == null) return;
        PlayerHealth player = _wallet.Player;
        if (player == null || player.IsDead || !CanAct())
        {
            _ui.SetPlantPrompt(null);
            return;
        }

        if (_camera == null) _camera = player.GetComponentInChildren<Camera>();
        if (_camera == null) return;

        bool aiming = TryGetGroundPoint(player.transform, out Vector3 point, out Terrain terrain);
        _ui.SetPlantPrompt(aiming && _wallet.Seeds > 0 ? "Zasaď semienko (F)" : null);

        if (Keyboard.current == null || !Keyboard.current.fKey.wasPressedThisFrame) return;

        if (_wallet.Seeds <= 0)
            _ui.ShowMessage("Nemáš semienka – kúp si ich v obchode (B)");
        else if (!aiming)
            _ui.ShowMessage("Pozri sa na rovnú zem blízko seba");
        else if (TerrainTreeCutting.Instance.IsTreeNear(point, minTreeSpacing) || GrowingTree.IsNear(point, minTreeSpacing))
            _ui.ShowMessage("Tu je málo miesta – skús to ďalej od stromov");
        else
            Plant(terrain, point);
    }

    private void Plant(Terrain terrain, Vector3 point)
    {
        int prototype = TerrainTreeCutting.Instance.RandomTreePrototype(terrain);
        if (prototype < 0 || !_wallet.TryUseSeed()) return;

        GrowingTree.Plant(terrain, point, prototype, growTime);
        _ui.ShowMessage("Semienko zasadené – strom čoskoro vyrastie");
        PlayerWallet.Log("tree_plant", point, "seed", _wallet.Seeds);
        if (QuestManager.Instance != null)
            QuestManager.Instance.NotifyTreePlanted(point);
    }

    // bod na teréne v strede obrazovky; ignoruje hráčku a neviditeľné zóny
    private bool TryGetGroundPoint(Transform player, out Vector3 point, out Terrain terrain)
    {
        point = default;
        terrain = null;

        Ray ray = _camera.ScreenPointToRay(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        // v pohľade z tretej osoby meraj dosah od hlavy, nie od kamery
        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller != null)
            ray.origin += ray.direction * controller.CameraPullback;
        RaycastHit best = default;
        float nearest = float.MaxValue;
        foreach (RaycastHit h in Physics.RaycastAll(ray, plantRange, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.transform.IsChildOf(player) || h.distance >= nearest) continue;
            nearest = h.distance;
            best = h;
        }

        if (best.collider is not TerrainCollider || best.normal.y < maxSlope) return false;

        point = best.point;
        terrain = best.collider.GetComponent<Terrain>();
        return terrain != null;
    }

    private bool CanAct()
    {
        if (PauseMenuController.IsPaused || _ui.IsShopOpen) return false;
        return QuestUI.Instance == null || !QuestUI.Instance.IsDialogueOpen;
    }
}
