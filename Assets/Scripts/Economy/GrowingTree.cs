using System.Collections.Generic;
using UnityEngine;

// Zasadený stromček: z kôpky hliny postupne vyrastie a keď je dospelý,
// stane sa obyčajným stromom na teréne (ten môžu drevorubači opäť zoťať).
public class GrowingTree : MonoBehaviour
{
    private static readonly List<GrowingTree> All = new();
    private static Material _soilMaterial;

    public float growTime = 45f;
    public float startScale = 0.05f;

    private Terrain _terrain;
    private int _prototype;
    private float _height, _width, _rotation;
    private Transform _model;
    private float _progress;

    public static bool IsNear(Vector3 position, float radius)
    {
        foreach (GrowingTree t in All)
        {
            Vector3 d = t.transform.position - position;
            d.y = 0f;
            if (d.sqrMagnitude < radius * radius) return true;
        }
        return false;
    }

    public static GrowingTree Plant(Terrain terrain, Vector3 position, int prototypeIndex, float growTime)
    {
        var go = new GameObject("Growing Tree");
        go.transform.position = position;
        var tree = go.AddComponent<GrowingTree>();
        tree.growTime = growTime;
        tree.Setup(terrain, prototypeIndex);
        return tree;
    }

    private void Setup(Terrain terrain, int prototypeIndex)
    {
        _terrain = terrain;
        _prototype = prototypeIndex;
        _height = Random.Range(0.9f, 1.15f);
        _width = _height * Random.Range(0.95f, 1.05f);
        _rotation = Random.Range(0f, Mathf.PI * 2f);

        CreateSoilMound();

        GameObject prefab = terrain.terrainData.treePrototypes[prototypeIndex].prefab;
        if (prefab != null)
        {
            GameObject model = Instantiate(prefab, transform.position, Quaternion.Euler(0f, _rotation * Mathf.Rad2Deg, 0f), transform);
            foreach (Collider c in model.GetComponentsInChildren<Collider>())
                Destroy(c);
            _model = model.transform;
            ApplyScale(startScale);
        }
    }

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    private void Update()
    {
        _progress += Time.deltaTime / growTime;
        float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_progress));
        ApplyScale(Mathf.Lerp(startScale, 1f, eased));

        if (_progress < 1f) return;

        TerrainTreeCutting.Instance.AddTree(_terrain, transform.position, _prototype, _height, _width, _rotation);
        PlayerWallet.Log("tree_grown", transform.position, "tree");
        Destroy(gameObject);
    }

    private void ApplyScale(float s)
    {
        if (_model != null)
            _model.localScale = new Vector3(_width * s, _height * s, _width * s);
    }

    private void CreateSoilMound()
    {
        GameObject mound = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mound.name = "Soil";
        Destroy(mound.GetComponent<Collider>());
        mound.transform.SetParent(transform, false);
        mound.transform.localScale = new Vector3(0.9f, 0.2f, 0.9f);

        if (_soilMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) return;
            _soilMaterial = new Material(shader);
            _soilMaterial.SetColor("_BaseColor", new Color(0.3f, 0.2f, 0.12f));
            _soilMaterial.SetFloat("_Smoothness", 0.05f);
        }
        mound.GetComponent<Renderer>().sharedMaterial = _soilMaterial;
    }
}
