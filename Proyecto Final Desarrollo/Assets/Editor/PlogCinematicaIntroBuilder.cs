using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Plog > Crear cinematica del intro: arma la escena CinematicaIntro con lo que ya esta modelado en el proyecto.
public static partial class PlogCinematicaIntroBuilder
{
    const string EscenaOrigen = "Assets/Scenes/PrimerMapa.unity";
    const string EscenaDestino = "Assets/Scenes/CinematicaIntro.unity";
    const string Carpeta = "Assets/Cinematica";
    const string TukoDir = "Assets/Personajes/Tuko/";
    static readonly string[] RaicesGameplay = {
        "PausaManager","PlogEnemyPointer","HUD","MobileControls","EnemySpawner","Canvas","CanvasInstrucciones",
        "GameOverManager","VictoriaManager","EstanqueVida","EstanqueVida (1)","EstanqueVida (2)","PlogHealPlayer","Aviso","Player",
        "VictoriaDebugManager","CanvasGameOver","CanvasVictoria" };

    static Vector3 O;           // origen del set (en el suelo)
    static Transform tomasRoot, actoresRoot, setRoot;

    [MenuItem("Plog/Crear cinematica del intro")]
    public static void Crear()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(EscenaDestino) != null)
        {
            // no se borra nada: la anterior se renombra
            string viejo = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/CinematicaIntro_anterior.unity");
            AssetDatabase.MoveAsset(EscenaDestino, viejo);
            AssetDatabase.Refresh();
            var lst = EditorBuildSettings.scenes.Where(s => !s.path.Contains("CinematicaIntro_anterior")).ToArray();
            EditorBuildSettings.scenes = lst;
            Debug.Log("[Cinematica] La escena anterior quedo como " + viejo);
        }
        AssetDatabase.Refresh();
        if (!AssetDatabase.IsValidFolder(Carpeta)) AssetDatabase.CreateFolder("Assets", "Cinematica");

        // 1) animaciones nuevas de Tuko
        ConfigurarClip(TukoDir + "Tuko_Caminar.fbx", true);
        ConfigurarClip(TukoDir + "Tuko_Sorpresa.fbx", false);
        ConfigurarClip(TukoDir + "Tuko_Mirar.fbx", false);
        var ctrl = CrearControlador();
        ConfigurarPueblo("Alien"); ConfigurarPueblo("Dino");

        // 2) escena = copia de PrimerMapa (el original no se toca)
        AssetDatabase.CopyAsset(EscenaOrigen, EscenaDestino);
        AssetDatabase.Refresh();
        var scene = EditorSceneManager.OpenScene(EscenaDestino, OpenSceneMode.Single);

        LimpiarEscena(scene);
        Physics.SyncTransforms();
        CalcularOrigen();

        setRoot = new GameObject("Set_Cinematica").transform; setRoot.position = O;
        tomasRoot = new GameObject("Tomas").transform; tomasRoot.SetParent(setRoot, true);
        actoresRoot = new GameObject("Actores").transform; actoresRoot.SetParent(setRoot, true);

        // 3) escenografia
        var matPiedra = Mat("Piedra", new Color(0.55f, 0.52f, 0.5f));
        var matRojo = Mat("BanderaRoja", new Color(0.85f, 0.12f, 0.1f));
        var matMorado = Mat("BanderaMorada", new Color(0.45f, 0.18f, 0.72f));
        var matMadera = Mat("Madera", new Color(0.4f, 0.26f, 0.14f));
        var matAnillo = Mat("Anillo", new Color(0.35f, 0.35f, 0.38f));
        var matSombra = MatUnlit("Silueta", new Color(0.02f, 0.02f, 0.05f));

        var pedL = Pedestal("Pedestal_Izq", new Vector3(-2.4f, 0, 8f), matPiedra);
        var pedR = Pedestal("Pedestal_Der", new Vector3(2.4f, 0, 8f), matPiedra);
        var totL = Totem("Totem_Hex", new Vector3(-2.4f, 0.3f, 8f), new Color(1f, 0.55f, 0.15f));
        var totR = Totem("Totem_Tierra", new Vector3(2.4f, 0.3f, 8f), new Color(0.2f, 0.9f, 0.8f));

        var tRoja = MatBandera("TelaBanderaRoja", "bandera_Roja_BaseColor.png");
        var tMorada = MatBandera("TelaBanderaMorada", "bandera_Morada_BaseColor.png");
        var banRojaIzq = BanderaFBX("Bandera_Roja_1", new Vector3(-6.5f, 0, 6f), tRoja, matMadera, 1.0f);
        var banMoradaDer = BanderaFBX("Bandera_Morada_1", new Vector3(6.5f, 0, 6f), tMorada, matMadera, 1.0f);
        var banMoradaPista = BanderaFBX("Pista_Morada", new Vector3(-2.2f, 0, 6.2f), tMorada, matMadera, 0.8f); banMoradaPista.SetActive(false);
        var banRojaPista = BanderaFBX("Pista_Roja", new Vector3(2.2f, 0, 6.2f), tRoja, matMadera, 0.8f); banRojaPista.SetActive(false);
        var banRojaGrande = BanderaFBX("Pueblo_Rojo", new Vector3(-4.5f, 0, 10f), tRoja, matMadera, 1.6f); banRojaGrande.SetActive(false);
        var banMoradaGrande = BanderaFBX("Pueblo_Morado", new Vector3(4.5f, 0, 10f), tMorada, matMadera, 1.6f); banMoradaGrande.SetActive(false);
        // aldeanos: Alien = pueblo morado, Dino = pueblo rojo (lideres de la toma de pistas + grupos de la toma de pueblos)
        var liderMorado = Aldeano("Alien", "Lider_Morado", new Vector3(-3.0f, 0, 5.2f), 1.7f);
        var liderRojo = Aldeano("Dino", "Lider_Rojo", new Vector3(3.0f, 0, 5.2f), 1.7f);
        var dinoA = Aldeano("Dino", "Dino_A", new Vector3(-4.6f, 0, 6.4f), 1.6f);
        var dinoB = Aldeano("Dino", "Dino_B", new Vector3(-6.4f, 0, 7.6f), 1.5f);
        var dinoC = Aldeano("Dino", "Dino_C", new Vector3(-5.4f, 0, 9.2f), 1.6f);
        var alienA = Aldeano("Alien", "Alien_A", new Vector3(4.6f, 0, 6.4f), 1.6f);
        var alienB = Aldeano("Alien", "Alien_B", new Vector3(6.4f, 0, 7.6f), 1.5f);
        var alienC = Aldeano("Alien", "Alien_C", new Vector3(5.4f, 0, 9.2f), 1.6f);
        var grupoPueblos = new[] { dinoA, dinoB, dinoC, alienA, alienB, alienC };
        foreach (var g in new[] { liderMorado, liderRojo, dinoA, dinoB, dinoC, alienA, alienB, alienC }) if (g) g.SetActive(false);

        var tuko = Actor(TukoDir + "Player.prefab", "Tuko", new Vector3(0, 0, -4f), null);
        var tAnim = tuko ? tuko.GetComponentInChildren<Animator>() : null;
        if (tAnim != null && ctrl != null) tAnim.runtimeAnimatorController = ctrl;
        if (tuko) tuko.SetActive(false);

        var ladron1 = Actor("Assets/Personajes/Mogo/Mogo_Idle.prefab", "Ladron_1", new Vector3(-5f, 0, 15f), matSombra);
        var ladron2 = Actor("Assets/Personajes/Mogo/Mogo_Idle.prefab", "Ladron_2", new Vector3(5f, 0, 15f), matSombra);
        var murcielago = Actor("Assets/Personajes/Nox/Enemy_Nox.prefab", "Murcielago", new Vector3(-9f, 5f, 12f), matSombra);
        var nox1 = Actor("Assets/Personajes/Nox/Enemy_Nox.prefab", "Nox_1", new Vector3(-8f, 0, 10f), null);
        var nox2 = Actor("Assets/Personajes/Nox/Enemy_Nox.prefab", "Nox_2", new Vector3(-9.5f, 0, 11.5f), null);
        var goro1 = Actor("Assets/Personajes/Goro/Enemy_Goro.prefab", "Goro_1", new Vector3(8f, 0, 10f), null);
        var goro2 = Actor("Assets/Personajes/Goro/Enemy_Goro.prefab", "Goro_2", new Vector3(9.5f, 0, 11.5f), null);
        foreach (var g in new[] { ladron1, ladron2, murcielago, nox1, nox2, goro1, goro2 }) if (g) g.SetActive(false);

        // 4) controlador y tomas
        var cam = Camera.main;
        if (cam == null) { var cg = new GameObject("Main Camera"); cg.tag = "MainCamera"; cam = cg.AddComponent<Camera>(); cg.AddComponent<AudioListener>(); }
        foreach (var mb in cam.GetComponents<MonoBehaviour>())
            if (mb != null && (mb.GetType().Name == "CameraFollow" || mb.GetType().Name == "CameraOclusion")) Object.DestroyImmediate(mb);

        var ctlGo = new GameObject("CinematicaIntro");
        var ctl = ctlGo.AddComponent<CinematicaIntroController>();
        ctl.camara = cam;
        ctl.luzDireccional = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
        ctl.girar = new[] { totL.transform, totR.transform };

        var T = new List<CinematicaIntroController.Toma>();
        int n = 1;
        CinematicaIntroController.Toma Nueva(string nombre, float dur, string texto, Vector3 pA, Vector3 lA, Vector3 pB, Vector3 lB)
        {
            int idx = n++;
            var go = new GameObject(string.Format("Toma_{0:00}_{1}", idx, nombre)); go.transform.SetParent(tomasRoot, true);
            var t = new CinematicaIntroController.Toma { nombre = nombre, duracion = dur, texto = texto };
            t.voz = AssetDatabase.LoadAssetAtPath<AudioClip>(string.Format("Assets/Cinematica/Voz/Voz_{0:00}.wav", idx));
            t.camaraInicio = Marcador(go.transform, "Cam_Inicio", pA, lA);
            t.camaraFin = Marcador(go.transform, "Cam_Fin", pB, lB);
            T.Add(t); return t;
        }
        CinematicaIntroController.MovimientoActor Mov(CinematicaIntroController.Toma t, GameObject a, Vector3 de, Vector3 haciaDe, Vector3 ha, Vector3 haciaHa, string anim, bool repetir)
        {
            if (a == null) return null;
            var parent = tomasRoot.Find(string.Format("Toma_{0:00}_{1}", n - 1, t.nombre));
            var m = new CinematicaIntroController.MovimientoActor { actor = a.transform, animacion = anim, repetir = repetir };
            m.desde = Marcador(parent, a.name + "_Desde", de, de + haciaDe);
            m.hasta = Marcador(parent, a.name + "_Hasta", ha, ha + haciaHa);
            t.actores.Add(m); return m;
        }
        Vector3 Z = Vector3.forward;
        Vector3 W(float x, float y, float z) { return new Vector3(x, y, z); }

        // 1: bosque y totems
        var t1 = Nueva("Bosque", 9f, "En el mundo de Plog, dos tótems ancestrales mantenían el equilibrio entre los pueblos.", W(0, 9, -6), W(0, 2, 8), W(0, 2.6f, 0), W(0, 2, 8));
        t1.entraDesdeNegro = true; t1.duracionEntrada = 1.2f; t1.nocheInicio = 0f; t1.nocheFin = 0f;
        // 2: tótems girando
        var t2 = Nueva("Totems", 8f, "", W(-5, 2.4f, 3), W(0, 2, 8), W(5, 2.4f, 3), W(0, 2, 8));
        // 3: noche, robo
        var t3 = Nueva("Robo", 12f, "Hasta que una noche, alguien los robó.", W(0, 2.2f, -1), W(0, 1.8f, 8), W(0, 2.0f, 0.5f), W(0, 1.8f, 8));
        t3.nocheInicio = 0.2f; t3.nocheFin = 1f; t3.destelloAlFinal = true;
        t3.activarAlInicio = new[] { ladron1, ladron2, murcielago }; t3.desactivarAlFinal = new[] { totL, totR, ladron1, ladron2, murcielago };
        Mov(t3, ladron1, W(-5, 0, 15), Z * -1f + W(0.4f, 0, 0), W(-1.4f, 0, 9.5f), W(0.3f, 0, -1), "", false);
        Mov(t3, ladron2, W(5, 0, 15), Z * -1f - W(0.4f, 0, 0), W(1.4f, 0, 9.5f), W(-0.3f, 0, -1), "", false);
        Mov(t3, murcielago, W(-9, 5, 12), W(1, 0, 0), W(9, 6, 12), W(1, 0, 0), "", false);
        // 4: pedestales vacios + huida de los ladrones con los totems (brillos pequenos)
        var brasaL = Brillo("Brasa_Izq", setRoot, new Vector3(-2.4f, 0.75f, 8f), new Color(1f, 0.55f, 0.15f), 0.18f, 1.6f);
        var brasaR = Brillo("Brasa_Der", setRoot, new Vector3(2.4f, 0.75f, 8f), new Color(0.2f, 0.9f, 0.8f), 0.18f, 1.6f);
        brasaL.SetActive(false); brasaR.SetActive(false);
        var brilloL = ladron1 ? Brillo("Totem_Robado", ladron1.transform, Vector3.zero, new Color(1f, 0.55f, 0.15f), 0.45f, 3f) : null;
        var brilloR = ladron2 ? Brillo("Totem_Robado", ladron2.transform, Vector3.zero, new Color(0.2f, 0.9f, 0.8f), 0.45f, 3f) : null;
        foreach (var bg in new[] { brilloL, brilloR }) if (bg) { bg.transform.localPosition = new Vector3(0f, 1.0f, 0.35f); }
        var t4 = Nueva("PedestalesVacios", 8f, "", W(-1.2f, 0.9f, 3.8f), W(-2.0f, 0.7f, 8f), W(0.4f, 2.2f, 2.0f), W(0, 1.2f, 16f));
        t4.activarAlInicio = new[] { ladron1, ladron2, brasaL, brasaR }; t4.desactivarAlFinal = new[] { ladron1, ladron2, brasaL, brasaR };
        t4.nocheInicio = 1f; t4.nocheFin = 1f; t4.fovInicio = 48f; t4.fovFin = 50f;
        Mov(t4, ladron1, W(-1.4f, 0, 9.5f), Z, W(-7f, 0, 34f), Z, "", false);
        Mov(t4, ladron2, W(1.4f, 0, 9.5f), Z, W(7f, 0, 34f), Z, "", false);
        // 5: pistas falsas (pantalla dividida como en el comic: izquierda pueblo morado, derecha pueblo rojo)
        var t5 = Nueva("Pistas", 6f, "Y dejaron pistas falsas para engañar a todos.", W(-2.3f, 1.5f, 2.2f), W(-2.2f, 1.6f, 6.2f), W(-2.3f, 1.5f, 3.4f), W(-2.2f, 1.6f, 6.2f));
        t5.fovInicio = 42f; t5.fovFin = 36f;
        t5.panelDividido = true;
        var padre5 = tomasRoot.Find(string.Format("Toma_{0:00}_{1}", n - 1, "Pistas"));
        t5.camara2Inicio = Marcador(padre5, "Cam2_Inicio", W(2.3f, 1.5f, 2.2f), W(2.2f, 1.6f, 6.2f));
        t5.camara2Fin = Marcador(padre5, "Cam2_Fin", W(2.3f, 1.5f, 3.4f), W(2.2f, 1.6f, 6.2f));
        t5.activarAlInicio = new[] { banMoradaPista, banRojaPista, liderMorado, liderRojo };
        t5.desactivarAlFinal = new[] { banMoradaPista, banRojaPista, liderMorado, liderRojo };
        t5.nocheInicio = 1f; t5.nocheFin = 1f;
        Mov(t5, liderMorado, W(-3.0f, 0, 5.2f), W(1f, 0, 0.5f), W(-3.0f, 0, 5.2f), W(1f, 0, 0.5f), "", false);
        Mov(t5, liderRojo, W(3.0f, 0, 5.2f), W(-1f, 0, 0.5f), W(3.0f, 0, 5.2f), W(-1f, 0, 0.5f), "", false);
        n++;   // el indice de voz 06 no existe: asi la toma siguiente conserva su Voz_07
        // 6: pueblos enfrentados
        var t6 = Nueva("PueblosEnfrentados", 9f, "Los pueblos se culparon entre sí, y la paz se rompió.", W(0, 2.5f, -2), W(0, 2, 10), W(0, 2.2f, 3), W(0, 2, 10));
        t6.activarAlInicio = new[] { banRojaGrande, banMoradaGrande, dinoA, dinoB, dinoC, alienA, alienB, alienC }; t6.destelloAlFinal = true;
        t6.desactivarAlFinal = grupoPueblos;
        Mov(t6, dinoA, W(-4.6f, 0, 6.4f), W(1, 0, 0.35f), W(-4.6f, 0, 6.4f), W(1, 0, 0.35f), "Punch", true);
        Mov(t6, dinoB, W(-6.4f, 0, 7.6f), W(1, 0, 0.35f), W(-6.4f, 0, 7.6f), W(1, 0, 0.35f), "No", true);
        Mov(t6, dinoC, W(-5.4f, 0, 9.2f), W(1, 0, 0.35f), W(-5.4f, 0, 9.2f), W(1, 0, 0.35f), "Wave", true);
        Mov(t6, alienA, W(4.6f, 0, 6.4f), W(-1, 0, 0.35f), W(4.6f, 0, 6.4f), W(-1, 0, 0.35f), "Punch", true);
        Mov(t6, alienB, W(6.4f, 0, 7.6f), W(-1, 0, 0.35f), W(6.4f, 0, 7.6f), W(-1, 0, 0.35f), "No", true);
        Mov(t6, alienC, W(5.4f, 0, 9.2f), W(-1, 0, 0.35f), W(5.4f, 0, 9.2f), W(-1, 0, 0.35f), "Wave", true);
        // 7: rebeldes
        var t7 = Nueva("Rebeldes", 11f, "Aprovechando el caos, los rebeldes se apoderaron del Bosque y del Desierto.", W(-7, 2, 4), W(0, 1, 10.5f), W(7, 2, 4), W(0, 1, 10.5f));
        t7.nocheInicio = 0f; t7.nocheFin = 0f; t7.desactivarAlInicio = new[] { banRojaGrande, banMoradaGrande, banRojaPista, banMoradaPista };
        t7.activarAlInicio = new[] { nox1, nox2, goro1, goro2 }; t7.desactivarAlFinal = new[] { nox1, nox2, goro1, goro2 };
        Mov(t7, nox1, W(-8, 0, 10), W(1, 0, 0), W(-1.2f, 0, 10), W(1, 0, 0), "", false);
        Mov(t7, nox2, W(-9.5f, 0, 11.5f), W(1, 0, 0), W(-2.7f, 0, 11.5f), W(1, 0, 0), "", false);
        Mov(t7, goro1, W(8, 0, 10), W(-1, 0, 0), W(1.2f, 0, 10), W(-1, 0, 0), "", false);
        Mov(t7, goro2, W(9.5f, 0, 11.5f), W(-1, 0, 0), W(2.7f, 0, 11.5f), W(-1, 0, 0), "", false);
        // 8a: Tuko camina
        var t8a = Nueva("TukoCamina", 8f, "Pero Tuko, un viajero Ambulator, descubrió la verdad.", W(2, 2, -8), W(0, 1, -3.5f), W(2, 2, -2), W(0, 1, 5));
        t8a.activarAlInicio = new[] { tuko }; t8a.enfocar = tuko ? tuko.transform : null; t8a.alturaEnfoque = 1.0f;
        Mov(t8a, tuko, W(0, 0, -4), Z, W(0, 0, 5.2f), Z, "Caminar", true);
        // 8b: sorpresa
        var t8b = Nueva("TukoSorpresa", 4f, "", W(0.9f, 1.4f, 7.6f), W(0, 1.2f, 5.4f), W(0.35f, 1.3f, 6.8f), W(0, 1.3f, 5.4f));
        Mov(t8b, tuko, W(0, 0, 5.2f), Z, W(0, 0, 5.2f), Z, "Sorpresa", false);
        t8b.fovInicio = 50f; t8b.fovFin = 40f; t8b.enfocar = tuko ? tuko.transform : null; t8b.alturaEnfoque = 1.3f;
        // 9: mira al horizonte
        var t9 = Nueva("TukoMira", 10f, "Ahora debe recuperar los tótems y devolver el equilibrio a Plog.", W(-2.5f, 0.6f, 3.2f), W(0, 1.2f, 5.4f), W(2.5f, 1.6f, 3.2f), W(0, 1.4f, 5.4f));
        Mov(t9, tuko, W(0, 0, 5.2f), Z, W(0, 0, 5.2f), Z, "Mirar", false);
        t9.enfocar = tuko ? tuko.transform : null; t9.alturaEnfoque = 1.3f;
        // 10: corre a la camara + titulo
        var t10 = Nueva("TukoCorre", 5f, "", W(0, 1.3f, 12.5f), W(0, 1f, 4), W(0, 1.3f, 12.5f), W(0, 1f, 4));
        t10.esTitulo = false; t10.salidaANegro = true;
        Mov(t10, tuko, W(0, 0, 4), Z, W(0, 0, 10.5f), Z, "Correr", true).curva = AnimationCurve.EaseInOut(0, 0, 1, 1);
        t10.fovInicio = 55f; t10.fovFin = 55f; t10.enfocar = tuko ? tuko.transform : null; t10.alturaEnfoque = 1.0f;

        ctl.tomas = T;
        Selection.activeGameObject = ctlGo;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        var lista = EditorBuildSettings.scenes.ToList();
        if (!lista.Any(s => s.path == EscenaDestino)) { lista.Add(new EditorBuildSettingsScene(EscenaDestino, true)); EditorBuildSettings.scenes = lista.ToArray(); }

        Debug.Log("[Cinematica] Escena creada: " + EscenaDestino + ". Dale Play. Mueve los marcadores en 'Set_Cinematica/Tomas' para ajustar camaras.");
    }

    // ---------------- animaciones ----------------
    static void ConfigurarClip(string path, bool loop)
    {
        var mi = AssetImporter.GetAtPath(path) as ModelImporter;
        if (mi == null) { Debug.LogWarning("[Cinematica] No encuentro " + path); return; }
        var idle = AssetDatabase.LoadAllAssetsAtPath(TukoDir + "Tuko idle.fbx").OfType<Avatar>().FirstOrDefault();
        mi.animationType = ModelImporterAnimationType.Generic;
        if (idle != null) { mi.avatarSetup = ModelImporterAvatarSetup.CopyFromOther; mi.sourceAvatar = idle; }
        mi.importAnimation = true;
        mi.materialImportMode = ModelImporterMaterialImportMode.None;
        var clips = mi.defaultClipAnimations;
        foreach (var c in clips) { c.loopTime = loop; c.loopPose = loop; }
        mi.clipAnimations = clips;
        mi.SaveAndReimport();
    }

    static AnimationClip Clip(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
    }

    static AnimatorController CrearControlador()
    {
        string p = Carpeta + "/Tuko_Cinematica.controller";
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(p);
        var sm = ctrl.layers[0].stateMachine;
        void Estado(string nombre, string fbx, bool def)
        {
            var c = Clip(TukoDir + fbx);
            if (c == null) { Debug.LogWarning("[Cinematica] Sin clip en " + fbx); return; }
            var s = sm.AddState(nombre); s.motion = c;
            if (def) sm.defaultState = s;
        }
        Estado("Idle", "Tuko idle.fbx", true);
        Estado("Caminar", "Tuko_Caminar.fbx", false);
        Estado("Sorpresa", "Tuko_Sorpresa.fbx", false);
        Estado("Mirar", "Tuko_Mirar.fbx", false);
        Estado("Correr", "Run.fbx", false);
        return ctrl;
    }

    // ---------------- escena ----------------
    static void LimpiarEscena(Scene scene, params string[] conservar)
    {
        foreach (var r in scene.GetRootGameObjects())
            if (RaicesGameplay.Contains(r.name) && !conservar.Contains(r.name)) Object.DestroyImmediate(r);
        foreach (var z in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (z != null && (z.GetType().Name == "PlogHealZone" || z.GetType().Name == "Health")) Object.DestroyImmediate(z.gameObject);
    }

    static void CalcularOrigen()
    {
        Vector3 c = Vector3.zero;
        var suelo = GameObject.Find("Suelo");
        if (suelo != null) { var r = suelo.GetComponentInChildren<Renderer>(); if (r != null) c = r.bounds.center; }
        // el piso es la cara de arriba del pasto principal (el raycast podia pegar en props mas altos)
        float y = 0f;
        var pasto = GameObject.Find("Pasto_Principal");
        if (pasto != null && pasto.GetComponent<Renderer>() != null) y = pasto.GetComponent<Renderer>().bounds.max.y;
        else if (Physics.Raycast(new Vector3(c.x, 100f, c.z), Vector3.down, out var h, 300f)) y = h.point.y;
        O = new Vector3(c.x, y, c.z);
    }

    static Transform Marcador(Transform parent, string nombre, Vector3 local, Vector3 mirarLocal)
    {
        var go = new GameObject(nombre); go.transform.SetParent(parent, true);
        go.transform.position = O + local;
        Vector3 dir = (O + mirarLocal) - go.transform.position;
        if (dir.sqrMagnitude > 0.0001f) go.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        return go.transform;
    }

    // esfera brillante + luz puntual (marca de "totem" sin usar puas ni modelos extra)
    static GameObject Brillo(string n, Transform padre, Vector3 local, Color c, float diametro, float intensidad)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.DestroyImmediate(g.GetComponent<Collider>());
        g.name = n; g.transform.SetParent(padre, false);
        g.transform.position = padre == setRoot ? O + local : padre.position + local;
        g.transform.localScale = Vector3.one * diametro;
        g.GetComponent<Renderer>().sharedMaterial = MatUnlit("Brillo_" + ColorUtility.ToHtmlStringRGB(c), c);
        var lg = new GameObject("Luz"); lg.transform.SetParent(g.transform, false);
        var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = c; l.intensity = intensidad; l.range = 4f;
        return g;
    }

    static GameObject Pedestal(string n, Vector3 local, Material m)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name = n;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(setRoot, true); g.transform.position = O + local + Vector3.up * 0.15f;
        g.transform.localScale = new Vector3(1.8f, 0.15f, 1.8f);
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }

    static GameObject Totem(string nombre, Vector3 local, Color luz, float altura = 2.4f, float intensidadLuz = 3f)
    {
        string modeloTotem = nombre.Contains("Hex") ? "Totem_Hex" : (nombre.Contains("Tierra") ? "Totem_Tierra" : nombre);
        var guids = AssetDatabase.FindAssets(modeloTotem + " t:Model");
        GameObject prefab = guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
        GameObject g;
        if (prefab != null)
        {
            g = (GameObject)Object.Instantiate(prefab);
            // usa el material URP propio del totem (el del FBX se veia con colores rotos)
            string rutaFbx = AssetDatabase.GUIDToAssetPath(guids[0]);
            var matT = AssetDatabase.LoadAssetAtPath<Material>(System.IO.Path.ChangeExtension(rutaFbx, ".mat"));
            if (matT != null) foreach (var rr in g.GetComponentsInChildren<Renderer>(true)) { var arr = new Material[rr.sharedMaterials.Length]; for (int i = 0; i < arr.Length; i++) arr[i] = matT; rr.sharedMaterials = arr; }
        }
        else { g = GameObject.CreatePrimitive(PrimitiveType.Cube); Debug.LogWarning("[Cinematica] No encontre " + nombre); }
        g.name = nombre;
        g.transform.SetParent(setRoot, true);
        g.transform.position = O + local;
        // escalar a ~2.4 m de alto
        var rs = g.GetComponentsInChildren<Renderer>();
        if (rs.Length > 0)
        {
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            if (b.size.y > 0.01f) g.transform.localScale *= altura / b.size.y;
            g.transform.position += Vector3.up * (O.y + local.y - b.min.y);
        }
        var lg = new GameObject("Luz"); lg.transform.SetParent(g.transform, true);
        lg.transform.position = g.transform.position + Vector3.up * (altura * 0.66f);
        var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = luz; l.intensity = intensidadLuz; l.range = 7f;
        return g;
    }

    static GameObject AnilloPuas(Vector3 local, Material m)
    {
        var root = new GameObject("Anillo_Puas"); root.transform.SetParent(setRoot, true); root.transform.position = O + local;
        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI * 2f / 12f;
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.DestroyImmediate(c.GetComponent<Collider>());
            c.transform.SetParent(root.transform, false);
            c.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.5f, 0.06f, Mathf.Sin(a) * 0.5f);
            c.transform.localRotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0);
            c.transform.localScale = new Vector3(0.28f, 0.12f, 0.16f);
            c.GetComponent<Renderer>().sharedMaterial = m;
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.DestroyImmediate(p.GetComponent<Collider>());
            p.transform.SetParent(root.transform, false);
            p.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.68f, 0.12f, Mathf.Sin(a) * 0.68f);
            p.transform.localRotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 45f);
            p.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
            p.GetComponent<Renderer>().sharedMaterial = m;
        }
        return root;
    }

    static GameObject Bandera(string n, Vector3 local, Material tela, Material palo, float escala)
    {
        var root = new GameObject(n); root.transform.SetParent(setRoot, true); root.transform.position = O + local;
        var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.DestroyImmediate(p.GetComponent<Collider>());
        p.transform.SetParent(root.transform, false);
        p.transform.localScale = new Vector3(0.08f, 1.4f * escala, 0.08f); p.transform.localPosition = new Vector3(0, 1.4f * escala, 0);
        p.GetComponent<Renderer>().sharedMaterial = palo;
        var f = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.DestroyImmediate(f.GetComponent<Collider>());
        f.transform.SetParent(root.transform, false);
        f.transform.localScale = new Vector3(0.9f * escala, 0.7f * escala, 0.04f); f.transform.localPosition = new Vector3(0.45f * escala, 2.3f * escala, 0);
        f.GetComponent<Renderer>().sharedMaterial = tela;
        return root;
    }

    const string BanderaFbx = "Assets/Cinematica/Bandera/Bandera_Cinematica.fbx";
    const string BanderaTex = "Assets/Cinematica/Bandera/Texturas/";

    static Material MatBandera(string nombre, string texBase)
    {
        string p = Carpeta + "/Mat_" + nombre + ".mat";
        var normalPath = BanderaTex + "bandera_low_bandera_Normal.png";
        var ni = AssetImporter.GetAtPath(normalPath) as TextureImporter;
        if (ni != null && ni.textureType != TextureImporterType.NormalMap) { ni.textureType = TextureImporterType.NormalMap; ni.SaveAndReimport(); }
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(BanderaTex + texBase);
        var nor = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        if (tex != null) { m.SetTexture("_BaseMap", tex); m.SetColor("_BaseColor", Color.white); }
        if (nor != null) { m.SetTexture("_BumpMap", nor); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 0.6f); }
        m.SetFloat("_Smoothness", 0.2f);
        m.SetFloat("_Cull", 0f);          // la tela se ve por los dos lados
        EditorUtility.SetDirty(m);
        return m;
    }

    // Bandera colgante (modelo de Blender) sobre un palo. Si falta el FBX usa la bandera simple de antes.
    static GameObject BanderaFBX(string n, Vector3 local, Material tela, Material palo, float escala)
    {
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(BanderaFbx);
        if (fbx == null || tela == null) return Bandera(n, local, tela, palo, escala);
        var root = new GameObject(n); root.transform.SetParent(setRoot, true); root.transform.position = O + local;
        // palo detras de la tela
        var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.DestroyImmediate(p.GetComponent<Collider>());
        p.transform.SetParent(root.transform, false);
        p.transform.localScale = new Vector3(0.09f * escala, 1.45f * escala, 0.09f * escala);
        p.transform.localPosition = new Vector3(0, 1.45f * escala, 0.2f * escala);
        p.GetComponent<Renderer>().sharedMaterial = palo;
        var b = (GameObject)Object.Instantiate(fbx);
        b.name = "Tela"; b.transform.SetParent(root.transform, false);
        b.transform.localRotation = Quaternion.identity; b.transform.localScale = Vector3.one;
        foreach (var r in b.GetComponentsInChildren<Renderer>(true))
        { var arr = new Material[r.sharedMaterials.Length]; for (int i = 0; i < arr.Length; i++) arr[i] = tela; r.sharedMaterials = arr; }
        var rs = b.GetComponentsInChildren<Renderer>(true);
        var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
        if (bb.size.y < bb.size.x * 0.8f) Debug.LogWarning("[Cinematica] La bandera parece venir girada (alto " + bb.size.y + ", ancho " + bb.size.x + "). Revisar orientacion de " + BanderaFbx);
        float f = 1.4f * escala / Mathf.Max(0.0001f, bb.size.x);
        b.transform.localScale = Vector3.one * f;
        bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
        // barra superior a la altura del palo, centrada
        Vector3 delta = new Vector3(root.transform.position.x - bb.center.x, root.transform.position.y + 2.85f * escala - bb.max.y, root.transform.position.z - bb.center.z);
        b.transform.position += delta;
        return root;
    }

    // ---------------- aldeanos (Alien = pueblo morado, Dino = pueblo rojo) ----------------
    const string PueblosDir = "Assets/Personajes/Pueblos/";
    static readonly Dictionary<string, RuntimeAnimatorController> ctrlPueblo = new Dictionary<string, RuntimeAnimatorController>();

    static void ConfigurarPueblo(string ch)
    {
        string fbx = PueblosDir + ch + "/" + ch + ".fbx";
        var mi = AssetImporter.GetAtPath(fbx) as ModelImporter;
        if (mi == null) { Debug.LogWarning("[Cinematica] No encuentro " + fbx); return; }
        mi.animationType = ModelImporterAnimationType.Generic;
        mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        mi.importAnimation = true;
        var clips = mi.defaultClipAnimations;
        foreach (var c in clips) { bool idle = c.name.Contains("Idle_"); c.loopTime = idle; c.loopPose = idle; }
        mi.clipAnimations = clips;
        mi.SaveAndReimport();

        string p = Carpeta + "/Pueblo_" + ch + ".controller";
        var yaExiste = AssetDatabase.LoadAssetAtPath<AnimatorController>(p);
        if (yaExiste != null) { ctrlPueblo[ch] = yaExiste; return; }
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(p);
        var sm = ctrl.layers[0].stateMachine;
        var todos = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToList();
        foreach (var par in new[] { "Idle", "No", "Wave", "Punch" })
        {
            var clip = todos.FirstOrDefault(c => c.name.Contains(par + "_"));
            if (clip == null) { Debug.LogWarning("[Cinematica] " + ch + ": no hay clip " + par); continue; }
            var st = sm.AddState(par); st.motion = clip;
            if (par == "Idle") sm.defaultState = st;
        }
        ctrlPueblo[ch] = ctrl;
    }

    // Los modelos del pack no traen texturas: son colores planos por material. Se asignan colores explicitos
    // (en el orden de los materiales del FBX) para que no se vean como una sola mancha oscura.
    static readonly Dictionary<string, Color[]> paletaPueblo = new Dictionary<string, Color[]>
    {
        // Alien: Main, Secondary, Eye_White, Eye_Black, Tongue
        { "Alien", new[] { new Color(0.50f, 0.22f, 0.70f), new Color(0.40f, 0.70f, 0.22f), new Color(0.95f, 0.95f, 0.98f), new Color(0.05f, 0.05f, 0.08f), new Color(0.80f, 0.35f, 0.45f) } },
        // Dino: Main, Secondary, Eye_Black, Eye_White, Teeth, Tongue
        { "Dino", new[] { new Color(0.72f, 0.20f, 0.30f), new Color(0.98f, 0.78f, 0.18f), new Color(0.05f, 0.05f, 0.08f), new Color(0.95f, 0.95f, 0.98f), new Color(0.96f, 0.96f, 0.92f), new Color(0.85f, 0.15f, 0.20f) } },
    };

    static void PintarPueblo(GameObject g, string ch)
    {
        Color[] pal; if (!paletaPueblo.TryGetValue(ch, out pal)) return;
        foreach (var r in g.GetComponentsInChildren<Renderer>(true))
        {
            var arr = r.sharedMaterials;
            if (arr.Length != pal.Length) Debug.LogWarning("[Cinematica] " + ch + ": el modelo tiene " + arr.Length + " materiales y se esperaban " + pal.Length + ". Revisar colores.");
            for (int i = 0; i < arr.Length; i++)
            {
                var c = pal[Mathf.Min(i, pal.Length - 1)];
                string pm = Carpeta + "/Mat_Pueblo_" + ch + "_" + i + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(pm);
                if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, pm); }
                m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", 0.3f); m.SetFloat("_Metallic", 0f);
                EditorUtility.SetDirty(m);
                arr[i] = m;
            }
            r.sharedMaterials = arr;
        }
    }

    static GameObject Aldeano(string ch, string nombre, Vector3 local, float altura)
    {
        string fbx = PueblosDir + ch + "/" + ch + ".fbx";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
        if (prefab == null) { Debug.LogWarning("[Cinematica] No encuentro " + fbx); return null; }
        var g = (GameObject)Object.Instantiate(prefab); g.name = nombre + "_Modelo";
        var an = g.GetComponentInChildren<Animator>(); if (an == null) an = g.AddComponent<Animator>();
        RuntimeAnimatorController rc; if (ctrlPueblo.TryGetValue(ch, out rc)) an.runtimeAnimatorController = rc;
        an.applyRootMotion = false;
        PintarPueblo(g, ch);
        var cont = new GameObject(nombre); cont.transform.SetParent(actoresRoot, true); cont.transform.position = O + local;
        g.transform.SetParent(cont.transform, true); g.transform.localRotation = Quaternion.identity; g.transform.position = cont.transform.position;
        var rs = g.GetComponentsInChildren<Renderer>(true);
        if (rs.Length > 0)
        {
            var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
            if (bb.size.y > 0.01f) g.transform.localScale *= altura / bb.size.y;
            bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
            g.transform.position += Vector3.up * (cont.transform.position.y - bb.min.y);   // pies en el piso
        }
        return cont;
    }

    static GameObject Actor(string prefabPath, string nombre, Vector3 local, Material silueta)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) { Debug.LogWarning("[Cinematica] No encuentro " + prefabPath); return null; }
        var g = (GameObject)Object.Instantiate(prefab);
        g.name = nombre + "_Modelo"; g.transform.position = O + local;
        // el prefab tiene el pivote en el centro de la capsula, no en los pies: medimos donde estan los pies
        float pies = 0f;
        var cc = g.GetComponentInChildren<CharacterController>(true);
        if (cc != null && !nombre.Contains("Goro")) pies = cc.transform.TransformPoint(cc.center + Vector3.down * cc.height * 0.5f).y - g.transform.position.y;
        else
        {
            var rr = g.GetComponentsInChildren<Renderer>(true);
            if (rr.Length > 0) { var bb = rr[0].bounds; foreach (var r2 in rr) bb.Encapsulate(r2.bounds); pies = bb.min.y - g.transform.position.y; }
        }
        for (int pass = 0; pass < 4; pass++)
        {
            foreach (var m in g.GetComponentsInChildren<MonoBehaviour>(true)) if (m != null) Object.DestroyImmediate(m);
            foreach (var c in g.GetComponentsInChildren<Collider>(true)) if (c != null) Object.DestroyImmediate(c);
            foreach (var a in g.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true)) if (a != null) Object.DestroyImmediate(a);
            foreach (var r in g.GetComponentsInChildren<Rigidbody>(true)) if (r != null) Object.DestroyImmediate(r);
            foreach (var s in g.GetComponentsInChildren<AudioSource>(true)) if (s != null) Object.DestroyImmediate(s);
        }
        if (silueta != null)
            foreach (var r in g.GetComponentsInChildren<Renderer>(true))
            { var arr = new Material[r.sharedMaterials.Length]; for (int i = 0; i < arr.Length; i++) arr[i] = silueta; r.sharedMaterials = arr; }
        // contenedor con el pivote a nivel del piso; el modelo baja/sube para que sus pies queden en el piso
        var cont = new GameObject(nombre); cont.transform.SetParent(actoresRoot, true); cont.transform.position = O + local;
        g.transform.SetParent(cont.transform, true);
        g.transform.localRotation = Quaternion.identity;   // los prefabs vienen girados 180 grados: asi el modelo mira hacia adelante
        g.transform.position = new Vector3(cont.transform.position.x, cont.transform.position.y - pies, cont.transform.position.z);
        return cont;
    }

    // ---------------- materiales ----------------
    static Material Mat(string n, Color c)
    {
        string p = Carpeta + "/Mat_" + n + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
        m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", 0.1f); EditorUtility.SetDirty(m);
        return m;
    }
    static Material MatUnlit(string n, Color c)
    {
        string p = Carpeta + "/Mat_" + n + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(m, p); }
        m.SetColor("_BaseColor", c); EditorUtility.SetDirty(m);
        return m;
    }
}
