using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class PlogPaletteSetup : AssetPostprocessor
{
    const string MatPath = "Assets/PlogPalette.mat";

    [MenuItem("Plog/1. Configurar paleta y asignar a todos los modelos")]
    static void Setup()
    {
        // 1) Textura: Point, sin mipmaps, sin compresión
        string[] guids = AssetDatabase.FindAssets("palette t:Texture2D");
        if (guids.Length == 0) { Debug.LogError("No encuentro palette.png"); return; }
        string texPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        var ti = (TextureImporter)AssetImporter.GetAtPath(texPath);
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.SaveAndReimport();
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

        // 2) Material (URP si existe, si no Standard)
        bool urp = GraphicsSettings.currentRenderPipeline != null;
        var shader = Shader.Find(urp ? "Universal Render Pipeline/Lit" : "Standard");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, MatPath); }
        mat.shader = shader;
        mat.mainTexture = tex;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0f);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        // 3) Reimportar todos los FBX (el postprocessor asigna el material)
        foreach (var g in AssetDatabase.FindAssets("t:Model"))
            AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(g), ImportAssetOptions.ForceUpdate);
        Debug.Log("Paleta asignada a todos los modelos.");
    }

    void OnPostprocessModel(GameObject go)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null) return;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var arr = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < arr.Length; i++) arr[i] = mat;
            r.sharedMaterials = arr;
        }
    }
}