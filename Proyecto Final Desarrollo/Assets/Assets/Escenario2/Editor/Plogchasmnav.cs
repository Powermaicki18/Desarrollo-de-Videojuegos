#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

// Plog > 6. Bloquear abismo para enemigos
// Marca el piso y las paredes del abismo como "Not Walkable" en el NavMesh y lo vuelve a hornear.
// Los puentes siguen siendo caminables.
public static class PlogChasmNav
{
    const string RootName = "Escenario Canon Ambar";

    static Type FindType(string name)
    {
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = a.GetType(name);
            if (t != null) return t;
        }
        return null;
    }

    [MenuItem("Plog/6. Bloquear abismo para enemigos")]
    public static void Apply()
    {
        var root = GameObject.Find(RootName);
        if (root == null) { EditorUtility.DisplayDialog("Plog", "No encuentro '" + RootName + "'.", "OK"); return; }

        var modType = FindType("Unity.AI.Navigation.NavMeshModifier");
        int marked = 0;

        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            var go = mf.gameObject;
            // piso o paredes del abismo: grupo Fondo, o cualquier pieza cuyo centro esté bajo el suelo
            bool inFondo = false;
            for (var t = go.transform; t != null && t != root.transform; t = t.parent)
                if (t.name == "Fondo") { inFondo = true; break; }
            var r = go.GetComponent<Renderer>();
            bool below = r != null && r.bounds.center.y < -1f;
            if (!inFondo && !below) continue;

            GameObjectUtility.SetNavMeshArea(go, 1);   // sistema clásico: 1 = Not Walkable

            if (modType != null)
            {
                var mod = go.GetComponent(modType);
                if (mod == null) mod = go.AddComponent(modType);
                modType.GetProperty("overrideArea")?.SetValue(mod, true);
                modType.GetProperty("area")?.SetValue(mod, 1);
            }
            EditorUtility.SetDirty(go);
            marked++;
        }

        // re-hornear NavMeshSurface si existe
        var surfType = FindType("Unity.AI.Navigation.NavMeshSurface");
        int baked = 0;
        if (surfType != null)
        {
            foreach (var s in UnityEngine.Object.FindObjectsByType(surfType, FindObjectsSortMode.None))
            {
                surfType.GetMethod("BuildNavMesh", Type.EmptyTypes)?.Invoke(s, null);
                baked++;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("[Plog] Abismo marcado como Not Walkable en " + marked + " piezas. NavMeshSurface horneados: " + baked +
                  (baked == 0 ? ". No encontré NavMeshSurface: hornea desde la pestaña Navigation (Bake)." : "."));
    }
}
#endif