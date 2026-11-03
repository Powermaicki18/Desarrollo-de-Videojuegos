// Plog Origins · Decorador de escenarios. Colocar en Assets/Editor.
// Menú: Plog > Organizar escenario (decoración)
// Lee una carpeta con los props del escenario, los clasifica (grandes / medianos / pequeños) y los reparte:
//   anillo exterior, borde del campo, coberturas, detalle de suelo, tapete exterior, con calles y plaza libres.
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlogScenarioDecorator : EditorWindow
{
    enum Cat { Grande, Mediano, Pequeno }
    enum Zone { Ring, Border, Inner }

    class Prop
    {
        public GameObject asset;
        public Cat cat;
        public bool tree;
        public float radius, pivotY;
        public Vector3 size, center, baseScale;
    }

    const string RootName = "Decor_Generado";

    // ---------- ajustes (públicos: Unity los recuerda) ----------
    public DefaultAsset kitFolder;
    public GameObject escenario;
    public Transform player;
    public LayerMask groundMask = ~0;
    public Material skirtMaterial;
    public Color skirtColor = new Color(0.55f, 0.84f, 0.74f);
    public bool copyFloorMaterial = false;

    public string excludeNames = "valla,fence,casa,house,puente,bridge,suelo,ground,floor,terreno";
    public string avoidNames = "agua,water,lago,rio,estanque,pond";
    public string treeWords = "arbol,árbol,tree,pino,pine,palma,palm,roble,oak";
    public string smallWords = "flor,flower,pasto,grass,hongo,mushroom,junco,reed,hierba,seta";

    public float minX = -32.6f, maxX = 13.4f, minZ = -34.7f, maxZ = -4.2f;
    public float ringWidth = 25f, ringGap = 1.5f, borderBand = 6f, edgeMargin = 1.5f;

    public int ringLarge = 60, ringMedium = 25, borderLarge = 14, innerMedium = 26, innerSmall = 140;
    public Vector2 scaleLarge = new Vector2(0.9f, 1.4f);
    public Vector2 scaleMedium = new Vector2(0.8f, 1.3f);
    public Vector2 scaleSmall = new Vector2(0.8f, 1.4f);

    public bool keepHLane = true, keepVLane = false;
    public float laneWidth = 5f, plazaRadius = 6f, playerSafe = 5f;
    public bool addColliders = true, makeStatic = true;
    public int seed = 7;
    public Color skyColor = new Color(0.60f, 0.86f, 0.92f);

    // ---------- estado interno ----------
    Transform rootT;
    readonly List<Vector3> placed = new List<Vector3>(); // x, z, radio
    Vector2 scroll;
    string info = "";
    readonly int[] rej = new int[6]; // zona, sin suelo, pendiente, agua, cerca de otro, choca con objeto

    [MenuItem("Plog/Organizar escenario (decoración)")]
    static void Open() { GetWindow<PlogScenarioDecorator>("Decorador Plog").minSize = new Vector2(380, 520); }

    void OnGUI()
    {
        var so = new SerializedObject(this);
        so.Update();
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("1. Qué usar", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("kitFolder"), new GUIContent("Carpeta de props"));
        EditorGUILayout.PropertyField(so.FindProperty("escenario"), new GUIContent("Objeto del escenario"));
        EditorGUILayout.PropertyField(so.FindProperty("player"), new GUIContent("Player (zona segura)"));
        EditorGUILayout.PropertyField(so.FindProperty("groundMask"), new GUIContent("Capas de suelo"));
        EditorGUILayout.PropertyField(so.FindProperty("skirtMaterial"), new GUIContent("Material del tapete (opcional)"));
        EditorGUILayout.PropertyField(so.FindProperty("skirtColor"), new GUIContent("Color del tapete (si no hay material)"));
        EditorGUILayout.PropertyField(so.FindProperty("copyFloorMaterial"), new GUIContent("Copiar el material del suelo"));
        EditorGUILayout.PropertyField(so.FindProperty("excludeNames"), new GUIContent("Excluir nombres"));
        EditorGUILayout.PropertyField(so.FindProperty("avoidNames"), new GUIContent("Evitar suelo llamado"));
        EditorGUILayout.PropertyField(so.FindProperty("treeWords"), new GUIContent("Palabras de árbol"));
        EditorGUILayout.PropertyField(so.FindProperty("smallWords"), new GUIContent("Palabras de detalle"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("2. Límites del campo jugable (X / Z)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("minX")); EditorGUILayout.PropertyField(so.FindProperty("maxX"));
        EditorGUILayout.PropertyField(so.FindProperty("minZ")); EditorGUILayout.PropertyField(so.FindProperty("maxZ"));
        so.ApplyModifiedProperties();
        if (GUILayout.Button("Detectar límites desde el escenario")) DetectBounds();
        so.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("3. Cantidades", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("ringWidth"), new GUIContent("Ancho del anillo"));
        EditorGUILayout.PropertyField(so.FindProperty("ringLarge"), new GUIContent("Anillo: árboles/grandes"));
        EditorGUILayout.PropertyField(so.FindProperty("ringMedium"), new GUIContent("Anillo: rocas/medianos"));
        EditorGUILayout.PropertyField(so.FindProperty("borderLarge"), new GUIContent("Borde del campo: grandes"));
        EditorGUILayout.PropertyField(so.FindProperty("borderBand"), new GUIContent("Ancho de la franja del borde"));
        EditorGUILayout.PropertyField(so.FindProperty("innerMedium"), new GUIContent("Interior: medianos (coberturas)"));
        EditorGUILayout.PropertyField(so.FindProperty("innerSmall"), new GUIContent("Interior: detalle pequeño"));
        EditorGUILayout.PropertyField(so.FindProperty("scaleLarge"), new GUIContent("Escala grandes (mín, máx)"));
        EditorGUILayout.PropertyField(so.FindProperty("scaleMedium"), new GUIContent("Escala medianos"));
        EditorGUILayout.PropertyField(so.FindProperty("scaleSmall"), new GUIContent("Escala pequeños"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("4. Zonas libres", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("keepHLane"), new GUIContent("Calle libre horizontal (eje X)"));
        EditorGUILayout.PropertyField(so.FindProperty("keepVLane"), new GUIContent("Calle libre vertical (eje Z)"));
        EditorGUILayout.PropertyField(so.FindProperty("laneWidth"), new GUIContent("Ancho de las calles"));
        EditorGUILayout.PropertyField(so.FindProperty("plazaRadius"), new GUIContent("Radio libre en el centro"));
        EditorGUILayout.PropertyField(so.FindProperty("playerSafe"), new GUIContent("Radio libre del Player"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("5. Opciones", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("addColliders"), new GUIContent("Colliders a árboles y rocas"));
        EditorGUILayout.PropertyField(so.FindProperty("makeStatic"), new GUIContent("Marcar como Static"));
        EditorGUILayout.PropertyField(so.FindProperty("seed"), new GUIContent("Semilla (cambia el resultado)"));
        EditorGUILayout.PropertyField(so.FindProperty("skyColor"), new GUIContent("Color de fondo y niebla"));
        so.ApplyModifiedProperties();

        EditorGUILayout.Space();
        GUI.backgroundColor = new Color(0.6f, 1f, 0.7f);
        if (GUILayout.Button("GENERAR DECORACIÓN", GUILayout.Height(34))) Generate();
        GUI.backgroundColor = Color.white;
        if (GUILayout.Button("Borrar decoración generada")) ClearExisting();
        if (GUILayout.Button("Aplicar fondo de cámara y niebla")) ApplySky();

        if (!string.IsNullOrEmpty(info)) EditorGUILayout.HelpBox(info, MessageType.Info);
        EditorGUILayout.EndScrollView();
    }

    // ---------- utilidades ----------
    static bool Has(string name, string words)
    {
        var n = name.ToLowerInvariant();
        foreach (var w in words.Split(','))
        {
            var t = w.Trim().ToLowerInvariant();
            if (t.Length > 0 && n.Contains(t)) return true;
        }
        return false;
    }

    // Compara palabras completas (separadas por _ - espacio o números), para que "Escenario" no cuente como "rio"
    static bool HasWord(string name, string words)
    {
        var tokens = new List<string>();
        var sb = new System.Text.StringBuilder();
        foreach (var ch in name.ToLowerInvariant())
        {
            if (char.IsLetter(ch)) sb.Append(ch);
            else { if (sb.Length > 0) tokens.Add(sb.ToString()); sb.Length = 0; }
        }
        if (sb.Length > 0) tokens.Add(sb.ToString());
        foreach (var w in words.Split(','))
        {
            var t = w.Trim().ToLowerInvariant();
            if (t.Length == 0) continue;
            if (tokens.Exists(x => x == t || (t.Length >= 4 && x.StartsWith(t)))) return true;
        }
        return false;
    }

    void DetectBounds()
    {
        if (escenario == null) { info = "Arrastra primero el objeto del escenario (por ejemplo 'Escenario')."; return; }
        var rs = escenario.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) { info = "El escenario no tiene Renderers."; return; }
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        minX = b.min.x + 1f; maxX = b.max.x - 1f; minZ = b.min.z + 1f; maxZ = b.max.z - 1f;
        info = string.Format("Límites detectados: X {0:0.#} a {1:0.#}, Z {2:0.#} a {3:0.#}. Revísalos antes de generar.", minX, maxX, minZ, maxZ);
        Repaint();
    }

    List<Prop> LoadProps()
    {
        var list = new List<Prop>();
        if (kitFolder == null) return list;
        var folder = AssetDatabase.GetAssetPath(kitFolder);
        foreach (var guid in AssetDatabase.FindAssets("t:GameObject", new[] { folder }))
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset == null || Has(asset.name, excludeNames)) continue;
            if (asset.GetComponentInChildren<Renderer>() == null) continue;

            var tmp = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            tmp.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var rs = tmp.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            var p = new Prop
            {
                asset = asset,
                size = b.size,
                center = b.center,
                radius = 0.5f * Mathf.Max(b.size.x, b.size.z),
                pivotY = -b.min.y,
                baseScale = asset.transform.localScale
            };
            DestroyImmediate(tmp);

            float footprint = Mathf.Max(p.size.x, p.size.z);
            if (footprint > 12f || p.size.y < 0.02f) continue; // piezas enormes o planas (suelo): no son props

            p.tree = Has(asset.name, treeWords);
            if (p.tree) p.cat = Cat.Grande;
            else if (Has(asset.name, smallWords)) p.cat = Cat.Pequeno;
            else p.cat = p.size.y > 3.5f ? Cat.Grande : (p.size.y > 0.9f ? Cat.Mediano : Cat.Pequeno);
            list.Add(p);
        }
        return list;
    }

    void ClearExisting()
    {
        var old = GameObject.Find(RootName);
        if (old != null) Undo.DestroyObjectImmediate(old);
        info = "Decoración generada borrada.";
    }

    void ApplySky()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam, "Fondo");
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = skyColor;
        }
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = skyColor;
        RenderSettings.fogStartDistance = 45f;
        RenderSettings.fogEndDistance = 110f;
        info = "Fondo de cámara y niebla aplicados.";
    }

    // ---------- generación ----------
    void Generate()
    {
        var props = LoadProps();
        if (props.Count == 0)
        {
            EditorUtility.DisplayDialog("Plog", "No encontré props. Arrastra la carpeta con los modelos del escenario (por ejemplo la de Escenario1) en 'Carpeta de props'.", "OK");
            return;
        }
        var large = props.FindAll(p => p.cat == Cat.Grande);
        var medium = props.FindAll(p => p.cat == Cat.Mediano);
        var small = props.FindAll(p => p.cat == Cat.Pequeno);

        ClearExisting();
        Physics.SyncTransforms();
        Random.InitState(seed);
        placed.Clear();
        System.Array.Clear(rej, 0, rej.Length);

        var rootGo = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(rootGo, "Generar decoración");
        rootT = rootGo.transform;
        var gRing = NewGroup("Anillo");
        var gBorder = NewGroup("Borde");
        var gInner = NewGroup("Interior");
        var gDetail = NewGroup("Detalle");

        // Altura del suelo: se miden 25 puntos y se usa el más bajo (así un objeto en el centro no la falsea)
        float groundY = float.MaxValue;
        Material floorMat = null;
        for (int ix = 0; ix <= 4; ix++)
        {
            for (int iz = 0; iz <= 4; iz++)
            {
                float gx = Mathf.Lerp(minX + 2f, maxX - 2f, ix / 4f);
                float gz = Mathf.Lerp(minZ + 2f, maxZ - 2f, iz / 4f);
                if (Physics.Raycast(new Vector3(gx, 200f, gz), Vector3.down, out var gh, 400f, groundMask, QueryTriggerInteraction.Ignore) && gh.point.y < groundY)
                {
                    groundY = gh.point.y;
                    var rr = gh.collider.GetComponent<Renderer>();
                    if (rr == null) rr = gh.collider.GetComponentInParent<Renderer>();
                    if (rr != null) floorMat = rr.sharedMaterial;
                }
            }
        }
        if (groundY == float.MaxValue) groundY = 0f;
        CreateSkirt(groundY, floorMat);
        Physics.SyncTransforms(); // para que el tapete recién creado ya cuente en los raycast

        int a = 0, b = 0, c = 0, d = 0, e = 0;
        EditorUtility.DisplayProgressBar("Plog", "Anillo exterior...", 0.1f);
        a = Scatter(large.Count > 0 ? large : medium, ringLarge, Zone.Ring, scaleLarge, 3, 5, 4f, gRing);
        b = Scatter(medium.Count > 0 ? medium : large, ringMedium, Zone.Ring, scaleMedium, 1, 3, 3f, gRing);
        EditorUtility.DisplayProgressBar("Plog", "Borde del campo...", 0.4f);
        c = Scatter(large.Count > 0 ? large : medium, borderLarge, Zone.Border, scaleLarge, 1, 3, 3f, gBorder);
        EditorUtility.DisplayProgressBar("Plog", "Coberturas...", 0.6f);
        d = Scatter(medium.Count > 0 ? medium : small, innerMedium, Zone.Inner, scaleMedium, 1, 3, 2.5f, gInner);
        EditorUtility.DisplayProgressBar("Plog", "Detalle de suelo...", 0.8f);
        e = Scatter(small.Count > 0 ? small : medium, innerSmall, Zone.Inner, scaleSmall, 2, 5, 1.8f, gDetail);
        EditorUtility.ClearProgressBar();

        Selection.activeGameObject = rootGo;
        info = string.Format("Listo. Props usados: {0} grandes, {1} medianos, {2} pequeños.\nColocados: anillo {3}+{4}, borde {5}, coberturas {6}, detalle {7}.\nRechazos: fuera de zona {8}, sin suelo {9}, pendiente {10}, agua {11}, muy cerca {12}, choca con objeto {13}.\nSi algo quedó mal clasificado, ajusta las palabras o cambia la semilla. Después hornea el NavMesh.",
            large.Count, medium.Count, small.Count, a, b, c, d, e, rej[0], rej[1], rej[2], rej[3], rej[4], rej[5]);
        Debug.Log("[Plog] " + info);
    }

    Transform NewGroup(string n)
    {
        var g = new GameObject(n);
        g.transform.SetParent(rootT, false);
        return g.transform;
    }

    // Crea (o actualiza) un material liso con el shader del pipeline activo y el color elegido
    Material MakeSkirtMaterial()
    {
        var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        Shader sh = (rp != null && rp.defaultMaterial != null) ? rp.defaultMaterial.shader : Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        const string path = "Assets/Materials/Tapete_Plog.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
        m.shader = sh;
        m.SetColor("_BaseColor", skirtColor); m.SetColor("_Color", skirtColor);
        m.SetFloat("_Smoothness", 0f); m.SetFloat("_Metallic", 0f);
        m.SetFloat("_SpecularHighlights", 0f); m.SetFloat("_EnvironmentReflections", 0f);
        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        return m;
    }

    void CreateSkirt(float groundY, Material floorMat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.name = "Tapete_Exterior";
        go.transform.SetParent(rootT, false);
        float size = Mathf.Max(maxX - minX, maxZ - minZ) + ringWidth * 2f + 140f;
        go.transform.position = new Vector3((minX + maxX) / 2f, groundY - 0.12f, (minZ + maxZ) / 2f);
        go.transform.localScale = new Vector3(size / 10f, 1f, size / 10f);
        Material mat = skirtMaterial != null ? skirtMaterial : ((copyFloorMaterial && floorMat != null) ? floorMat : MakeSkirtMaterial());
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        if (makeStatic) go.isStatic = true;
    }

    int Scatter(List<Prop> pool, int count, Zone zone, Vector2 scaleRange, int cMin, int cMax, float spread, Transform parent)
    {
        if (pool == null || pool.Count == 0 || count <= 0) return 0;
        int done = 0, tries = 0;
        while (done < count && tries < count * 40)
        {
            tries++;
            Vector2 center = RandomPoint(zone);
            int n = Mathf.Min(count - done, Random.Range(cMin, cMax + 1));
            for (int i = 0; i < n; i++)
            {
                var p = pool[Random.Range(0, pool.Count)];
                float s = Random.Range(scaleRange.x, scaleRange.y);
                Vector2 xz = center + Random.insideUnitCircle * (i == 0 ? 0f : spread);
                if (TryPlace(p, xz, s, zone, parent)) done++;
            }
        }
        return done;
    }

    Vector2 RandomPoint(Zone zone)
    {
        if (zone == Zone.Ring)
        {
            for (int i = 0; i < 30; i++)
            {
                var pt = new Vector2(Random.Range(minX - ringWidth, maxX + ringWidth), Random.Range(minZ - ringWidth, maxZ + ringWidth));
                if (!InsideInner(pt, -ringGap)) return pt;
            }
        }
        return new Vector2(Random.Range(minX + edgeMargin, maxX - edgeMargin), Random.Range(minZ + edgeMargin, maxZ - edgeMargin));
    }

    // shrink > 0 encoge el rectángulo; < 0 lo agranda
    bool InsideInner(Vector2 p, float shrink)
    {
        return p.x > minX + shrink && p.x < maxX - shrink && p.y > minZ + shrink && p.y < maxZ - shrink;
    }

    bool ZoneOk(Vector2 p, Zone zone)
    {
        switch (zone)
        {
            case Zone.Ring:
                return !InsideInner(p, -ringGap) && p.x > minX - ringWidth && p.x < maxX + ringWidth && p.y > minZ - ringWidth && p.y < maxZ + ringWidth;
            case Zone.Border:
                return InsideInner(p, edgeMargin) && !InsideInner(p, borderBand);
            default:
                if (!InsideInner(p, edgeMargin)) return false;
                float cx = (minX + maxX) / 2f, cz = (minZ + maxZ) / 2f;
                if (keepHLane && Mathf.Abs(p.y - cz) < laneWidth / 2f) return false;
                if (keepVLane && Mathf.Abs(p.x - cx) < laneWidth / 2f) return false;
                if (Vector2.Distance(p, new Vector2(cx, cz)) < plazaRadius) return false;
                if (player != null && Vector2.Distance(p, new Vector2(player.position.x, player.position.z)) < playerSafe) return false;
                return true;
        }
    }

    bool TryPlace(Prop p, Vector2 xz, float s, Zone zone, Transform parent)
    {
        if (!ZoneOk(xz, zone)) { rej[0]++; return false; }
        float r = p.radius * s;

        if (!Physics.Raycast(new Vector3(xz.x, 200f, xz.y), Vector3.down, out var hit, 400f, groundMask, QueryTriggerInteraction.Ignore)) { rej[1]++; return false; }
        if (hit.normal.y < 0.85f) { rej[2]++; return false; }
        if (hit.collider.transform.IsChildOf(rootT) && hit.collider.name != "Tapete_Exterior") { rej[4]++; return false; }
        if (HasWord(hit.collider.name, avoidNames) || (hit.collider.transform.parent != null && HasWord(hit.collider.transform.parent.name, avoidNames))) { rej[3]++; return false; }

        foreach (var q in placed)
            if (Vector2.Distance(xz, new Vector2(q.x, q.y)) < (r + q.z) * 0.75f) { rej[4]++; return false; }

        float cr = Mathf.Max(0.2f, r * 0.6f);
        var cols = Physics.OverlapSphere(hit.point + Vector3.up * (0.45f + cr), cr, ~0, QueryTriggerInteraction.Ignore);
        foreach (var col in cols)
        {
            if (col == hit.collider || col.transform.IsChildOf(rootT)) continue;
            if (col.bounds.max.y < hit.point.y + 0.35f) continue; // piezas planas de suelo o caminos
            rej[5]++;
            return false;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(p.asset, parent);
        go.transform.position = new Vector3(xz.x, hit.point.y + p.pivotY * s - 0.02f, xz.y);
        go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        go.transform.localScale = p.baseScale * s;
        Undo.RegisterCreatedObjectUndo(go, "Decor");

        if (addColliders && p.cat != Cat.Pequeno && go.GetComponentInChildren<Collider>() == null)
        {
            var bc = go.AddComponent<BoxCollider>();
            Vector3 sz = p.tree ? new Vector3(Mathf.Max(0.4f, p.size.x * 0.25f), p.size.y, Mathf.Max(0.4f, p.size.z * 0.25f)) : p.size * 0.9f;
            if (!p.tree) sz.y = p.size.y;
            var bs = p.baseScale;
            bc.size = new Vector3(sz.x / bs.x, sz.y / bs.y, sz.z / bs.z);
            bc.center = new Vector3(p.center.x / bs.x, p.center.y / bs.y, p.center.z / bs.z);
        }
        if (makeStatic) foreach (var t in go.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;

        placed.Add(new Vector3(xz.x, xz.y, r));
        return true;
    }
}
#endif