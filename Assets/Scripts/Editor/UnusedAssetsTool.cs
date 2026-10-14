using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Nájde súbory v Assets, ktoré hra nepoužíva: nie sú závislosťou scén v Build Settings
// ani priečinkov Resources. Unity číta aj binárne súbory (TerrainData, LightingData),
// preto je to spoľahlivejšie ako hľadanie odkazov v texte.
//   Tools > Project > Find Unused Assets   – zapíše zoznam do UnusedAssets.txt (v koreni projektu)
//   Tools > Project > Delete Unused Assets – po potvrdení zmaže súbory zo zoznamu
// Skripty, shadery, pluginy a editorové súbory sa nikdy neoznačia – používajú sa aj bez odkazu zo scény.
public static class UnusedAssetsTool
{
    private const string ReportPath = "UnusedAssets.txt";

    private static readonly string[] KeepExtensions =
    {
        ".cs", ".asmdef", ".asmref", ".shader", ".cginc", ".hlsl", ".compute", ".shadergraph", ".shadersubgraph",
        ".dll", ".so", ".jar", ".aar", ".a", ".rsp", ".xml", ".json", ".md", ".txt"
    };

    private static readonly string[] KeepFolders =
    {
        "/Editor/", "/Resources/", "/Plugins/", "/StreamingAssets/", "/Gizmos/",
        "Assets/TextMesh Pro/", "Assets/Firebase/", "Assets/ExternalDependencyManager/", "Assets/Settings/"
    };

    [MenuItem("Tools/Project/Find Unused Assets")]
    public static void Find()
    {
        List<string> unused = FindUnused(out long bytes);

        var sb = new StringBuilder();
        sb.AppendLine($"# {unused.Count} unused files, {bytes / 1048576f:0.0} MB");
        sb.AppendLine("# Roots: scenes in Build Settings, Resources folders, project settings.");
        foreach (var group in unused.GroupBy(TopFolder).OrderByDescending(g => g.Sum(Size)))
            sb.AppendLine($"# {group.Sum(Size) / 1048576f,9:0.0} MB {group.Count(),5}  {group.Key}");
        sb.AppendLine();
        foreach (string path in unused) sb.AppendLine(path);
        File.WriteAllText(ReportPath, sb.ToString());

        Debug.Log($"Unused assets: {unused.Count} files ({bytes / 1048576f:0.0} MB). List written to {Path.GetFullPath(ReportPath)}");
        EditorUtility.RevealInFinder(ReportPath);
    }

    [MenuItem("Tools/Project/Delete Unused Assets")]
    public static void Delete()
    {
        if (!File.Exists(ReportPath))
        {
            EditorUtility.DisplayDialog("Delete Unused Assets", "Run Tools > Project > Find Unused Assets first.", "OK");
            return;
        }

        // zmaže sa len to, čo je v zozname (riadky, ktoré z neho ručne odstrániš, ostanú)
        var paths = File.ReadAllLines(ReportPath)
            .Where(l => l.StartsWith("Assets/") && File.Exists(l))
            .ToList();
        if (!EditorUtility.DisplayDialog("Delete Unused Assets",
                $"Delete {paths.Count} files listed in {ReportPath}?\nThis cannot be undone outside of Git.", "Delete", "Cancel"))
            return;

        var failed = new List<string>();
        AssetDatabase.DeleteAssets(paths.ToArray(), failed);
        DeleteEmptyFolders("Assets");
        AssetDatabase.Refresh();
        Debug.Log($"Deleted {paths.Count - failed.Count} unused files" + (failed.Count > 0 ? $", failed: {string.Join(", ", failed)}" : "."));
    }

    private static List<string> FindUnused(out long bytes)
    {
        var roots = new List<string>();
        roots.AddRange(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path));
        roots.AddRange(AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && p.Contains("/Resources/")));
        // render pipeline, input actions a iné nastavenia projektu
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")
                     .Concat(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")))
        {
            var so = new SerializedObject(asset);
            var it = so.GetIterator();
            while (it.Next(true))
                if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue != null)
                    roots.Add(AssetDatabase.GetAssetPath(it.objectReferenceValue));
        }
        roots.AddRange(PlayerSettings.GetPreloadedAssets().Where(a => a != null).Select(AssetDatabase.GetAssetPath));
        // nastavenia balíkov uložené v Build Settings (napr. vstupné akcie Input Systemu)
        foreach (string key in EditorBuildSettings.GetConfigObjectNames())
            if (EditorBuildSettings.TryGetConfigObject(key, out Object config) && config != null)
                roots.Add(AssetDatabase.GetAssetPath(config));

        var used = new HashSet<string>(AssetDatabase.GetDependencies(roots.Where(r => !string.IsNullOrEmpty(r)).ToArray(), true));

        var unused = new List<string>();
        bytes = 0;
        foreach (string path in AssetDatabase.GetAllAssetPaths())
        {
            if (!path.StartsWith("Assets/") || AssetDatabase.IsValidFolder(path) || used.Contains(path)) continue;
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (KeepExtensions.Contains(ext) || KeepFolders.Any(f => (path + "/").Contains(f) || path.StartsWith(f))) continue;
            // glTF model načítava .bin a textúry podľa názvu súboru, nie cez odkaz Unity – nechaj celý jeho priečinok
            if (IsNextToUsedGltf(path, used)) continue;
            unused.Add(path);
            bytes += Size(path);
        }
        unused.Sort();
        return unused;
    }

    private static bool IsNextToUsedGltf(string path, HashSet<string> used)
    {
        for (string dir = Path.GetDirectoryName(path)?.Replace('\\', '/'); !string.IsNullOrEmpty(dir) && dir != "Assets";
             dir = Path.GetDirectoryName(dir)?.Replace('\\', '/'))
        {
            if (Directory.GetFiles(dir, "*.gltf").Concat(Directory.GetFiles(dir, "*.glb"))
                .Any(g => used.Contains(g.Replace('\\', '/'))))
                return true;
        }
        return false;
    }

    private static long Size(string path) => File.Exists(path) ? new FileInfo(path).Length : 0;

    private static string TopFolder(string path)
    {
        string[] parts = path.Split('/');
        return parts.Length > 3 ? string.Join("/", parts.Take(3)) : string.Join("/", parts.Take(parts.Length - 1));
    }

    private static void DeleteEmptyFolders(string folder)
    {
        foreach (string sub in AssetDatabase.GetSubFolders(folder))
            DeleteEmptyFolders(sub);
        if (folder != "Assets" && Directory.GetFileSystemEntries(folder).Length == 0)
            AssetDatabase.DeleteAsset(folder);
    }
}
