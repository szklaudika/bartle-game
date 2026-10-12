using UnityEditor;
using UnityEngine;

// Vytvorí prefab ohnivej gule a výbuchu a pridá hráčovi (PlayerModel) streľbu a zdravie.
// Menu: Tools > Player > Set Up Shooting. Dá sa spustiť opakovane.
public static class PlayerShootingSetup
{
    private const string PlayerPrefabPath = "Assets/player/Prefab/PlayerModel.prefab";
    private const string Folder = "Assets/prefabs/Fireball";

    [MenuItem("Tools/Player/Set Up Shooting")]
    public static void SetUp()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/prefabs", "Fireball");

        GameObject explosion = BuildExplosion();
        GameObject fireball = BuildFireball(explosion);
        SetUpPlayer(fireball);

        AssetDatabase.SaveAssets();
        Debug.Log("Player shooting set up: fireball prefab created and PlayerShooting/PlayerHealth added to PlayerModel.");
    }

    private static GameObject BuildFireball(GameObject explosion)
    {
        Material core = Mat("Fireball_Core", "Universal Render Pipeline/Unlit", new Color(1f, 0.55f, 0.1f));
        Material trail = Mat("Fireball_Trail", "Universal Render Pipeline/Particles/Unlit", new Color(1f, 0.35f, 0.05f));

        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "Fireball";
        root.transform.localScale = Vector3.one * 0.9f;
        root.GetComponent<Renderer>().sharedMaterial = core;

        var col = root.GetComponent<SphereCollider>();
        col.isTrigger = true;

        var rb = root.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        var bullet = root.AddComponent<Bullet>();
        bullet.explosionPrefab = explosion;
        bullet.explosionRadius = 8f;

        AddLight(root.transform, new Color(1f, 0.5f, 0.15f), 7f, 2f);

        var tr = root.AddComponent<TrailRenderer>();
        tr.sharedMaterial = trail;
        tr.time = 0.25f;
        tr.startWidth = 0.75f;
        tr.endWidth = 0f;
        tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        return Save(root, "Fireball");
    }

    private static GameObject BuildExplosion()
    {
        Material mat = Mat("Fireball_Explosion", "Universal Render Pipeline/Unlit", new Color(1f, 0.75f, 0.25f));

        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "FireballExplosion";
        Object.DestroyImmediate(root.GetComponent<Collider>());
        root.GetComponent<Renderer>().sharedMaterial = mat;
        root.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        AddLight(root.transform, new Color(1f, 0.6f, 0.2f), 18f, 6f);
        root.AddComponent<FireballExplosion>().maxSize = 6f;

        return Save(root, "FireballExplosion");
    }

    private static void SetUpPlayer(GameObject fireball)
    {
        GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            Camera cam = player.GetComponentInChildren<Camera>(true);
            if (cam == null)
            {
                Debug.LogError("PlayerModel has no Camera child – shooting needs one to aim.");
                return;
            }
            // Camera.main (napr. pre otáčanie panelov zdravia nepriateľov) potrebuje tento tag
            cam.tag = "MainCamera";

            Transform shootPoint = cam.transform.Find("ShootPoint");
            if (shootPoint == null)
            {
                shootPoint = new GameObject("ShootPoint").transform;
                shootPoint.SetParent(cam.transform, false);
            }
            // trochu vpravo dole pred kamerou – guľa nevyletí priamo z oka
            shootPoint.localPosition = new Vector3(0.12f, -0.12f, 0.45f);
            shootPoint.localRotation = Quaternion.identity;

            if (!player.TryGetComponent(out PlayerHealth _))
                player.AddComponent<PlayerHealth>();

            if (!player.TryGetComponent(out PlayerShooting shooting))
                shooting = player.AddComponent<PlayerShooting>();
            shooting.cam = cam;
            shooting.bulletPrefab = fireball;
            shooting.shootPoint = shootPoint;
            shooting.playerAnimator = player.GetComponentInChildren<Animator>();
            shooting.audioSource = player.GetComponent<AudioSource>();

            PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(player);
        }
    }

    private static void AddLight(Transform parent, Color color, float range, float intensity)
    {
        var go = new GameObject("Light");
        go.transform.SetParent(parent, false);
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = range;
        light.intensity = intensity;
        light.shadows = LightShadows.None;
    }

    private static Material Mat(string name, string shaderName, Color color)
    {
        string path = $"{Folder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find(shaderName));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static GameObject Save(GameObject root, string name)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Folder}/{name}.prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }
}
