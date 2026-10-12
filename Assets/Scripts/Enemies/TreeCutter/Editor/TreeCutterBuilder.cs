using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Poskladá prefab drevorubača s motorovou pílou z primitív (placeholder model)
// a umiestni ho do scény. Menu: Tools > Tree Cutter.
public static class TreeCutterBuilder
{
    private const string Folder = "Assets/prefabs/TreeCutter";
    private const string PrefabPath = Folder + "/TreeCutter.prefab";

    [MenuItem("Tools/Tree Cutter/Place Tree Cutter In Scene")]
    public static void PlaceInScene()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) prefab = BuildPrefab();

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = GroundPointInFrontOfSceneCamera();
        Undo.RegisterCreatedObjectUndo(instance, "Place Tree Cutter");
        Selection.activeGameObject = instance;
        EditorSceneManager.MarkSceneDirty(instance.scene);
    }

    [MenuItem("Tools/Tree Cutter/Rebuild Prefab")]
    public static GameObject BuildPrefab()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/prefabs", "TreeCutter");

        Material shirt  = Mat("TreeCutter_Shirt",  new Color(0.70f, 0.10f, 0.08f));
        Material jeans  = Mat("TreeCutter_Jeans",  new Color(0.15f, 0.22f, 0.38f));
        Material boots  = Mat("TreeCutter_Boots",  new Color(0.22f, 0.14f, 0.08f));
        Material skin   = Mat("TreeCutter_Skin",   new Color(0.90f, 0.70f, 0.55f));
        Material beard  = Mat("TreeCutter_Beard",  new Color(0.35f, 0.20f, 0.10f));
        Material dark   = Mat("TreeCutter_Dark",   new Color(0.06f, 0.06f, 0.06f));
        Material helmet = Mat("TreeCutter_Helmet", new Color(1.00f, 0.75f, 0.05f), 0.6f);
        Material saw    = Mat("TreeCutter_Saw",    new Color(1.00f, 0.42f, 0.05f), 0.5f);
        Material metal  = Mat("TreeCutter_Metal",  new Color(0.70f, 0.72f, 0.75f), 0.8f, 0.9f);

        var root = new GameObject("TreeCutter");

        // ---------- Telo ----------
        Transform body = Pivot("Body", root.transform, Vector3.zero);

        Transform legL = Pivot("LegL", body, new Vector3(-0.12f, 0.85f, 0f));
        Transform legR = Pivot("LegR", body, new Vector3(0.12f, 0.85f, 0f));
        foreach (Transform leg in new[] { legL, legR })
        {
            float x = leg.position.x;
            Box("Leg", leg, new Vector3(x, 0.45f, 0f), new Vector3(0.2f, 0.8f, 0.22f), jeans);
            Box("Boot", leg, new Vector3(x, 0.06f, 0.04f), new Vector3(0.23f, 0.12f, 0.32f), boots);
        }

        Box("Torso", body, new Vector3(0f, 1.17f, 0f), new Vector3(0.5f, 0.65f, 0.3f), shirt);
        Box("Belt", body, new Vector3(0f, 0.87f, 0f), new Vector3(0.52f, 0.07f, 0.32f), dark);
        Box("SuspenderL", body, new Vector3(-0.12f, 1.17f, 0.152f), new Vector3(0.06f, 0.64f, 0.02f), dark);
        Box("SuspenderR", body, new Vector3(0.12f, 1.17f, 0.152f), new Vector3(0.06f, 0.64f, 0.02f), dark);

        Shape(PrimitiveType.Sphere, "Head", body, new Vector3(0f, 1.66f, 0f), Vector3.one * 0.3f, skin);
        Box("Beard", body, new Vector3(0f, 1.57f, 0.1f), new Vector3(0.24f, 0.15f, 0.1f), beard);
        Shape(PrimitiveType.Sphere, "EyeL", body, new Vector3(-0.06f, 1.69f, 0.135f), Vector3.one * 0.04f, dark);
        Shape(PrimitiveType.Sphere, "EyeR", body, new Vector3(0.06f, 1.69f, 0.135f), Vector3.one * 0.04f, dark);
        // zamračené obočie
        Box("BrowL", body, new Vector3(-0.06f, 1.73f, 0.135f), new Vector3(0.08f, 0.022f, 0.02f), beard)
            .rotation = Quaternion.Euler(0f, 0f, -20f);
        Box("BrowR", body, new Vector3(0.06f, 1.73f, 0.135f), new Vector3(0.08f, 0.022f, 0.02f), beard)
            .rotation = Quaternion.Euler(0f, 0f, 20f);
        Shape(PrimitiveType.Sphere, "HardHat", body, new Vector3(0f, 1.77f, 0f), new Vector3(0.34f, 0.2f, 0.34f), helmet);
        Shape(PrimitiveType.Cylinder, "HardHatBrim", body, new Vector3(0f, 1.76f, 0.02f), new Vector3(0.42f, 0.008f, 0.44f), helmet);

        // ---------- Ruky a píla ----------
        Transform arms = Pivot("Arms", body, new Vector3(0f, 1.4f, 0f));
        Vector3 handL = new Vector3(-0.1f, 1.17f, 0.5f);   // horná rukoväť
        Vector3 handR = new Vector3(0.06f, 1.06f, 0.28f);  // zadná rukoväť
        Limb("ArmL", arms, new Vector3(-0.3f, 1.42f, 0f), handL, 0.13f, shirt);
        Limb("ArmR", arms, new Vector3(0.3f, 1.42f, 0f), handR, 0.13f, shirt);
        Shape(PrimitiveType.Sphere, "HandL", arms, handL, Vector3.one * 0.1f, skin);
        Shape(PrimitiveType.Sphere, "HandR", arms, handR, Vector3.one * 0.1f, skin);

        Transform chainsaw = Pivot("Chainsaw", arms, new Vector3(0f, 1.08f, 0.45f));
        Box("Engine", chainsaw, new Vector3(0f, 1.08f, 0.45f), new Vector3(0.17f, 0.2f, 0.34f), saw);
        Box("RearHandle", chainsaw, new Vector3(0.06f, 1.06f, 0.27f), new Vector3(0.05f, 0.08f, 0.1f), dark);
        Box("TopHandle", chainsaw, new Vector3(-0.03f, 1.2f, 0.5f), new Vector3(0.18f, 0.04f, 0.04f), dark);
        Box("Bar", chainsaw, new Vector3(0f, 1.05f, 0.85f), new Vector3(0.02f, 0.08f, 0.52f), metal);

        Transform chain = Pivot("Chain", chainsaw, new Vector3(0f, 1.05f, 0.85f));
        for (int i = 0; i < 10; i++)
        {
            float z = 0.62f + i * 0.05f;
            Box("Tooth", chain, new Vector3(0f, 1.095f, z), new Vector3(0.03f, 0.02f, 0.025f), dark);
            Box("Tooth", chain, new Vector3(0f, 1.005f, z), new Vector3(0.03f, 0.02f, 0.025f), dark);
        }

        // ---------- Komponenty ----------
        var capsule = root.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 0.9f, 0.1f);
        capsule.height = 1.8f;
        capsule.radius = 0.38f;

        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        var health = root.AddComponent<EnemyHealth>();
        health.maxHealth = 100f;
        health.destroyDelay = 5f;

        var audio = root.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 1f;

        root.AddComponent<TreeCutterAI>();

        var anim = root.AddComponent<TreeCutterProceduralAnimation>();
        anim.body = body;
        anim.leftLeg = legL;
        anim.rightLeg = legR;
        anim.arms = arms;
        anim.chainsaw = chainsaw;
        anim.chain = chain;

        BuildHealthBar(root.transform, health);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        Debug.Log($"Tree Cutter prefab saved to {PrefabPath}", prefab);
        return prefab;
    }

    private static void BuildHealthBar(Transform root, EnemyHealth health)
    {
        var canvasGo = new GameObject("HealthBar", typeof(RectTransform), typeof(Canvas));
        canvasGo.transform.SetParent(root, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var rect = (RectTransform)canvasGo.transform;
        rect.sizeDelta = new Vector2(100f, 12f);
        rect.localPosition = new Vector3(0f, 2.15f, 0f);
        rect.localScale = Vector3.one * 0.01f;

        Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        Image background = UIImage("Background", rect, new Color(0f, 0f, 0f, 0.6f), sprite);
        Image fill = UIImage("Fill", rect, Color.green, sprite);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        ((RectTransform)fill.transform).offsetMin = new Vector2(1f, 1f);
        ((RectTransform)fill.transform).offsetMax = new Vector2(-1f, -1f);
        background.raycastTarget = false;
        fill.raycastTarget = false;

        var bar = canvasGo.AddComponent<EnemyHealthBar>();
        bar.enemyHealth = health;
        bar.fillImage = fill;
    }

    private static Image UIImage(string name, RectTransform parent, Color color, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        return image;
    }

    // ---------- Pomocné funkcie (pozície sú v priestore koreňa, koreň je v počiatku) ----------

    private static Transform Pivot(string name, Transform parent, Vector3 position)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.position = position;
        return t;
    }

    private static Transform Box(string name, Transform parent, Vector3 position, Vector3 size, Material mat)
        => Shape(PrimitiveType.Cube, name, parent, position, size, mat);

    private static Transform Shape(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 size, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = size;
        return go.transform;
    }

    // kváder natiahnutý medzi dvoma bodmi (ruka od ramena po dlaň)
    private static void Limb(string name, Transform parent, Vector3 from, Vector3 to, float thickness, Material mat)
    {
        Vector3 dir = to - from;
        Transform t = Box(name, parent, (from + to) * 0.5f, new Vector3(thickness, dir.magnitude, thickness), mat);
        t.rotation = Quaternion.FromToRotation(Vector3.up, dir);
    }

    private static Material Mat(string name, Color color, float smoothness = 0.15f, float metallic = 0f)
    {
        string path = $"{Folder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", metallic);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Vector3 GroundPointInFrontOfSceneCamera()
    {
        SceneView view = SceneView.lastActiveSceneView;
        if (view == null) return Vector3.zero;

        Transform cam = view.camera.transform;
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 500f))
            return hit.point;

        Vector3 point = cam.position + cam.forward * 10f;
        if (TerrainTreeCutting.TryGetGroundHeight(point, out float y)) point.y = y;
        return point;
    }
}
