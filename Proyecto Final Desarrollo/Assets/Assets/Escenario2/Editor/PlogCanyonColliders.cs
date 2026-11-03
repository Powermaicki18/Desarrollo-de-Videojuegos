#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// Plog > 5. Colliders del cañón
// Quita los colliders del "Escenario Canon Ambar" y los vuelve a poner por tipo de pieza.
// Se puede ejecutar las veces que quieras.
public static class PlogCanyonColliders
{
    const string RootName = "Escenario Canon Ambar";
    const string LimitsName = "Limites_Invisibles";

    // Superficies por las que se camina o que bloquean con su forma exacta
    static readonly string[] MeshTypes =
    {
        "CA_Suelo_Arena", "CA_Pared_Canon", "CA_Rampa", "CA_Escalera", "CA_Meseta",
        "CA_Puente", "CA_Arco_Roca", "CA_Entrada_Mina", "CA_Oasis_Estanque"
    };

    // Plantas / postes altos: solo el tronco (caja delgada)
    static readonly string[] TrunkTypes =
    {
        "CA_Cactus_Saguaro", "CA_Palmera", "CA_Arbol_Seco", "CA_Totem", "CA_Cristal_Ambar"
    };

    // Sin collider (decoración pequeña que no debe estorbar)
    static readonly string[] NoneTypes =
    {
        "CA_Mata_Pasto", "CA_Arbusto_Seco", "CA_Roca_Pequena", "CA_Cactus_Pala", "CA_Letrero",
        "CA_Fogata", "CA_Cactus_Barril", "CA_Barril"
    };

    // Todo lo demás (rocas, pilares, cajas, carpa, carreta, torre, ruinas, pozo, vallas) = caja ajustada

    static bool Match(string n, string[] list)
    {
        foreach (var s in list) if (n.StartsWith(s)) return true;
        return false;
    }

    [MenuItem("Plog/5. Colliders del cañón")]
    public static void Apply()
    {
        var root = GameObject.Find(RootName);
        if (root == null) { EditorUtility.DisplayDialog("Plog", "No encuentro '" + RootName + "'. Arma el escenario primero (menú 3).", "OK"); return; }

        // 1) limpiar
        var old = root.transform.Find(LimitsName);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        foreach (var c in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);

        int mesh = 0, box = 0, trunk = 0, none = 0;

        // 2) por pieza (hijos de cada grupo)
        foreach (Transform group in root.transform)
        {
            foreach (Transform piece in group)
            {
                string n = piece.name;
                bool isFloorFar = n.StartsWith("CA_Suelo_Fondo");

                if (Match(n, NoneTypes)) { none++; continue; }

                foreach (var mf in piece.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.sharedMesh == null) continue;
                    var go = mf.gameObject;
                    var b = mf.sharedMesh.bounds;

                    if (isFloorFar || Match(n, MeshTypes))
                    {
                        var mc = go.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                        mesh++;
                    }
                    else if (Match(n, TrunkTypes))
                    {
                        var bc = go.AddComponent<BoxCollider>();
                        var s = b.size;
                        float w = Mathf.Min(0.7f, Mathf.Min(s.x, s.z) * 0.3f);
                        bc.center = b.center;
                        bc.size = new Vector3(w, s.y, w);
                        trunk++;
                    }
                    else
                    {
                        var bc = go.AddComponent<BoxCollider>();
                        bc.center = b.center;
                        bc.size = b.size;
                        box++;
                    }
                }
            }
        }

        // 3) límites invisibles (por si alguien se acerca al borde)
        var lim = new GameObject(LimitsName);
        lim.transform.SetParent(root.transform, false);
        lim.isStatic = true;
        const float hx = 44.5f, hz = 34.5f, h = 8f, t = 1f;
        AddWall(lim.transform, "Limite_Este",  new Vector3( hx, h / 2, 0), new Vector3(t, h, hz * 2 + 2));
        AddWall(lim.transform, "Limite_Oeste", new Vector3(-hx, h / 2, 0), new Vector3(t, h, hz * 2 + 2));
        AddWall(lim.transform, "Limite_Norte", new Vector3(0, h / 2,  hz), new Vector3(hx * 2 + 2, h, t));
        AddWall(lim.transform, "Limite_Sur",   new Vector3(0, h / 2, -hz), new Vector3(hx * 2 + 2, h, t));

        foreach (var tr in root.GetComponentsInChildren<Transform>(true)) tr.gameObject.isStatic = true;
        Physics.SyncTransforms();
        EditorUtility.SetDirty(root);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);

        Debug.Log("[Plog] Colliders: " + mesh + " mesh, " + box + " caja, " + trunk + " tronco, " + none + " piezas sin collider. " +
                  "Ahora re-hornea el NavMesh.");
    }

    static void AddWall(Transform parent, string name, Vector3 pos, Vector3 size)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.isStatic = true;
        var bc = g.AddComponent<BoxCollider>();
        bc.size = size;
    }

    [MenuItem("Plog/5b. Quitar colliders del cañón")]
    public static void Clear()
    {
        var root = GameObject.Find(RootName);
        if (root == null) return;
        var old = root.transform.Find(LimitsName);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        foreach (var c in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        Debug.Log("[Plog] Colliders del cañón eliminados.");
    }
}
#endif