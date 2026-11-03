// Plog Origins · Cañón Ámbar (Región 2). Colocar en Assets/Editor.
// Menús: Plog > 2. Preparar kit Cañón Ámbar · 2b. Reparar materiales del kit · 3. Armar escenario Cañón Ámbar · 4. Fondo y niebla del cañón
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PlogCanyonSetup
{
    const string MatFolder = "Assets/Materials/CanonAmbar";
    const string RootName = "Escenario Canon Ambar";

    struct Mat
    {
        public string name, hex; public float smooth, emission;
        public Mat(string n, string h, float s, float e) { name = n; hex = h; smooth = s; emission = e; }
    }

    static readonly Mat[] Palette =
    {
        new Mat("CA_RocaClara", "#F2A65A", 0.0f, 0.0f),
        new Mat("CA_RocaMedia", "#D9753B", 0.0f, 0.0f),
        new Mat("CA_RocaOscura", "#A84A2C", 0.0f, 0.0f),
        new Mat("CA_RocaRoja", "#C2452D", 0.0f, 0.0f),
        new Mat("CA_Arena", "#F4D58D", 0.0f, 0.0f),
        new Mat("CA_ArenaOscura", "#D9B26A", 0.0f, 0.0f),
        new Mat("CA_Pasto", "#C9B84A", 0.0f, 0.0f),
        new Mat("CA_PastoVerde", "#7FB04A", 0.0f, 0.0f),
        new Mat("CA_Madera", "#8F5B3A", 0.0f, 0.0f),
        new Mat("CA_MaderaOscura", "#5E3B28", 0.0f, 0.0f),
        new Mat("CA_MaderaClara", "#C08A55", 0.0f, 0.0f),
        new Mat("CA_Cactus", "#3FA672", 0.0f, 0.0f),
        new Mat("CA_CactusOscuro", "#2C8059", 0.0f, 0.0f),
        new Mat("CA_Flor", "#F0527E", 0.0f, 0.0f),
        new Mat("CA_TelaRoja", "#D8473C", 0.0f, 0.0f),
        new Mat("CA_TelaCrema", "#F1E3C2", 0.0f, 0.0f),
        new Mat("CA_TelaTeal", "#2FA7A0", 0.0f, 0.0f),
        new Mat("CA_Piedra", "#A3A0AE", 0.0f, 0.0f),
        new Mat("CA_PiedraOscura", "#6E6B7B", 0.0f, 0.0f),
        new Mat("CA_Metal", "#5B6270", 0.3f, 0.0f),
        new Mat("CA_Oscuro", "#2A1D1A", 0.0f, 0.0f),
        new Mat("CA_Fuego", "#FF7A1F", 0.0f, 1.6f),
        new Mat("CA_FuegoClaro", "#FFD45C", 0.0f, 1.8f),
        new Mat("CA_Cristal", "#FFB020", 0.0f, 1.2f),
        new Mat("CA_CristalClaro", "#FFE08A", 0.0f, 1.4f),
        new Mat("CA_Agua", "#4FC3E0", 0.7f, 0.0f),
        new Mat("CA_Hoja", "#3FB36B", 0.0f, 0.0f)
    };

    // Detalles pequeños que no necesitan collider
    static readonly string[] NoCollider =
    {
        "CA_Mata_Pasto", "CA_Arbusto_Seco", "CA_Roca_Pequena", "CA_Cactus_Pala", "CA_Letrero", "CA_Fogata", "CA_Cactus_Barril", "CA_Barril", "CA_Suelo_Fondo"
    };

    // Usa el shader del pipeline activo (URP), así nunca sale magenta
    static Shader PickShader()
    {
        var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        if (rp != null && rp.defaultMaterial != null && rp.defaultMaterial.shader != null) return rp.defaultMaterial.shader;
        var s = Shader.Find("Universal Render Pipeline/Lit");
        return s != null ? s : Shader.Find("Standard");
    }

    static Dictionary<string, Material> EnsureMaterials()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(MatFolder)) AssetDatabase.CreateFolder("Assets/Materials", "CanonAmbar");
        var shader = PickShader();
        var mats = new Dictionary<string, Material>();
        foreach (var p in Palette)
        {
            var path = MatFolder + "/" + p.name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            ColorUtility.TryParseHtmlString(p.hex, out var c);
            m.shader = shader;
            m.SetColor("_BaseColor", c); m.SetColor("_Color", c);
            m.SetFloat("_Smoothness", p.smooth); m.SetFloat("_Metallic", 0f);
            m.SetFloat("_SpecularHighlights", 0f); m.SetFloat("_EnvironmentReflections", 0f);
            m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            if (p.emission > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * p.emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }
            EditorUtility.SetDirty(m);
            mats[p.name] = m;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[Plog] Materiales del cañón listos con shader: " + shader.name);
        return mats;
    }

    static Dictionary<string, GameObject> FindModels()
    {
        var d = new Dictionary<string, GameObject>();
        foreach (var guid in AssetDatabase.FindAssets("CA_ t:Model"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var n = Path.GetFileNameWithoutExtension(path);
            if (!n.StartsWith("CA_")) continue;
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go) d[n] = go;
        }
        return d;
    }

    // Nombres de material en el mismo orden en que el OBJ declara sus submallas
    static List<string> ReadUsemtl(string assetPath)
    {
        var l = new List<string>();
        if (string.IsNullOrEmpty(assetPath) || !assetPath.ToLower().EndsWith(".obj") || !File.Exists(assetPath)) return l;
        foreach (var line in File.ReadLines(assetPath))
            if (line.StartsWith("usemtl ")) l.Add(line.Substring(7).Trim());
        return l;
    }

    static void ApplyMaterials(GameObject inst, string assetPath, Dictionary<string, Material> mats)
    {
        var names = ReadUsemtl(assetPath);
        foreach (var rend in inst.GetComponentsInChildren<MeshRenderer>())
        {
            var cur = rend.sharedMaterials;
            var arr = new Material[cur.Length];
            for (int i = 0; i < cur.Length; i++)
            {
                string n = i < names.Count ? names[i] : (cur[i] != null ? cur[i].name : "");
                arr[i] = mats.TryGetValue(n, out var mm) ? mm : cur[i];
            }
            rend.sharedMaterials = arr;
        }
    }

    static int PrepareImporters(Dictionary<string, Material> mats)
    {
        int count = 0;
        foreach (var guid in AssetDatabase.FindAssets("CA_ t:Model"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!Path.GetFileNameWithoutExtension(path).StartsWith("CA_")) continue;
            var imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp == null) continue;
            imp.globalScale = 1f;
            imp.importNormals = ModelImporterNormals.Import;
            imp.importTangents = ModelImporterTangents.None;
            imp.isReadable = false;
            imp.importCameras = false; imp.importLights = false;
            var old = new List<AssetImporter.SourceAssetIdentifier>(imp.GetExternalObjectMap().Keys);
            foreach (var k in old) imp.RemoveRemap(k);
            foreach (var kv in mats)
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
            imp.SaveAndReimport();
            count++;
        }
        return count;
    }

    [MenuItem("Plog/2. Preparar kit Cañón Ámbar")]
    public static void PrepareKit()
    {
        var mats = EnsureMaterials();
        int count = PrepareImporters(mats);
        Debug.Log("[Plog] Kit Cañón Ámbar: " + mats.Count + " materiales, " + count + " modelos configurados.");
        if (count == 0) Debug.LogWarning("[Plog] No encontré modelos que empiecen con CA_. Copia la carpeta Props dentro de Assets y vuelve a ejecutar.");
    }

    [MenuItem("Plog/2b. Reparar materiales del kit")]
    public static void RepairMaterials()
    {
        var mats = EnsureMaterials();
        PrepareImporters(mats);
        var root = GameObject.Find(RootName);
        int fixedCount = 0;
        if (root != null)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == root.transform || t.parent == null || t.parent.parent != root.transform) continue;
                var src = PrefabUtility.GetCorrespondingObjectFromOriginalSource(t.gameObject);
                if (src == null) continue;
                ApplyMaterials(t.gameObject, AssetDatabase.GetAssetPath(src), mats);
                fixedCount++;
            }
            EditorUtility.SetDirty(root);
        }
        Debug.Log("[Plog] Materiales reparados. Objetos de la escena corregidos: " + fixedCount);
    }

    [MenuItem("Plog/3. Armar escenario Cañón Ámbar")]
    public static void BuildScene()
    {
        var models = FindModels();
        if (models.Count == 0)
        {
            EditorUtility.DisplayDialog("Plog", "No encontré modelos CA_*. Copia la carpeta Props dentro de Assets y ejecuta primero 'Plog > 2. Preparar kit Cañón Ámbar'.", "OK");
            return;
        }
        var mats = EnsureMaterials();

        var old = GameObject.Find(RootName);
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("Plog", "Ya existe '" + RootName + "'. ¿Reemplazarlo?", "Reemplazar", "Cancelar")) return;
            Undo.DestroyObjectImmediate(old);
        }

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Armar escenario Cañón Ámbar");
        var groups = new Dictionary<string, Transform>();
        var missing = new HashSet<string>();
        int placed = 0, colliders = 0;

        foreach (var r in CanonLayoutData.All)
        {
            if (!models.TryGetValue(r.name, out var asset)) { missing.Add(r.name); continue; }
            if (!groups.TryGetValue(r.group, out var parent))
            {
                var g = new GameObject(r.group);
                g.transform.SetParent(root.transform, false);
                groups[r.group] = parent = g.transform;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.transform.position = new Vector3(r.x, r.y, r.z);
            go.transform.rotation = Quaternion.Euler(0f, r.ry, 0f);
            go.transform.localScale = new Vector3(r.sx, r.sy, r.sz);
            foreach (var t in go.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
            ApplyMaterials(go, AssetDatabase.GetAssetPath(asset), mats);

            bool skip = r.group == "Fondo";
            foreach (var s in NoCollider) if (r.name.StartsWith(s)) skip = true;
            if (!skip)
            {
                var mf = go.GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.sharedMesh != null && mf.GetComponent<Collider>() == null)
                {
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    colliders++;
                }
            }
            placed++;
        }

        Selection.activeGameObject = root;
        Debug.Log("[Plog] Escenario armado: " + placed + " objetos, " + colliders + " colliders.");
        if (missing.Count > 0) Debug.LogWarning("[Plog] Faltan modelos: " + string.Join(", ", missing));
        Debug.Log("[Plog] Siguiente paso: hornea el NavMesh (NavMesh Surface > Bake) y coloca al Player en (0, 0.5, -26).");
    }

    [MenuItem("Plog/4. Fondo y niebla del cañón")]
    public static void SkyAndFog()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam, "Fondo cañón");
            cam.clearFlags = CameraClearFlags.SolidColor;
            ColorUtility.TryParseHtmlString("#F6C58B", out var bg);
            cam.backgroundColor = bg;
        }
        ColorUtility.TryParseHtmlString("#F6C58B", out var fog);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = fog;
        RenderSettings.fogStartDistance = 55f;
        RenderSettings.fogEndDistance = 130f;
        ColorUtility.TryParseHtmlString("#FFE2B8", out var amb);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = amb;
        Debug.Log("[Plog] Fondo cálido y niebla aplicados (cámara + Lighting).");
    }
}
#endif