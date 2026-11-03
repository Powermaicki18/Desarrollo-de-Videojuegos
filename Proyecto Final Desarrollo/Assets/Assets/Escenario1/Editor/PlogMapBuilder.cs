// Plog Origins · Constructor del Mapa 1 grande (isla de pasto). Colocar en Assets/Editor.
// Menú: Plog > Construir Mapa 1 grande
// Construye el suelo (isla rodeada de bosque, con río y puente), el perímetro y toda la decoración
// con los props de tu carpeta de modelos, siguiendo la composición del concept art. Medidas de referencia: 91 x 61 m.
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;

public class PlogMap1Builder : EditorWindow
{
    class Prop
    {
        public GameObject asset;
        public string role, name;
        public float radius, pivotY;
        public Vector3 size, center, baseScale;
    }

    const string RootName = "Mapa1_Grande";
    const string DefaultDict =
        "ignorar=dead,muerto,seco\n" +
        "puerta=puerta,portal,arco,gate,arch\n" +
        "muro=muro,pared,wall\n" +
        "antorcha=antorcha,torch\n" +
        "estandarte=estandarte,bandera,banner,flag\n" +
        "torre=torre,tower,ruina,ruin,columna,column,pilar\n" +
        "carpa=carpa,tienda,tent\n" +
        "fuente=fuente,fountain,plaza,altar\n" +
        "menhir=menhir,monolito,standing,piedraerguida\n" +
        "caja=caja,crate,box\n" +
        "barril=barril,barrel\n" +
        "valla=valla,fence\n" +
        "pastoalto=pastoalto,tallgrass,trigo,wheat,cultivo,maiz\n" +
        "tronco=tronco,log,tocon,stump\n" +
        "hongo=hongo,mushroom,seta\n" +
        "flor=flor,flower\n" +
        "linterna=linterna,lantern,lampara,lamp,farol\n" +
        "arbusto=arbusto,bush,shrub,seto\n" +
        "pino=pino,pine\n" +
        "arbolgrande=treebig,arbolgrande,bigtree\n" +
        "arbol=arbol,tree,roble,oak\n" +
        "roca=roca,rock,piedra,stone,boulder\n" +
        "pasto=pasto,grass,hierba,tuft";
    const float RefW = 91f, RefD = 61f; // medidas con las que está diseñado el plano

    // ---------- ajustes ----------
    public DefaultAsset kitFolder;
    public Transform player;
    public Vector2 center = Vector2.zero;
    public float width = 91f, depth = 61f;
    public float propScale = 1f;
    public float treeDensity = 1f, detailDensity = 1f;
    public int seed = 11;
    public bool buildGround = true, addColliders = true, makeStatic = true;
    public string excludeNames = "suelo,terreno,ground,floor,agua,water,puente,bridge,personaje,player,enemy,enemigo,goro,nox,tuko,zapo,mogo,escenario";
    [TextArea(12, 24)]
    public string roleDict = DefaultDict;


    public Color grassColor = new Color(0.36f, 0.72f, 0.25f);
    public Color soilColor = new Color(0.42f, 0.26f, 0.16f);
    public Color waterColor = new Color(0.16f, 0.72f, 0.84f);
    public Color forestColor = new Color(0.17f, 0.44f, 0.17f);
    public float forestDepth = 9f;
    public Color pathColor = new Color(0.80f, 0.60f, 0.35f);
    public Color woodColor = new Color(0.60f, 0.40f, 0.22f);
    public Color stoneColor = new Color(0.56f, 0.61f, 0.66f);
    public Color runeColor = new Color(0.20f, 0.90f, 0.88f);

    // ---------- estado ----------
    readonly Dictionary<string, List<Prop>> roles = new Dictionary<string, List<Prop>>();
    readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();
    readonly List<Vector3> placed = new List<Vector3>();
    readonly HashSet<string> missing = new HashSet<string>();
    Transform root, gProps, gGround;
    float kx = 1f, kz = 1f;
    float avoidK = 0.7f; // 0.7 = separados, 0.4 = copas que se tocan
    int count;
    Vector2 scroll;
    string info = "";

    static readonly Dictionary<string, string[]> Fallback = new Dictionary<string, string[]>
    {
        { "pastoalto", new[] { "pasto", "arbusto" } }, { "hongo", new[] { "flor" } }, { "flor", new[] { "pasto" } },
        { "antorcha", new[] { "linterna" } }, { "linterna", new[] { "antorcha" } }, { "menhir", new[] { "roca" } },
        { "torre", new[] { "muro" } }, { "barril", new[] { "caja" } }, { "caja", new[] { "barril" } }, { "puerta", new[] { "muro" } },
        { "tronco", new[] { "roca" } }, { "arbolgrande", new[] { "arbol" } }, { "arbol", new[] { "arbolgrande" } }, { "carpa", new[] { "caja" } }, { "arbusto", new[] { "pasto" } }, { "estandarte", new[] { "antorcha", "linterna" } }
    };
    static readonly HashSet<string> NoNavRoles = new HashSet<string> { "flor", "hongo", "pasto", "pastoalto", "estandarte", "antorcha", "linterna", "valla" };
    static readonly HashSet<string> BoxRoles = new HashSet<string> { "roca", "caja", "barril", "muro", "torre", "carpa", "menhir", "tronco", "puerta", "fuente" };

