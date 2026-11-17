using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEngine;
using UnityEngine.SceneManagement;

// Menu: Plog > Grabar cinematica (escena abierta)  -> MP4 en <proyecto>/Grabaciones/<escena>.mp4
// Abre la escena de la cinematica, deja visible la ventana Game y ejecuta el menu. Para sola al terminar.
[InitializeOnLoad]
public static class PlogGrabarCinematica
{
    const string Flag = "PlogGrabar_activo";
    const string Nombre = "PlogGrabar_nombre";
    static RecorderController rc;
    static double inicio, duracion;
    static string escenaInicial;

    static PlogGrabarCinematica()
    {
        EditorApplication.playModeStateChanged += AlCambiarModo;
    }

    [MenuItem("Plog/Grabar cinematica (escena abierta)")]
    static void Grabar()
    {
        if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Grabar", "Sal del modo Play antes de grabar.", "OK"); return; }
        var cam = Object.FindFirstObjectByType<CinematicaIntroController>();
        if (cam == null) { EditorUtility.DisplayDialog("Grabar", "Esta escena no tiene CinematicaIntroController.", "OK"); return; }
        if (!EditorSceneManager_Guardada()) return;
        EditorPrefs.SetBool(Flag, true);
        EditorPrefs.SetString(Nombre, SceneManager.GetActiveScene().name);
        EditorApplication.isPlaying = true;
    }

    static bool EditorSceneManager_Guardada()
    {
        return UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
    }

    static void AlCambiarModo(PlayModeStateChange e)
    {
        if (e == PlayModeStateChange.EnteredPlayMode && EditorPrefs.GetBool(Flag, false))
        {
            Empezar();
        }
        else if (e == PlayModeStateChange.ExitingPlayMode)
        {
            Terminar(false);
        }
    }

    static void Empezar()
    {
        var c = Object.FindFirstObjectByType<CinematicaIntroController>();
        if (c == null) { EditorPrefs.SetBool(Flag, false); Debug.LogError("Plog: no hay CinematicaIntroController en play."); return; }
        duracion = c.fadeInicial + c.fadeFinal + 0.5;
        foreach (var t in c.tomas) duracion += t.duracion;
        escenaInicial = SceneManager.GetActiveScene().name;

        string carpeta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Grabaciones"));
        Directory.CreateDirectory(carpeta);
        var cs = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        var mv = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        mv.name = "Plog Movie"; mv.Enabled = true;
        mv.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        mv.VideoBitRateMode = VideoBitrateMode.High;
        mv.ImageInputSettings = new GameViewInputSettings { OutputWidth = 1920, OutputHeight = 1080 };
        mv.AudioInputSettings.PreserveAudio = true;
        mv.OutputFile = Path.Combine(carpeta, EditorPrefs.GetString(Nombre, escenaInicial));
        cs.AddRecorderSettings(mv);
        cs.SetRecordModeToManual();
        cs.FrameRate = 30f;
        RecorderOptions.VerboseMode = false;
        rc = new RecorderController(cs);
        rc.PrepareRecording();
        rc.StartRecording();
        inicio = EditorApplication.timeSinceStartup;
        EditorApplication.update += Vigilar;
        Debug.Log("Plog: grabando (" + duracion.ToString("0.0") + " s) -> " + carpeta);
    }

    static void Vigilar()
    {
        bool cambioEscena = SceneManager.GetActiveScene().name != escenaInicial;
        if (EditorApplication.timeSinceStartup - inicio >= duracion || cambioEscena)
        {
            Terminar(true);
        }
    }

    static void Terminar(bool salirDePlay)
    {
        EditorApplication.update -= Vigilar;
        if (rc != null && rc.IsRecording()) { rc.StopRecording(); Debug.Log("Plog: grabacion terminada. Revisa la carpeta Grabaciones junto a Assets."); }
        rc = null;
        if (EditorPrefs.GetBool(Flag, false)) EditorPrefs.SetBool(Flag, false);
        if (salirDePlay && EditorApplication.isPlaying) EditorApplication.isPlaying = false;
    }
}
