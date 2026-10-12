using System.Collections.Generic;
using UnityEngine;

// Strom namaľovaný na teréne, ktorý si drevorubač vybral.
public class TreeTarget
{
    public Terrain terrain;
    public Vector3 normalizedPosition; // pozícia v TreeInstance (0..1)
    public Vector3 worldPosition;
    public TreeInstance instance;
}

// Spravuje rúbanie stromov na teréne: hľadanie, rezervovanie a odstránenie stromu.
// TerrainData sú assety – zmeny by v editore prežili Play mode, preto sa pôvodné stromy
// pri zničení scény vrátia späť.
public class TerrainTreeCutting : MonoBehaviour
{
    private static TerrainTreeCutting _instance;

    public static TerrainTreeCutting Instance
    {
        get
        {
            if (_instance == null)
                _instance = new GameObject("[TerrainTreeCutting]").AddComponent<TerrainTreeCutting>();
            return _instance;
        }
    }

    public static bool HasInstance => _instance != null;

    private readonly Dictionary<TerrainData, TreeInstance[]> _originalTrees = new();
    private readonly List<TreeTarget> _claimed = new();
    private Material _stumpMaterial;

    // Nájde najbližší voľný strom v okruhu a rezervuje ho, aby ho nerúbali dvaja naraz.
    public TreeTarget ClaimNearestTree(Vector3 from, float maxDistance)
    {
        TreeTarget best = null;
        float bestSqr = maxDistance * maxDistance;

        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            TerrainData data = terrain.terrainData;
            if (data == null) continue;

            Vector3 origin = terrain.transform.position;
            Bounds bounds = new Bounds(origin + data.size * 0.5f, data.size);
            if (bounds.SqrDistance(from) > bestSqr) continue;

            int count = data.treeInstanceCount;
            for (int i = 0; i < count; i++)
            {
                TreeInstance tree = data.GetTreeInstance(i);
                Vector3 world = origin + Vector3.Scale(tree.position, data.size);
                float sqr = (world - from).sqrMagnitude;
                if (sqr >= bestSqr || IsClaimed(terrain, tree.position)) continue;

                bestSqr = sqr;
                best ??= new TreeTarget();
                best.terrain = terrain;
                best.normalizedPosition = tree.position;
                best.worldPosition = world;
                best.instance = tree;
            }
        }

        if (best != null) _claimed.Add(best);
        return best;
    }

    public void Release(TreeTarget target)
    {
        if (target != null) _claimed.Remove(target);
    }

    // Odstráni strom z terénu a namiesto neho vytvorí padajúci strom a peň.
    // Vracia false, ak strom medzitým zmizol.
    public bool CutTree(TreeTarget target, Vector3 fallDirection)
    {
        Release(target);
        if (target == null || target.terrain == null) return false;

        TerrainData data = target.terrain.terrainData;
        TreeInstance[] trees = data.treeInstances;
        int index = System.Array.FindIndex(trees, t => SamePosition(t.position, target.normalizedPosition));
        if (index < 0) return false;

        if (!_originalTrees.ContainsKey(data))
            _originalTrees[data] = trees;

        var remaining = new TreeInstance[trees.Length - 1];
        System.Array.Copy(trees, 0, remaining, 0, index);
        System.Array.Copy(trees, index + 1, remaining, index, trees.Length - index - 1);
        data.SetTreeInstances(remaining, false);

        SpawnFallingTree(target, data, fallDirection);
        return true;
    }

    private void SpawnFallingTree(TreeTarget target, TerrainData data, Vector3 fallDirection)
    {
        TreeInstance tree = target.instance;
        float width = tree.widthScale;
        float height = tree.heightScale;

        GameObject prefab = null;
        if (tree.prototypeIndex >= 0 && tree.prototypeIndex < data.treePrototypes.Length)
            prefab = data.treePrototypes[tree.prototypeIndex].prefab;

        if (prefab != null)
        {
            Quaternion rot = Quaternion.Euler(0f, tree.rotation * Mathf.Rad2Deg, 0f);
            GameObject fallen = Instantiate(prefab, target.worldPosition, rot);
            fallen.name = prefab.name + " (cut)";
            fallen.transform.localScale = new Vector3(width, height, width);

            // padajúci strom nesmie strkať do hráča ani do drevorubača
            foreach (Collider c in fallen.GetComponentsInChildren<Collider>())
                Destroy(c);

            fallen.AddComponent<FallingTree>().Begin(fallDirection);
        }

        CreateStump(target.worldPosition, width);
    }

    private void CreateStump(Vector3 position, float widthScale)
    {
        GameObject stump = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stump.name = "Tree Stump";
        Destroy(stump.GetComponent<Collider>());
        stump.transform.SetParent(transform, true);
        stump.transform.position = position + Vector3.up * 0.15f;
        float r = 0.55f * Mathf.Max(0.5f, widthScale);
        stump.transform.localScale = new Vector3(r, 0.2f, r);

        if (_stumpMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) return;
            _stumpMaterial = new Material(shader);
            _stumpMaterial.SetColor("_BaseColor", new Color(0.45f, 0.3f, 0.17f));
        }
        stump.GetComponent<Renderer>().sharedMaterial = _stumpMaterial;
    }

    private bool IsClaimed(Terrain terrain, Vector3 normalizedPosition)
    {
        foreach (TreeTarget t in _claimed)
            if (t.terrain == terrain && SamePosition(t.normalizedPosition, normalizedPosition))
                return true;
        return false;
    }

    private static bool SamePosition(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < 1e-10f;

    // Výška terénu pod bodom (pre pohyb bez NavMeshu).
    public static bool TryGetGroundHeight(Vector3 position, out float height)
    {
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            Vector3 origin = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            if (position.x < origin.x || position.x > origin.x + size.x ||
                position.z < origin.z || position.z > origin.z + size.z)
                continue;

            height = terrain.SampleHeight(position) + origin.y;
            return true;
        }

        height = position.y;
        return false;
    }

    private void OnDestroy()
    {
        foreach (var pair in _originalTrees)
            if (pair.Key != null)
                pair.Key.SetTreeInstances(pair.Value, false);
        _originalTrees.Clear();

        if (_instance == this) _instance = null;
    }
}
