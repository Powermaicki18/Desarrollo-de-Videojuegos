using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

// Plog > Optimizar > ... : ajustes graficos para celular y estaticos en las escenas de juego.
public static class PlogOptimizadorMovil
{
    const string RutaURP = "Assets/Settings/Mobile_RPAsset.asset";

    [MenuItem("Plog/Optimizar/1 Ajustes graficos para celular")]
    public static void AjustesGraficos()
    {
        var asset = AssetDatabase.LoadAssetAtPath<Object>(RutaURP);
        if (asset == null) { Debug.LogError("[Optimizar] No encuentro " + RutaURP); return; }
        string copia = "Assets/Settings/Mobile_RPAsset_ANTES.asset";
        if (AssetDatabase.LoadAssetAtPath<Object>(copia) == null) AssetDatabase.CopyAsset(RutaURP, copia);   // respaldo de como estaba

        var so = new SerializedObject(asset);
        void B(string n, bool v) { var p = so.FindProperty(n); if (p != null) p.boolValue = v; else Debug.LogWarning("[Optimizar] No existe " + n); }
        void I(string n, int v) { var p = so.FindProperty(n); if (p != null) p.intValue = v; else Debug.LogWarning("[Optimizar] No existe " + n); }
        void F(string n, float v) { var p = so.FindProperty(n); if (p != null) p.floatValue = v; else Debug.LogWarning("[Optimizar] No existe " + n); }

        F("m_RenderScale", 0.85f);          // antes 0.75 (se veia borroso)
        I("m_MSAA", 4);                     // bordes limpios; en GPUs de celular (por tiles) es barato
        B("m_SupportsHDR", false);          // ahorra ancho de banda; no hay postprocesado que lo necesite
        B("m_MainLightShadowsSupported", true);   // antes estaban apagadas: las sombras dan profundidad
        I("m_MainLightShadowmapResolution", 1024);
        F("m_ShadowDistance", 25f);         // vista top-down: no hace falta mas
        I("m_ShadowCascadeCount", 1);
        B("m_SoftShadowsSupported", false);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        Debug.Log("[Optimizar] Ajustes graficos aplicados a " + RutaURP + " (respaldo: " + copia + ")");
    }

    [MenuItem("Plog/Optimizar/2 Marcar objetos estaticos (escena abierta)")]
    public static void MarcarEstaticos()
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        var add = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;
        int n = 0, saltados = 0;
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (r.gameObject.scene != scene) continue;
            if (SeMueve(r.transform)) { saltados++; continue; }
            var f = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
            if ((f & add) == add) continue;
            GameObjectUtility.SetStaticEditorFlags(r.gameObject, f | add);   // se conserva lo que ya tenia (p. ej. la iluminacion)
            n++;
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[Optimizar] " + n + " objetos marcados como estaticos, " + saltados + " saltados por moverse. Guarda la escena (Ctrl+S).");
    }

    static bool SeMueve(Transform t)
    {
        for (var x = t; x != null; x = x.parent)
        {
            string nm = x.name;
            if (nm.Contains("Player") || nm.Contains("Enemy") || nm.Contains("Totem") || nm.Contains("Estanque") || nm.Contains("Pickup") || nm.Contains("Spawner")) return true;
            if (x.GetComponent<Animator>() || x.GetComponent<Rigidbody>() || x.GetComponent<CharacterController>() || x.GetComponent<NavMeshAgent>()
                || x.GetComponent<ParticleSystem>() || x.GetComponent<Canvas>()) return true;
            foreach (var mb in x.GetComponents<MonoBehaviour>()) if (mb != null) return true;   // si tiene un script, puede moverse
        }
        return false;
    }
}