    [MenuItem("Plog/Construir Mapa 1 grande")]
    static void Open() { GetWindow<PlogMap1Builder>("Mapa 1 Plog").minSize = new Vector2(400, 560); }

    void OnGUI()
    {
        var so = new SerializedObject(this);
        so.Update();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("1. Props", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("kitFolder"), new GUIContent("Carpeta de modelos del Escenario 1"));
        EditorGUILayout.PropertyField(so.FindProperty("player"), new GUIContent("Player (opcional)"));
        EditorGUILayout.PropertyField(so.FindProperty("excludeNames"), new GUIContent("Excluir nombres"));
        EditorGUILayout.PropertyField(so.FindProperty("roleDict"), new GUIContent("Palabras por rol (rol=palabras)"));
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("2. Tamaño y cantidad", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("center"), new GUIContent("Centro del mapa (X, Z)"));
        EditorGUILayout.PropertyField(so.FindProperty("width"), new GUIContent("Ancho (X)"));
        EditorGUILayout.PropertyField(so.FindProperty("depth"), new GUIContent("Fondo (Z)"));
        EditorGUILayout.PropertyField(so.FindProperty("propScale"), new GUIContent("Escala de todos los props"));
        EditorGUILayout.PropertyField(so.FindProperty("treeDensity"), new GUIContent("Densidad de árboles"));
        EditorGUILayout.PropertyField(so.FindProperty("detailDensity"), new GUIContent("Densidad de detalle"));
        EditorGUILayout.PropertyField(so.FindProperty("seed"), new GUIContent("Semilla"));
        EditorGUILayout.PropertyField(so.FindProperty("buildGround"), new GUIContent("Crear suelo, agua y puente"));
        EditorGUILayout.PropertyField(so.FindProperty("addColliders"), new GUIContent("Colliders a rocas, cajas, árboles"));
        EditorGUILayout.PropertyField(so.FindProperty("makeStatic"), new GUIContent("Marcar Static"));
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("3. Colores", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("grassColor"), new GUIContent("Pasto"));
        EditorGUILayout.PropertyField(so.FindProperty("soilColor"), new GUIContent("Tierra (borde de la isla)"));
        EditorGUILayout.PropertyField(so.FindProperty("waterColor"), new GUIContent("Agua del río"));
        EditorGUILayout.PropertyField(so.FindProperty("forestColor"), new GUIContent("Suelo del bosque y fondo"));
        EditorGUILayout.PropertyField(so.FindProperty("forestDepth"), new GUIContent("Profundidad del bosque (m)"));
        EditorGUILayout.PropertyField(so.FindProperty("pathColor"), new GUIContent("Tierra de caminos"));
        EditorGUILayout.PropertyField(so.FindProperty("woodColor"), new GUIContent("Madera (puente)"));
        EditorGUILayout.PropertyField(so.FindProperty("stoneColor"), new GUIContent("Piedra (plataformas)"));
        EditorGUILayout.PropertyField(so.FindProperty("runeColor"), new GUIContent("Runas"));
        so.ApplyModifiedProperties();

        EditorGUILayout.Space();
        if (GUILayout.Button("Restaurar palabras por rol (recomendado)")) { roleDict = DefaultDict; GUI.FocusControl(null); }
        if (GUILayout.Button("Listar props del kit y sus roles")) ListProps();
        GUI.backgroundColor = new Color(0.6f, 1f, 0.7f);
        if (GUILayout.Button("CONSTRUIR MAPA 1", GUILayout.Height(36))) Build();
        GUI.backgroundColor = Color.white;
        if (GUILayout.Button("Hornear NavMesh del mapa")) BakeNav();
        if (GUILayout.Button("Fondo de cámara = color del bosque")) ApplySky();
        if (GUILayout.Button("Borrar mapa generado")) { ClearExisting(); info = "Mapa borrado."; }
        if (!string.IsNullOrEmpty(info)) EditorGUILayout.HelpBox(info, MessageType.Info);
        EditorGUILayout.EndScrollView();
    }

    // ====================== utilidades ======================
    static string Norm(string s)
    {
        var d = s.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in d)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Replace(" ", "").Replace("_", "").Replace("-", "").Replace(".", "");
    }

    static bool AnyWord(string normName, string csv)
    {
        foreach (var w in csv.Split(','))
        {
            var t = Norm(w);
            if (t.Length > 0 && normName.Contains(t)) return true;
        }
        return false;
    }

    List<KeyValuePair<string, string>> ParseDict()
    {
        var l = new List<KeyValuePair<string, string>>();
        foreach (var line in roleDict.Split('\n'))
        {
            var t = line.Trim();
            if (t.Length == 0 || t.StartsWith("#")) continue;
            int i = t.IndexOf('=');
            if (i <= 0) continue;
            l.Add(new KeyValuePair<string, string>(t.Substring(0, i).Trim().ToLowerInvariant(), t.Substring(i + 1)));
        }
        return l;
    }

    List<Prop> LoadProps(out List<string> unassigned)
    {
        unassigned = new List<string>();
        var all = new List<Prop>();
        roles.Clear();
        if (kitFolder == null) return all;
        var dict = ParseDict();
        var folder = AssetDatabase.GetAssetPath(kitFolder);
        foreach (var guid in AssetDatabase.FindAssets("t:GameObject", new[] { folder }))
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset == null) continue;
            var nn = Norm(asset.name);
            if (AnyWord(nn, excludeNames)) continue;
            if (asset.GetComponentInChildren<Renderer>() == null) continue;

            var tmp = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            tmp.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var rs = tmp.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            var p = new Prop
            {
                asset = asset, name = asset.name, size = b.size, center = b.center,
                radius = 0.5f * Mathf.Max(b.size.x, b.size.z), pivotY = -b.min.y, baseScale = asset.transform.localScale
            };
            DestroyImmediate(tmp);
            if (Mathf.Max(p.size.x, p.size.z) > 18f || p.size.y < 0.02f) continue;

            foreach (var kv in dict)
            {
                if (AnyWord(nn, kv.Value)) { p.role = kv.Key; break; }
            }
            if (p.role == null) { unassigned.Add(asset.name); continue; }
            if (!roles.ContainsKey(p.role)) roles[p.role] = new List<Prop>();
            roles[p.role].Add(p);
            all.Add(p);
        }
        return all;
    }

    void ListProps()
    {
        var all = LoadProps(out var un);
        var sb = new StringBuilder("Props reconocidos (" + all.Count + "):\n");
        foreach (var kv in roles)
        {
            sb.Append("  [" + kv.Key + "] ");
            foreach (var p in kv.Value) sb.Append(p.name + " (" + p.size.x.ToString("0.0") + "x" + p.size.y.ToString("0.0") + "x" + p.size.z.ToString("0.0") + ")  ");
            sb.Append("\n");
        }
        sb.Append("Sin rol (se ignoran): " + (un.Count == 0 ? "ninguno" : string.Join(", ", un)));
        info = sb.ToString();
        Debug.Log("[Plog] " + info);
    }

    void ClearExisting()
    {
        var old = GameObject.Find(RootName);
        if (old != null) Undo.DestroyObjectImmediate(old);
    }

    void ApplySky()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam, "Fondo");
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = forestColor;
        }
        RenderSettings.fog = false;
        info = "Fondo de cámara aplicado.";
    }

    // ====================== materiales y primitivas ======================
    static Shader PickShader()
    {
        var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        if (rp != null && rp.defaultMaterial != null && rp.defaultMaterial.shader != null) return rp.defaultMaterial.shader;
        var s = Shader.Find("Universal Render Pipeline/Lit");
        return s != null ? s : Shader.Find("Standard");
    }

    Material GetMat(string name, Color c, float emission)
    {
        if (matCache.TryGetValue(name, out var cached) && cached != null) return cached;
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Mapa1")) AssetDatabase.CreateFolder("Assets/Materials", "Mapa1");
        var path = "Assets/Materials/Mapa1/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        var sh = PickShader();
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
        m.shader = sh;
        m.SetColor("_BaseColor", c); m.SetColor("_Color", c);
        m.SetFloat("_Smoothness", 0f); m.SetFloat("_Metallic", 0f);
        m.SetFloat("_SpecularHighlights", 0f); m.SetFloat("_EnvironmentReflections", 0f);
        if (emission > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }
        EditorUtility.SetDirty(m);
        matCache[name] = m;
        return m;
    }

    GameObject Prim(PrimitiveType t, string name, Transform parent, Vector3 pos, Vector3 scale, Material m, bool keepCollider)
    {
        var go = GameObject.CreatePrimitive(t);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        if (!keepCollider) { var c = go.GetComponent<Collider>(); if (c != null) DestroyImmediate(c); }
        if (makeStatic) go.isStatic = true;
        return go;
    }

    void SetNavProp(GameObject go, string prop, object value)
    {
        try
        {
            var t = System.Type.GetType("Unity.AI.Navigation.NavMeshModifier, Unity.AI.Navigation");
            if (t == null) return;
            var mod = go.GetComponent(t);
            if (mod == null) mod = go.AddComponent(t);
            t.GetProperty("overrideArea").SetValue(mod, prop == "area", null);
            if (prop == "area") t.GetProperty("area").SetValue(mod, value, null);
            if (prop == "ignore") t.GetProperty("ignoreFromBuild").SetValue(mod, true, null);
        }
        catch (System.Exception) { }
    }

    // ====================== coordenadas ======================
    Vector2 Wp(float rx, float rz) { return new Vector2(center.x + rx * kx, center.y + rz * kz); }

    GameObject Box(string name, Material m, float x0, float x1, float y0, float y1, float z0, float z1, bool collider)
    {
        var p = Wp((x0 + x1) / 2f, (z0 + z1) / 2f);
        return Prim(PrimitiveType.Cube, name, gGround, new Vector3(p.x, (y0 + y1) / 2f, p.y), new Vector3((x1 - x0) * kx, y1 - y0, (z1 - z0) * kz), m, collider);
    }

    // ====================== props ======================
    Prop Pick(string role)
    {
        if (roles.TryGetValue(role, out var l) && l.Count > 0) return l[Random.Range(0, l.Count)];
        if (Fallback.TryGetValue(role, out var alts))
            foreach (var a in alts) if (roles.TryGetValue(a, out var l2) && l2.Count > 0) return l2[Random.Range(0, l2.Count)];
        missing.Add(role);
        return null;
    }

    GameObject Put(string role, float rx, float rz, float ry, float s, bool avoid)
    {
        var p = Pick(role);
        if (p == null) return null;
        s *= propScale;
        var pos = Wp(rx, rz);
        float r = p.radius * s;
        if (avoid)
            foreach (var q in placed)
                if (Vector2.Distance(pos, new Vector2(q.x, q.y)) < (r + q.z) * avoidK) return null;

        var go = (GameObject)PrefabUtility.InstantiatePrefab(p.asset, gProps);
        go.transform.position = new Vector3(pos.x, p.pivotY * s, pos.y);
        go.transform.rotation = Quaternion.Euler(0f, ry, 0f);
        go.transform.localScale = p.baseScale * s;
        placed.Add(new Vector3(pos.x, pos.y, r));

        if (addColliders && go.GetComponentInChildren<Collider>() == null)
        {
            bool tree = (role == "arbol" || role == "arbolgrande");
            if (tree || BoxRoles.Contains(role))
            {
                var bc = go.AddComponent<BoxCollider>();
                var sz = tree ? new Vector3(Mathf.Max(0.4f, p.size.x * 0.22f), p.size.y, Mathf.Max(0.4f, p.size.z * 0.22f)) : p.size * 0.92f;
                if (!tree) sz.y = p.size.y;
                var bs = p.baseScale;
                bc.size = new Vector3(sz.x / bs.x, sz.y / bs.y, sz.z / bs.z);
                bc.center = new Vector3(p.center.x / bs.x, p.center.y / bs.y, p.center.z / bs.z);
            }
        }
        if (makeStatic) foreach (var t in go.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
        if (NoNavRoles.Contains(role)) SetNavProp(go, "ignore", null);
        count++;
        return go;
    }

    // fila de props entre dos puntos (coordenadas de referencia). spacing <= 0 usa el largo del prop
    void Row(string role, float x0, float z0, float x1, float z1, float spacing, float jitter, float smin, float smax, float ry, bool avoid)
    {
        var p = Pick(role);
        if (p == null) return;
        float len = Vector2.Distance(new Vector2(x0, z0), new Vector2(x1, z1));
        float sp = spacing > 0f ? spacing : Mathf.Max(0.5f, Mathf.Max(p.size.x, p.size.z) * 0.97f * (smin + smax) * 0.5f * propScale / Mathf.Max(kx, 0.01f));
        int n = Mathf.Max(1, Mathf.RoundToInt(len / sp));
        for (int i = 0; i < n; i++)
        {
            float t = (i + 0.5f) / n;
            float x = Mathf.Lerp(x0, x1, t) + Random.Range(-jitter, jitter);
            float z = Mathf.Lerp(z0, z1, t) + Random.Range(-jitter, jitter);
            Put(role, x, z, ry, Random.Range(smin, smax), avoid);
        }
    }

    void TreeRow(float x0, float z0, float x1, float z1, float spacing, float smin, float smax)
    {
        float prevK = avoidK;
        avoidK = 0.42f;
        float len = Vector2.Distance(new Vector2(x0, z0), new Vector2(x1, z1));
        int n = Mathf.Max(1, Mathf.RoundToInt(len / (spacing / Mathf.Max(0.3f, treeDensity))));
        for (int i = 0; i < n; i++)
        {
            float t = (i + 0.5f) / n;
            float x = Mathf.Lerp(x0, x1, t) + Random.Range(-0.9f, 0.9f);
            float z = Mathf.Lerp(z0, z1, t) + Random.Range(-0.9f, 0.9f);
            float v = Random.value;
            if (v < 0.34f) Put("arbolgrande", x, z, Random.Range(0f, 360f), Random.Range(smin * 0.8f, smax * 0.9f), true);
            else if (v < 0.80f) Put("arbol", x, z, Random.Range(0f, 360f), Random.Range(smin, smax), true);
            else Put("arbusto", x, z, Random.Range(0f, 360f), Random.Range(0.9f, 1.4f), true);
        }
        avoidK = prevK;
    }

    void FillGrass(float x0, float z0, float x1, float z1)
    {
        for (float x = x0 + 0.8f; x < x1; x += 1.6f)
            for (float z = z0 + 0.8f; z < z1; z += 1.6f)
                Put("pastoalto", x + Random.Range(-0.3f, 0.3f), z + Random.Range(-0.3f, 0.3f), Random.Range(0f, 360f), Random.Range(0.9f, 1.2f), false);
    }

    // zonas donde no se esparce detalle
    bool NoGo(float x, float z)
    {
        if (z < -20f) return true; // agua y orilla
        float[,] circles = { { 0, 1.5f, 8f }, { -11.7f, 2.6f, 3f }, { 10.7f, 8f, 3f }, { 0, -12.9f, 3f }, { 0, 14f, 8.5f }, { -39.5f, 3f, 6f }, { 39.5f, 3f, 6f } };
        for (int i = 0; i < circles.GetLength(0); i++)
            if (Vector2.Distance(new Vector2(x, z), new Vector2(circles[i, 0], circles[i, 1])) < circles[i, 2]) return true;
        if (z > 22f) return true; // muralla norte
        return false;
    }

    void Scatter(string role, int n, float x0, float z0, float x1, float z1, float smin, float smax, bool avoid, bool ignoreNoGo = false)
    {
        n = Mathf.RoundToInt(n * detailDensity);
        int done = 0, tries = 0;
        while (done < n && tries < n * 30)
        {
            tries++;
            float x = Random.Range(x0, x1), z = Random.Range(z0, z1);
            if (!ignoreNoGo && NoGo(x, z)) continue;
            if (Put(role, x, z, Random.Range(0f, 360f), Random.Range(smin, smax), avoid) != null) done++;
        }
    }

    // ====================== construcción ======================
    void Build()
    {
        var all = LoadProps(out var unassigned);
        if (all.Count == 0)
        {
            EditorUtility.DisplayDialog("Plog", "No encontré props. Arrastra la carpeta con los modelos del Escenario 1 en 'Carpeta de modelos' y revisa 'Palabras por rol'.", "OK");
            return;
        }
        ClearExisting();
        Random.InitState(seed);
        placed.Clear(); missing.Clear(); matCache.Clear(); count = 0;
        kx = width / RefW; kz = depth / RefD;

        var rootGo = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(rootGo, "Construir Mapa 1");
        root = rootGo.transform;
        gGround = new GameObject("Suelo").transform; gGround.SetParent(root, false);
        gProps = new GameObject("Props").transform; gProps.SetParent(root, false);

        EditorUtility.DisplayProgressBar("Plog", "Suelo...", 0.1f);
        if (buildGround) BuildGround();
        EditorUtility.DisplayProgressBar("Plog", "Estructuras...", 0.35f);
        BuildStructures();
        EditorUtility.DisplayProgressBar("Plog", "Árboles del perímetro...", 0.6f);
        BuildPerimeter();
        EditorUtility.DisplayProgressBar("Plog", "Bosque exterior...", 0.75f);
        BuildForest();
        EditorUtility.DisplayProgressBar("Plog", "Detalle...", 0.85f);
        BuildDetail();
        EditorUtility.ClearProgressBar();

        AssetDatabase.SaveAssets();
        Selection.activeGameObject = rootGo;
        var sb = new StringBuilder();
        sb.Append("Listo: " + count + " props colocados. Mapa de " + width + " x " + depth + " m centrado en (" + center.x + ", " + center.y + ").\n");
        if (missing.Count > 0) sb.Append("Roles sin modelo en tu kit (se omitieron): " + string.Join(", ", missing) + ".\n");
        if (unassigned.Count > 0) sb.Append(unassigned.Count + " modelos sin rol se ignoraron (usa 'Listar props').\n");
        sb.Append("Siguiente: desactiva el 'Escenario' viejo, pulsa 'Hornear NavMesh del mapa' y mueve el Player a (" + center.x + ", 0.5, " + (center.y - 17f * kz).ToString("0.#") + ").");
        info = sb.ToString();
        Debug.Log("[Plog] " + info);
    }

    void BuildGround()
    {
        var grass = GetMat("Pasto", grassColor, 0f);
        var soil = GetMat("Tierra", soilColor, 0f);
        var water = GetMat("Agua", waterColor, 0f);
        var forest = GetMat("SueloBosque", forestColor, 0f);
        var path = GetMat("Camino", pathColor, 0f);
        var wood = GetMat("Madera", woodColor, 0f);
        var stone = GetMat("Piedra", stoneColor, 0f);
        var rune = GetMat("Runa", runeColor, 1.6f);

        float hx = RefW / 2f, hz = RefD / 2f;

        // tierra (borde de la isla) y capa de pasto
        var s = Box("Tierra", soil, -hx, hx, -1.4f, -0.35f, -hz, hz, false);
        SetNavProp(s, "area", 1);
        Box("Pasto_Principal", grass, -hx, hx, -0.35f, 0f, -21f, hz, true);
        Box("Pasto_Orilla_Oeste", grass, -hx, -36f, -0.35f, 0f, -hz, -21f, true);
        Box("Pasto_Orilla_Este", grass, 36f, hx, -0.35f, 0f, -hz, -21f, true);
        Box("Pasto_Orilla_Sur", grass, -36f, 36f, -0.35f, 0f, -hz, -28f, true);

        // río
        var w = Box("Agua", water, -36f, 36f, -0.32f, -0.22f, -28f, -21f, false);
        SetNavProp(w, "area", 1);

        // suelo del bosque alrededor de la isla (no caminable para los enemigos)
        float F = forestDepth + 14f;
        var f1 = Box("Bosque_Norte", forest, -hx - F, hx + F, -0.35f, 0f, hz, hz + F, false);
        var f2 = Box("Bosque_Sur", forest, -hx - F, hx + F, -0.35f, 0f, -hz - F, -hz, false);
        var f3 = Box("Bosque_Oeste", forest, -hx - F, -hx, -0.35f, 0f, -hz, hz, false);
        var f4 = Box("Bosque_Este", forest, hx, hx + F, -0.35f, 0f, -hz, hz, false);
        SetNavProp(f1, "area", 1); SetNavProp(f2, "area", 1); SetNavProp(f3, "area", 1); SetNavProp(f4, "area", 1);

        // bloqueos: agua a los lados del puente y bordes del mapa
        Blocker("Bloqueo_Agua_Oeste", -36f, -2.6f, -28f, -21f, 3f);
        Blocker("Bloqueo_Agua_Este", 2.6f, 36f, -28f, -21f, 3f);
        Blocker("Borde_Norte", -hx - 1f, hx + 1f, hz, hz + 1f, 8f);
        Blocker("Borde_Sur", -hx - 1f, hx + 1f, -hz - 1f, -hz, 8f);
        Blocker("Borde_Oeste", -hx - 1f, -hx, -hz, hz, 8f);
        Blocker("Borde_Este", hx, hx + 1f, -hz, hz, 8f);

        // puente de madera (de z=-20.6 a z=-28.6)
        for (int i = 0; i < 16; i++)
        {
            float z = -20.7f - 0.5f * (i + 0.5f);
            var pk = Wp(0f, z);
            Prim(PrimitiveType.Cube, "Tabla", gGround, new Vector3(pk.x, 0.03f, pk.y), new Vector3(4.6f * kx, 0.14f, 0.46f * kz), wood, true);
        }
        for (int sx = -1; sx <= 1; sx += 2)
        {
            var pr = Wp(sx * 2.35f, -24.7f);
            Prim(PrimitiveType.Cube, "Baranda", gGround, new Vector3(pr.x, 0.95f, pr.y), new Vector3(0.14f, 0.12f, 8.2f * kz), wood, false);
            for (int k = 0; k < 4; k++)
            {
                var pp = Wp(sx * 2.35f, -20.8f - k * 2.6f);
                Prim(PrimitiveType.Cylinder, "Poste", gGround, new Vector3(pp.x, 0.6f, pp.y), new Vector3(0.3f, 0.7f, 0.3f), wood, false);
            }
        }

        // caminos de tierra frente a las tiendas
        foreach (float sx in new[] { -35f, 35f })
        {
            var pd = Wp(sx, 2.5f);
            Prim(PrimitiveType.Cylinder, "Camino", gGround, new Vector3(pd.x, 0.012f, pd.y), new Vector3(7f * kx, 0.01f, 5f * kz), path, false);
        }

        // plataformas de runas
        float[,] runes = { { -11.7f, 2.6f }, { 10.7f, 8f }, { 0f, -12.9f }, { 0f, 14f } };
        for (int i = 0; i < runes.GetLength(0); i++) RunePlatform(runes[i, 0], runes[i, 1], i == 3 ? 1.6f : 1.5f, stone, rune);
        if (!roles.ContainsKey("fuente")) BigPlaza(stone, rune);
    }

    void Blocker(string name, float x0, float x1, float z0, float z1, float h)
    {
        var p = Wp((x0 + x1) / 2f, (z0 + z1) / 2f);
        var go = new GameObject(name);
        go.transform.SetParent(gGround, false);
        go.transform.position = new Vector3(p.x, h / 2f - 0.5f, p.y);
        var bc = go.AddComponent<BoxCollider>();
        bc.size = new Vector3((x1 - x0) * kx, h, (z1 - z0) * kz);
    }

    void RunePlatform(float rx, float rz, float r, Material stone, Material rune)
    {
        var p = Wp(rx, rz);
        Prim(PrimitiveType.Cylinder, "Runa_Base", gGround, new Vector3(p.x, 0.04f, p.y), new Vector3(r * 2f, 0.04f, r * 2f), stone, false);
        Prim(PrimitiveType.Cylinder, "Runa_Brillo", gGround, new Vector3(p.x, 0.07f, p.y), new Vector3(r * 1.25f, 0.02f, r * 1.25f), rune, false);
    }

    void BigPlaza(Material stone, Material rune)
    {
        var p = Wp(0f, 1.5f);
        Prim(PrimitiveType.Cylinder, "Plaza_Base", gGround, new Vector3(p.x, 0.12f, p.y), new Vector3(10f * kx, 0.12f, 10f * kx), stone, false);
        Prim(PrimitiveType.Cylinder, "Plaza_Anillo", gGround, new Vector3(p.x, 0.26f, p.y), new Vector3(7.2f * kx, 0.04f, 7.2f * kx), GetMat("PiedraClara", stoneColor * 1.25f, 0f), false);
        Prim(PrimitiveType.Cylinder, "Plaza_Runa", gGround, new Vector3(p.x, 0.3f, p.y), new Vector3(3.4f * kx, 0.02f, 3.4f * kx), rune, false);
    }

    void BuildStructures()
    {
        // --- muralla norte con puerta, antorchas y estandartes ---
        Row("muro", -37f, 24f, -9f, 24f, 0f, 0f, 1f, 1f, 0f, false);
        Row("muro", 9f, 24f, 37f, 24f, 0f, 0f, 1f, 1f, 0f, false);
        Put("puerta", 0f, 24.3f, 180f, 1f, false);
        Put("antorcha", -7.2f, 24f, 180f, 1f, false); Put("antorcha", 7.2f, 24f, 180f, 1f, false);
        Put("estandarte", -16f, 22.6f, 180f, 1f, false); Put("estandarte", 16f, 22.6f, 180f, 1f, false);

        // --- plaza central y círculo de menhires ---
        Put("fuente", 0f, 1.5f, 0f, 1f, false);
        for (int i = 0; i < 7; i++)
        {
            float a = i * Mathf.PI * 2f / 7f;
            Put("menhir", Mathf.Cos(a) * 5.2f, 14f + Mathf.Sin(a) * 4.2f, -a * Mathf.Rad2Deg - 90f, Random.Range(0.95f, 1.15f), false);
        }

        // --- parches de pasto alto con vallas ---
        FillGrass(-22f, 14f, -10f, 18.5f); Row("valla", -22f, 13.8f, -10f, 13.8f, 0f, 0f, 1f, 1f, 0f, false); Row("valla", -22.2f, 14f, -22.2f, 18.5f, 0f, 0f, 1f, 1f, 90f, false);
        FillGrass(8f, 15.5f, 17f, 20f); FillGrass(14f, 8f, 17.5f, 15.5f); Row("valla", 13.8f, 8f, 13.8f, 15.4f, 0f, 0f, 1f, 1f, 90f, false); Row("valla", 8f, 15.4f, 13.8f, 15.4f, 0f, 0f, 1f, 1f, 0f, false);
        FillGrass(-24f, -16f, -14f, -12.5f); FillGrass(-24f, -12.5f, -21f, -8f); Row("valla", -24.2f, -16f, -24.2f, -8f, 0f, 0f, 1f, 1f, 90f, false); Row("valla", -24f, -16.2f, -14f, -16.2f, 0f, 0f, 1f, 1f, 0f, false);
        FillGrass(13f, -15f, 26f, -11f); Row("valla", 13f, -15.2f, 26f, -15.2f, 0f, 0f, 1f, 1f, 0f, false); Row("valla", 12.8f, -15f, 12.8f, -11f, 0f, 0f, 1f, 1f, 90f, false);

        // --- muros sueltos, cajas, barriles ---
        Row("muro", -27f, 9.5f, -19f, 9.5f, 0f, 0f, 1f, 1f, 0f, false);
        Row("muro", -23f, -3.5f, -16f, -3.5f, 0f, 0f, 1f, 1f, 0f, false);
        Row("muro", 11f, 1f, 17f, 1f, 0f, 0f, 1f, 1f, 0f, false);
        Put("caja", -12.3f, 10.2f, 0f, 1f, false); Put("caja", -10.9f, 10.2f, 5f, 1f, false); Put("caja", -11.6f, 11.4f, -4f, 1f, false); Put("barril", -9.6f, 9.7f, 0f, 1f, false);
        Put("caja", 18.8f, -4f, 0f, 1f, false); Put("caja", 20.2f, -4f, 3f, 1f, false); Put("caja", 18.8f, -5.4f, 0f, 1f, false); Put("caja", 20.2f, -5.4f, -3f, 1f, false); Put("barril", 22.2f, -4.4f, 0f, 1f, false);

        // --- rocas con arbustos ---
        Put("roca", -22f, 3.5f, 20f, 1.4f, false); Put("arbusto", -23.8f, 2.2f, 0f, 1.1f, false); Put("arbusto", -20.4f, 2.6f, 90f, 1.0f, false); Put("arbusto", -22f, 1.4f, 45f, 0.9f, false);
        Put("roca", 22f, 4.5f, -30f, 1.4f, false); Put("arbusto", 20.2f, 3.2f, 0f, 1.1f, false); Put("arbusto", 23.8f, 3.6f, 90f, 1.0f, false); Put("arbusto", 22f, 2.4f, 45f, 0.9f, false);

        // --- tronco con hongos ---
        Put("tronco", -7f, -7f, 10f, 1f, false); Put("hongo", -8.4f, -7.9f, 0f, 1.2f, false); Put("hongo", -5.8f, -6.2f, 40f, 1f, false); Put("hongo", -5.5f, -7.8f, 80f, 1.1f, false); Put("arbusto", -6.4f, -8.4f, 0f, 1f, false);

        // --- campamentos laterales ---
        Put("carpa", -39.5f, 3f, 90f, 1f, false); Put("carpa", 39.5f, 3f, -90f, 1f, false);
        Put("barril", -37f, 0.2f, 0f, 1f, false); Put("barril", 37f, 0.2f, 0f, 1f, false);
        Put("antorcha", -36.5f, 5.6f, 90f, 1f, false); Put("antorcha", 36.5f, 5.6f, -90f, 1f, false);

        // --- torres de ruinas en las esquinas ---
        Put("torre", 42f, 21f, -90f, 1.2f, false); Put("torre", 42f, -16f, -90f, 1.2f, false);
        Put("torre", -42f, 21f, 90f, 1.0f, false);

        // --- farolas y entrada del puente ---
        Put("linterna", -39f, 17f, 90f, 1f, false); Put("linterna", -38.5f, -14f, 90f, 1f, false); Put("linterna", 38.5f, 18f, -90f, 1f, false); Put("linterna", 39f, -14f, -90f, 1f, false);
        Row("valla", -18f, -20.4f, -5f, -20.4f, 0f, 0f, 1f, 1f, 0f, false); Row("valla", 5f, -20.4f, 18f, -20.4f, 0f, 0f, 1f, 1f, 0f, false);
        Put("estandarte", -9f, -19.4f, 0f, 1f, false); Put("estandarte", 9f, -19.4f, 0f, 1f, false);
        Put("roca", -33f, -23.5f, 0f, 1.2f, false); Put("roca", 33f, -23.5f, 0f, 1.0f, false);
    }

    void BuildPerimeter()
    {
        // borde interior del campo: copas pegadas, como en el concept
        TreeRow(-44f, 27.6f, 44f, 27.6f, 3.0f, 1.1f, 1.6f);
        TreeRow(-44f, 29.8f, 44f, 29.8f, 2.8f, 1.2f, 1.8f);
        TreeRow(-43.6f, -27f, -43.6f, 26f, 3.0f, 1.1f, 1.6f);
        TreeRow(-41.2f, -27f, -41.2f, -3f, 3.6f, 1.0f, 1.4f);
        TreeRow(-41.2f, 9f, -41.2f, 25f, 3.6f, 1.0f, 1.4f);
        TreeRow(43.6f, -27f, 43.6f, 26f, 3.0f, 1.1f, 1.6f);
        TreeRow(41.2f, -27f, 41.2f, -3f, 3.6f, 1.0f, 1.4f);
        TreeRow(41.2f, 9f, 41.2f, 25f, 3.6f, 1.0f, 1.4f);
        // árboles grandes de esquina
        Put("arbolgrande", -41f, 25f, 0f, 1.5f, false); Put("arbolgrande", -40f, -24f, 90f, 1.5f, false); Put("arbolgrande", 41f, -25f, 45f, 1.4f, false);
        // orillas junto al río
        TreeRow(-45f, -29.2f, -37f, -29.2f, 2.8f, 1.0f, 1.4f);
        TreeRow(37f, -29.2f, 45f, -29.2f, 2.8f, 1.0f, 1.4f);
    }

    // Bosque denso por fuera del campo: filas de árboles hacia afuera, sin colliders (no se pueden alcanzar)
    void BuildForest()
    {
        float hx = RefW / 2f, hz = RefD / 2f;
        bool prev = addColliders;
        addColliders = false;
        float[] offs = { 1.6f, 4.2f, 6.8f, 9.4f, 12f, 14.6f, 17.2f };
        foreach (float o in offs)
        {
            if (o > forestDepth) break;
            TreeRow(-hx - o, hz + o, hx + o, hz + o, 2.9f, 1.2f, 1.9f);
            TreeRow(-hx - o, -hz - o, hx + o, -hz - o, 2.9f, 1.2f, 1.9f);
            TreeRow(-hx - o, -hz - o, -hx - o, hz + o, 2.9f, 1.2f, 1.9f);
            TreeRow(hx + o, -hz - o, hx + o, hz + o, 2.9f, 1.2f, 1.9f);
        }
        addColliders = prev;
    }

    void BuildDetail()
    {
        Scatter("flor", 140, -42f, -29f, 42f, 22f, 0.9f, 1.4f, true);
        Scatter("hongo", 22, -42f, -27f, 42f, 22f, 0.9f, 1.3f, true);
        Scatter("pasto", 40, -42f, -19f, 42f, 22f, 0.9f, 1.4f, true);
        Scatter("arbusto", 26, -42f, -19f, 42f, 22f, 0.8f, 1.2f, true);
        Scatter("roca", 24, -42f, -19f, 42f, 22f, 0.35f, 0.7f, true);
        // orilla sur con flores y rocas pequeñas
        Scatter("flor", 20, -36f, -29.8f, 36f, -28.2f, 0.9f, 1.3f, false, true);
    }

    // ====================== NavMesh ======================
    void BakeNav()
    {
        var rootGo = GameObject.Find(RootName);
        if (rootGo == null) { info = "Primero construye el mapa."; return; }
        try
        {
            var t = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
            if (t == null) { info = "No encontré el paquete AI Navigation. Agrega un NavMesh Surface al objeto " + RootName + " (Collect Objects: Current Object Hierarchy) y pulsa Bake."; return; }
            var surf = rootGo.GetComponent(t);
            if (surf == null) surf = rootGo.AddComponent(t);
            var pc = t.GetProperty("collectObjects");
            pc.SetValue(surf, System.Enum.ToObject(pc.PropertyType, 2), null);
            t.GetMethod("BuildNavMesh").Invoke(surf, null);
            info = "NavMesh horneado en " + RootName + ". Desactiva el NavMesh Surface del 'Escenario' viejo.";
        }
        catch (System.Exception e)
        {
            info = "No pude hornear automáticamente (" + e.Message + "). Agrega un NavMesh Surface a " + RootName + " y pulsa Bake.";
        }
    }
}
#endif