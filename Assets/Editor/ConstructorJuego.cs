using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

// Arma el juego siguiendo el PDF "Creando un videojuego 2D con Unity".
// Paso 1: Unity -batchmode -executeMethod ConstructorJuego.Preparar
// Paso 2: Unity -batchmode -executeMethod ConstructorJuego.Construir
// También se puede lanzar desde el menú "Juego" del editor.
public static class ConstructorJuego
{
    const string Pack = "Assets/Sprites/Legacy-Fantasy - High Forest 2.3";
    const string RutaTiles = Pack + "/Assets/Tiles.png";
    const float Celda = 0.16f;
    const int TamTile = 16;

    // ---------------------------------------------------------------- Paso 1
    // TextMeshPro Essentials se importa con -importPackage desde la línea de comandos
    // (o en el editor: Window > TextMeshPro > Import TMP Essential Resources)
    [MenuItem("Juego/1. Preparar recursos")]
    public static void Preparar()
    {
        // Sprite Editor > Slice > Grid by cell size 16x16, Filter mode Point
        CortarHoja(RutaTiles, TamTile, TamTile, "Tiles", new Vector2(0.5f, 0.5f));
        // El caracol viene como una sola imagen: 8 cuadros de 48x32
        CortarHoja(Pack + "/Mob/Snail/Dead-Sheet.png", 48, 32, "Dead-Sheet", new Vector2(0.5f, 0f));
        AssetDatabase.SaveAssets();
        Debug.Log("[Constructor] Preparar OK");
    }

    static void CortarHoja(string ruta, int ancho, int alto, string prefijo, Vector2 pivote)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.spritePixelsPerUnit = 100;
        imp.SaveAndReimport();

        var tex = new Texture2D(2, 2);
        tex.LoadImage(File.ReadAllBytes(ruta));
        int cols = tex.width / ancho, filas = tex.height / alto;

        var rects = new List<SpriteRect>();
        for (int f = 0; f < filas; f++)
            for (int c = 0; c < cols; c++)
            {
                var r = new RectInt(c * ancho, tex.height - (f + 1) * alto, ancho, alto);
                if (CeldaVacia(tex, r)) continue;
                rects.Add(new SpriteRect
                {
                    name = $"{prefijo}_{f * cols + c}",
                    rect = new Rect(r.x, r.y, r.width, r.height),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivote,
                    spriteID = GUID.Generate()
                });
            }

        var fabricas = new SpriteDataProviderFactories();
        fabricas.Init();
        var dp = fabricas.GetSpriteEditorDataProviderFromObject(imp);
        dp.InitSpriteEditorDataProvider();
        dp.SetSpriteRects(rects.ToArray());
        var nombres = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        nombres?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        dp.Apply();
        imp.SaveAndReimport();
        Debug.Log($"[Constructor] {prefijo} cortado: {rects.Count} sprites");
    }

    static bool CeldaVacia(Texture2D t, RectInt r)
    {
        for (int y = r.y; y < r.yMax; y++)
            for (int x = r.x; x < r.xMax; x++)
                if (t.GetPixel(x, y).a > 0.01f) return false;
        return true;
    }

    // ---------------------------------------------------------------- Paso 2
    [MenuItem("Juego/2. Construir escenas")]
    public static void Construir()
    {
        Carpeta("Assets/Prefab");
        Carpeta("Assets/Animaciones");
        Carpeta("Assets/Tiles");

        // Physics Material 2D "solido" con fricción 0
        var solido = new PhysicsMaterial2D("solido") { friction = 0f, bounciness = 0f };
        Guardar(solido, "Assets/solido.physicsMaterial2D");

        var prefJugador = CrearJugador(solido);
        var prefAbeja = CrearAbeja();
        var prefPuerquito = CrearPuerquito();
        var prefCaracol = CrearCaracol();

        ConstruirEscenaJuego(prefJugador, prefAbeja, prefPuerquito, prefCaracol);
        ConstruirEscenaMenu();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MenuPrincipal.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Juego.unity", true),
        };
        AssetDatabase.SaveAssets();
        Debug.Log("[Constructor] Construir OK");
    }

    // ------------------------------------------------------------ Personaje
    static GameObject CrearJugador(PhysicsMaterial2D solido)
    {
        string dir = "Assets/Animaciones/Jugador";
        var idle = ExportarClip(Pack + "/Character/Idle/Idle.aseprite", dir, "Idle", true);
        var run = ExportarClip(Pack + "/Character/Run/Run.aseprite", dir, "Run", true);
        var jump = ExportarClip(Pack + "/Character/Jump/Jump.aseprite", dir, "Jump", false);
        var jumpEnd = ExportarClip(Pack + "/Character/Jump-End/Jump-End.aseprite", dir, "Jump-End", false);
        var ataque = ExportarClip(Pack + "/Character/Attack-01/Attack-01.aseprite", dir, "Attack-01", false);
        var muerte = ExportarClip(Pack + "/Character/Dead/Dead.aseprite", dir, "Dead", false);

        // PjController con parámetros y transiciones del PDF
        var ctrl = Controlador(dir + "/PjController.controller");
        ctrl.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("VelocidadVertical", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("estaEnPiso", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Atacar", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Muerto", AnimatorControllerParameterType.Trigger);
        var sm = ctrl.layers[0].stateMachine;
        var sIdle = sm.AddState("Idle", new Vector3(300, 0));
        var sRun = sm.AddState("Run", new Vector3(300, 120));
        var sJump = sm.AddState("Jump", new Vector3(600, 0));
        var sJumpEnd = sm.AddState("Jump-End", new Vector3(600, 120));
        sIdle.motion = idle; sRun.motion = run; sJump.motion = jump; sJumpEnd.motion = jumpEnd;
        sm.defaultState = sIdle;
        var sAtaque = sm.AddState("Attack-01", new Vector3(0, 240));
        sAtaque.motion = ataque;
        var anyAtaque = sm.AddAnyStateTransition(sAtaque);
        anyAtaque.hasExitTime = false; anyAtaque.duration = 0; anyAtaque.canTransitionToSelf = false;
        anyAtaque.AddCondition(AnimatorConditionMode.If, 0, "Atacar");
        var finAtaque = sAtaque.AddTransition(sIdle);
        finAtaque.hasExitTime = true; finAtaque.exitTime = 1f; finAtaque.duration = 0;
        // Muerte: desde cualquier estado y sin salida
        var sMuerte = sm.AddState("Dead", new Vector3(300, 240));
        sMuerte.motion = muerte;
        var anyMuerte = sm.AddAnyStateTransition(sMuerte);
        anyMuerte.hasExitTime = false; anyMuerte.duration = 0; anyMuerte.canTransitionToSelf = false;
        anyMuerte.AddCondition(AnimatorConditionMode.If, 0, "Muerto");

        Transicion(sIdle, sRun, ("Velocidad", AnimatorConditionMode.Greater, 0.1f));
        Transicion(sRun, sIdle, ("Velocidad", AnimatorConditionMode.Less, 0.1f));
        var anyJump = sm.AddAnyStateTransition(sJump);
        anyJump.hasExitTime = false; anyJump.duration = 0; anyJump.canTransitionToSelf = false;
        anyJump.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VelocidadVertical");
        anyJump.AddCondition(AnimatorConditionMode.IfNot, 0, "estaEnPiso");
        Transicion(sJump, sJumpEnd, ("VelocidadVertical", AnimatorConditionMode.Less, 0.1f));
        Transicion(sJumpEnd, sRun, ("Velocidad", AnimatorConditionMode.Greater, 0.1f), ("estaEnPiso", AnimatorConditionMode.If, 0));
        Transicion(sJumpEnd, sIdle, ("Velocidad", AnimatorConditionMode.Less, 0.1f), ("estaEnPiso", AnimatorConditionMode.If, 0));
        // Caer de un borde sin saltar también usa la animación de caída
        Transicion(sIdle, sJumpEnd, ("VelocidadVertical", AnimatorConditionMode.Less, -0.5f), ("estaEnPiso", AnimatorConditionMode.IfNot, 0));
        Transicion(sRun, sJumpEnd, ("VelocidadVertical", AnimatorConditionMode.Less, -0.5f), ("estaEnPiso", AnimatorConditionMode.IfNot, 0));

        var go = new GameObject("Idle");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PrimerSprite(idle);
        sr.sortingOrder = 2;
        go.AddComponent<Animator>().runtimeAnimatorController = ctrl;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = 1.5f; // caída más rápida: el salto se siente más ágil

        var cap = go.AddComponent<CapsuleCollider2D>();
        var b = sr.sprite.bounds;
        cap.size = new Vector2(0.24f, b.size.y - 0.02f);
        cap.offset = b.center;
        cap.sharedMaterial = solido;

        var comprobador = new GameObject("ComprobadorDePiso");
        comprobador.transform.SetParent(go.transform, false);
        comprobador.transform.localPosition = new Vector3(b.center.x, b.min.y, 0);

        var punto = new GameObject("PuntoAtaque");
        punto.transform.SetParent(go.transform, false);
        punto.transform.localPosition = new Vector3(b.center.x + 0.22f, b.center.y, 0);

        go.AddComponent<AudioSource>().playOnAwake = false;
        var j = go.AddComponent<Jugador>();
        j.velocidad = 2f;
        j.alturaSalto = 6f;
        j.comprobadorPiso = comprobador.transform;
        j.layerPiso = LayerMask.GetMask("Pisito");
        j.puntoAtaque = punto.transform;
        j.audioSource = go.GetComponent<AudioSource>();
        j.audioAbeja = Audio("Efectos/04_sack_open_1.wav");
        j.audioCaracol = Audio("Efectos/26_sword_hit_1.wav");
        j.audioPuerquito = Audio("Efectos/11_human_damage_1.wav");
        j.audioAtaque = Audio("Efectos/07_human_atk_sword_1.wav");
        j.audioMuerte = Audio("Efectos/14_human_death_spin.wav");

        Debug.Log($"[Constructor] Sprite Idle rect={sr.sprite.rect} pivot={sr.sprite.pivot}");
        return GuardarPrefab(go, "Assets/Prefab/Jugador.prefab");
    }

    // ------------------------------------------------------------- Enemigos
    static GameObject CrearAbeja()
    {
        var clip = ExportarClip(Pack + "/Mob/Small Bee/Fly/Fly.aseprite", "Assets/Animaciones/Abeja", "Fly", true);
        var go = Mob("Abeja", clip, "Assets/Animaciones/Abeja/Abeja.controller", "abejita");
        var cap = go.AddComponent<CapsuleCollider2D>();
        cap.isTrigger = true;
        var b = go.GetComponent<SpriteRenderer>().sprite.bounds;
        cap.size = new Vector2(0.2f, 0.3f);
        cap.offset = b.center;
        go.AddComponent<Flotar>();
        return GuardarPrefab(go, "Assets/Prefab/Abeja.prefab");
    }

    static GameObject CrearPuerquito()
    {
        string dir = "Assets/Animaciones/Puerquito";
        AssetDatabase.DeleteAsset(dir + "/Idle.anim"); // ya no se usa: ahora patrulla
        var caminar = ExportarClip(Pack + "/Mob/Boar/Walk/Walk-Base.aseprite", dir, "Walk", true);
        var correr = ExportarClip(Pack + "/Mob/Boar/Run/Run.aseprite", dir, "Run", true);
        var golpe = ExportarClip(Pack + "/Mob/Boar/Hit-Vanish/Hit.aseprite", dir, "Hit", false);

        var ctrl = Controlador(dir + "/Puerquito.controller");
        ctrl.AddParameter("Corriendo", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Muere", AnimatorControllerParameterType.Trigger);
        var sm = ctrl.layers[0].stateMachine;
        var sCaminar = sm.AddState("Walk");
        var sCorrer = sm.AddState("Run");
        var sGolpe = sm.AddState("Hit");
        sCaminar.motion = caminar; sCorrer.motion = correr; sGolpe.motion = golpe;
        sm.defaultState = sCaminar;
        Transicion(sCaminar, sCorrer, ("Corriendo", AnimatorConditionMode.If, 0));
        Transicion(sCorrer, sCaminar, ("Corriendo", AnimatorConditionMode.IfNot, 0));
        CualquierA(sm, sGolpe, "Muere");

        var go = Enemigo("Puerquito", "puerquito", caminar, ctrl, 0.35f, 0.9f, true);
        var cap = go.AddComponent<CapsuleCollider2D>();
        cap.isTrigger = true;
        cap.direction = CapsuleDirection2D.Horizontal;
        var b = go.GetComponent<SpriteRenderer>().sprite.bounds;
        cap.size = new Vector2(b.size.x - 0.08f, b.size.y - 0.06f);
        cap.offset = b.center;
        go.GetComponent<Enemigo>().mitadAncho = b.extents.x;
        return GuardarPrefab(go, "Assets/Prefab/Puerquito.prefab");
    }

    static GameObject CrearCaracol()
    {
        string dir = "Assets/Animaciones/Caracol";
        // Las hojas del caracol vienen como una sola imagen: 8 cuadros de 48x32
        string rutaCaminar = Pack + "/Mob/Snail/walk-Sheet.png";
        if (((TextureImporter)AssetImporter.GetAtPath(rutaCaminar)).spriteImportMode != SpriteImportMode.Multiple)
            CortarHoja(rutaCaminar, 48, 32, "walk-Sheet", new Vector2(0.5f, 0f));
        Sprite[] Cuadros(string ruta) => AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Split('_')[1])).ToArray();
        var caminar = ClipDeSprites(Cuadros(rutaCaminar), dir, "Walk", 8, true);
        var muerte = ClipDeSprites(Cuadros(Pack + "/Mob/Snail/Dead-Sheet.png"), dir, "Dead", 12, false);

        var ctrl = Controlador(dir + "/Caracol.controller");
        ctrl.AddParameter("Muere", AnimatorControllerParameterType.Trigger);
        var sm = ctrl.layers[0].stateMachine;
        var sCaminar = sm.AddState("Walk");
        var sMuerte = sm.AddState("Dead");
        sCaminar.motion = caminar; sMuerte.motion = muerte;
        sm.defaultState = sCaminar;
        CualquierA(sm, sMuerte, "Muere");

        var go = Enemigo("Caracol", "caracol", caminar, ctrl, 0.18f, 0.6f, false);
        go.GetComponent<Enemigo>().mitadAncho = 0.14f;

        var box = go.AddComponent<BoxCollider2D>();
        // Caparazón sólido abajo, trigger de pisotón encima (cabeza libre)
        box.size = new Vector2(0.27f, 0.15f);
        box.offset = new Vector2(0, 0.075f);

        var cap = go.AddComponent<CapsuleCollider2D>();
        cap.isTrigger = true;
        cap.direction = CapsuleDirection2D.Horizontal;
        cap.size = new Vector2(0.27f, 0.08f);
        cap.offset = new Vector2(0, 0.19f);
        return GuardarPrefab(go, "Assets/Prefab/Caracol.prefab");
    }

    // Enemigo con cuerpo cinemático que se mueve con el script Enemigo
    static GameObject Enemigo(string nombre, string tag, AnimationClip clip, AnimatorController ctrl,
        float velocidad, float rango, bool persigue)
    {
        var go = new GameObject(nombre) { tag = tag };
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PrimerSprite(clip);
        sr.sortingOrder = 2;
        go.AddComponent<Animator>().runtimeAnimatorController = ctrl;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var e = go.AddComponent<Enemigo>();
        e.velocidad = velocidad;
        e.rangoPatrulla = rango;
        e.persigue = persigue;
        e.layerPiso = LayerMask.GetMask("Pisito");
        return go;
    }

    static void CualquierA(AnimatorStateMachine sm, AnimatorState destino, string trigger)
    {
        var t = sm.AddAnyStateTransition(destino);
        t.hasExitTime = false; t.duration = 0; t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.If, 0, trigger);
    }

    static GameObject Mob(string nombre, AnimationClip clip, string rutaCtrl, string tag)
    {
        var ctrl = Controlador(rutaCtrl);
        ctrl.layers[0].stateMachine.AddState(clip.name).motion = clip;
        var go = new GameObject(nombre) { tag = tag };
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PrimerSprite(clip);
        sr.sortingOrder = 2;
        go.AddComponent<Animator>().runtimeAnimatorController = ctrl;
        return go;
    }

    // ----------------------------------------------------------- Escena juego
    static void ConstruirEscenaJuego(GameObject jugador, GameObject abeja, GameObject puerquito, GameObject caracol)
    {
        const string ruta = "Assets/Scenes/Juego.unity";
        var escena = EscenaDesdePlantilla(ruta);

        // Cámara
        var cam = Camera.main;
        cam.orthographicSize = 1.5f;
        cam.backgroundColor = new Color(0.12f, 0.16f, 0.12f);

        // Fondo: hijo de la cámara para que siempre cubra la pantalla
        var fondoSprite = AssetDatabase.LoadAllAssetsAtPath(Pack + "/Background/Background.aseprite").OfType<Sprite>().First();
        var fondo = new GameObject("Background");
        var fsr = fondo.AddComponent<SpriteRenderer>();
        fsr.sprite = fondoSprite;
        fsr.sortingOrder = -10;
        fondo.transform.SetParent(cam.transform, false);
        float escala = 3.2f / fondoSprite.bounds.size.y;
        fondo.transform.localPosition = new Vector3(-fondoSprite.bounds.center.x * escala, -fondoSprite.bounds.center.y * escala, 10);
        fondo.transform.localScale = new Vector3(escala, escala, 1);
        fondo.AddComponent<FondoCamara>(); // lo estira para cubrir toda la cámara

        // Música de fondo en loop
        var musica = new GameObject("Musica").AddComponent<AudioSource>();
        musica.clip = Audio("Musica/Goblins_Den_(Regular).wav");
        musica.loop = true;
        musica.playOnAwake = true;
        musica.volume = 0.35f;

        // ---- Diseño del nivel (coordenadas en casillas de 16 px) ----
        // Suelos: (desde, hasta, fila de la superficie). Todos bajan hasta la fila -8.
        var suelos = new (int x0, int x1, int y)[]
        {
            (-14, 30, -3), (35, 49, -3), (50, 62, -1), (67, 95, -3), (96, 110, -2),
            (116, 140, -3), (157, 170, -3), (171, 182, -1), (183, 205, -3),
        };
        // Plataformas flotantes de un bloque de alto
        var flotantes = new (int x0, int x1, int y)[]
        {
            (10, 15, 3), (42, 47, 3), (53, 58, 5), (80, 86, 3), (100, 104, 4),
            (125, 130, 3), (143, 146, -1), (150, 153, 0), (175, 179, 5),
        };
        int xMinNivel = -14, xMaxNivel = 205;
        float Sup(int fila) => (fila + 1) * Celda; // altura del borde superior de una fila

        // Paisaje: montañas, bosques y pinos con parallax
        ConstruirPaisaje(xMinNivel * Celda, xMaxNivel * Celda, Sup(-3));

        // Grid + Tilemaps
        var grid = new GameObject("Grid").AddComponent<Grid>();
        grid.cellSize = new Vector3(Celda, Celda, 0);
        var piso = NuevoTilemap(grid, "Piso", 0);
        piso.gameObject.layer = LayerMask.NameToLayer("Pisito");
        piso.gameObject.AddComponent<TilemapCollider2D>();
        var deco = NuevoTilemap(grid, "Decoracion", 1);

        // Bloque de pasto (5x5 arriba a la izquierda del spritesheet de Tiles)
        TileBase T(int fila, int col) => Tile(fila, col);
        void Plataforma(int x0, int x1, int ySup, int prof)
        {
            for (int x = x0; x <= x1; x++)
            {
                int col = x == x0 ? 0 : x == x1 ? 4 : 1 + (x - x0 - 1) % 3;
                deco.SetTile(new Vector3Int(x, ySup + 1, 0), T(0, col));
                piso.SetTile(new Vector3Int(x, ySup, 0), T(1, col));
                for (int y = ySup - 1; y > ySup - prof; y--)
                    piso.SetTile(new Vector3Int(x, y, 0), T(2 + (ySup - 1 - y) % 2, col));
                piso.SetTile(new Vector3Int(x, ySup - prof, 0), T(4, col));
            }
        }
        foreach (var (x0, x1, y) in suelos) Plataforma(x0, x1, y, y + 8);
        foreach (var (x0, x1, y) in flotantes) Plataforma(x0, x1, y, 1);

        // Paredes invisibles a los lados
        Pared(piso.gameObject, new Vector2((xMinNivel - 0.5f) * Celda, 1f));
        Pared(piso.gameObject, new Vector2((xMaxNivel + 1.5f) * Celda, 1f));

        // Decoración sobre los suelos (detrás del personaje)
        ConstruirDecoracion(suelos, flotantes, Celda);

        // Personaje
        var pj = (GameObject)PrefabUtility.InstantiatePrefab(jugador);
        pj.transform.position = new Vector3(-8 * Celda, Sup(-3) + 0.1f, 0);

        cam.gameObject.AddComponent<Camara>().target = pj.transform;
        cam.transform.position = new Vector3(pj.transform.position.x, pj.transform.position.y, -10);

        // Abejas para recolectar: (x, fila de la superficie debajo, altura extra en casillas)
        var abejas = new GameObject("Abejas");
        var posAbejas = new (float x, int y, float extra)[]
        {
            (-2, -3, 0), (4, -3, 0), (12.5f, 3, 0), (22, -3, 0), (38, -3, 0), (44.5f, 3, 0),
            (55.5f, 5, 0), (60, -1, 0), (72, -3, 0), (83, 3, 0), (92, -3, 0), (102, 4, 0),
            (108, -2, 0), (113, -3, 5), (120, -3, 0), (127.5f, 3, 0), (144.5f, -1, 0), (151.5f, 0, 0),
            (162, -3, 0), (168, -3, 0), (177, 5, 0), (186, -3, 0), (191, -3, 0),
        };
        foreach (var (x, y, extra) in posAbejas)
            Colocar(abeja, abejas.transform, x * Celda, Sup(y) + extra * Celda);

        // Caracoles para pisar y puerquitos peligrosos
        var mobs = new GameObject("Mobs");
        foreach (var (x, y) in new (float, int)[] { (18, -3), (41, -3), (75, -3), (99, -2), (132, -3), (165, -3), (186, -3) })
            Colocar(caracol, mobs.transform, x * Celda, Sup(y));
        foreach (var (x, y) in new (float, int)[] { (27, -3), (59, -1), (90, -3), (122, -3), (180, -1), (195, -3) })
            Colocar(puerquito, mobs.transform, x * Celda, Sup(y));

        // Zona de caída al vacío (tag puerquito)
        var vacio = new GameObject("Vacio") { tag = "puerquito" };
        vacio.transform.position = new Vector3((xMinNivel + xMaxNivel) / 2f * Celda, -2.5f, 0);
        var bv = vacio.AddComponent<BoxCollider2D>();
        bv.isTrigger = true;
        bv.size = new Vector2((xMaxNivel - xMinNivel + 40) * Celda, 0.5f);

        // Meta: cofre al final del nivel
        var cofre = new GameObject("Cofre");
        var csr = cofre.AddComponent<SpriteRenderer>();
        csr.sprite = SpriteDecoracion("Cofre_Cerrado");
        csr.sortingOrder = 1;
        cofre.transform.position = new Vector3(200.5f * Celda, Sup(-3), 0);
        var cbox = cofre.AddComponent<BoxCollider2D>();
        cbox.isTrigger = true;
        cbox.size = new Vector2(0.3f, 0.3f);
        cbox.offset = new Vector2(0, 0.15f);
        var meta = cofre.AddComponent<Meta>();
        meta.cofreAbierto = SpriteDecoracion("Cofre_Abierto");
        meta.audioAbrir = Audio("Efectos/01_chest_open_1.wav");

        // UI: contador de abejas
        var canvas = NuevoCanvas();
        var marcador = new GameObject("Marcador", typeof(RectTransform), typeof(Image));
        marcador.transform.SetParent(canvas.transform, false);
        var img = marcador.GetComponent<Image>();
        img.sprite = PrimerSprite(AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animaciones/Abeja/Fly.anim"));
        img.preserveAspect = true;
        Esquina(marcador.GetComponent<RectTransform>(), new Vector2(24, -110), new Vector2(100, 100));

        var txt = new GameObject("TxtAbejas", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        txt.transform.SetParent(canvas.transform, false);
        txt.text = "0";
        txt.fontSize = 64;
        txt.alignment = TextAlignmentOptions.MidlineLeft;
        Esquina(txt.rectTransform, new Vector2(135, -110), new Vector2(300, 100));
        var jug = pj.GetComponent<Jugador>();
        jug.textoAbejas = txt;

        // UI: corazones de vida
        jug.corazonLleno = SpriteSuelto("Assets/Sprites/HUD/Corazon_Lleno.png", false);
        jug.corazonVacio = SpriteSuelto("Assets/Sprites/HUD/Corazon_Vacio.png", false);
        jug.corazones = new Image[jug.vidasMax];
        for (int i = 0; i < jug.vidasMax; i++)
        {
            var c = new GameObject($"Corazon_{i + 1}", typeof(RectTransform), typeof(Image));
            c.transform.SetParent(canvas.transform, false);
            var ci = c.GetComponent<Image>();
            ci.sprite = jug.corazonLleno;
            ci.preserveAspect = true;
            Esquina(c.GetComponent<RectTransform>(), new Vector2(30 + i * 90, -24), new Vector2(78, 72));
            jug.corazones[i] = ci;
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(jug);

        // Partida: controla las pantallas de fin de juego
        var partida = new GameObject("Partida").AddComponent<Partida>();
        partida.musica = musica;
        var recursos = RecursosUI();

        // Pantalla de Game Over (oculta hasta que el jugador muere)
        var (gameOver, subGameOver) = PanelPantalla(canvas.transform, "PanelGameOver", "GAME OVER", new Color(0.85f, 0.2f, 0.2f));
        var reintentar = Boton(gameOver.transform, recursos, "REINTENTAR", new Vector2(0, -90), "BLANK_BOTTOM");
        var menuGO = Boton(gameOver.transform, recursos, "MENU", new Vector2(0, -230), "BLANK_BOTTOM");
        UnityEventTools.AddPersistentListener(reintentar.onClick, partida.Reintentar);
        UnityEventTools.AddPersistentListener(menuGO.onClick, partida.IrAlMenu);
        partida.panelGameOver = gameOver;
        partida.textoGameOver = subGameOver;
        gameOver.SetActive(false);

        // Pantalla de victoria
        var (victoria, subVictoria) = PanelPantalla(canvas.transform, "PanelVictoria", "¡NIVEL COMPLETADO!", new Color(1f, 0.82f, 0.29f));
        subVictoria.rectTransform.anchoredPosition = new Vector2(0, 40);
        subVictoria.rectTransform.sizeDelta = new Vector2(1400, 260);
        var otraVez = Boton(victoria.transform, recursos, "JUGAR DE NUEVO", new Vector2(0, -170), "BLANK_BOTTOM");
        var menuV = Boton(victoria.transform, recursos, "MENU", new Vector2(0, -310), "BLANK_BOTTOM");
        UnityEventTools.AddPersistentListener(otraVez.onClick, partida.Reintentar);
        UnityEventTools.AddPersistentListener(menuV.onClick, partida.IrAlMenu);
        partida.panelVictoria = victoria;
        partida.textoVictoria = subVictoria;
        victoria.SetActive(false);

        // Cronómetro arriba a la derecha
        var tiempo = Texto(canvas.transform, "TxtTiempo", "00:00", 64, Vector2.zero, new Vector2(300, 100));
        tiempo.rectTransform.anchorMin = tiempo.rectTransform.anchorMax = tiempo.rectTransform.pivot = new Vector2(1, 1);
        tiempo.rectTransform.anchoredPosition = new Vector2(-30, -20);
        tiempo.alignment = TextAlignmentOptions.MidlineRight;
        partida.textoTiempo = tiempo;

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
    }

    // ------------------------------------------------------------ Menú
    static void ConstruirEscenaMenu()
    {
        const string ruta = "Assets/Scenes/MenuPrincipal.unity";
        var escena = EscenaDesdePlantilla(ruta);
        var cam = Camera.main;
        var opciones = cam.gameObject.AddComponent<OpcionesMenu>();

        var canvas = NuevoCanvas();
        var recursos = RecursosUI();

        // Fondo
        var fondo = new GameObject("Fondo", typeof(RectTransform), typeof(Image));
        fondo.transform.SetParent(canvas.transform, false);
        fondo.GetComponent<Image>().sprite = AssetDatabase.LoadAllAssetsAtPath(Pack + "/Background/Background.aseprite").OfType<Sprite>().First();
        Estirar(fondo.GetComponent<RectTransform>());

        var titulo = Texto(canvas.transform, "Titulo", "High Forest", 110, new Vector2(0, 300), new Vector2(1400, 160));
        titulo.fontStyle = FontStyles.Bold;
        Texto(canvas.transform, "Autor", "Jhoel Condoreno", 48, new Vector2(0, 190), new Vector2(1000, 80));

        var jugar = Boton(canvas.transform, recursos, "Jugar", new Vector2(0, 30), "PLAY_BOTTOM_PIXEL");
        var btnOpciones = Boton(canvas.transform, recursos, "Opciones", new Vector2(0, -110), "OPTIONS_BOTTOM_PIXEL");
        var salir = Boton(canvas.transform, recursos, "Salir", new Vector2(0, -250), "EXIT_BOTTOM_PIXEL");
        UnityEventTools.AddPersistentListener(jugar.onClick, opciones.vamosAjugar);
        UnityEventTools.AddPersistentListener(salir.onClick, opciones.salir);

        // Panel de opciones (desactivado; el botón Opciones lo activa)
        var panelito = new GameObject("Panelito", typeof(RectTransform));
        panelito.transform.SetParent(canvas.transform, false);
        Estirar(panelito.GetComponent<RectTransform>());
        var panel = new GameObject("Panel de opciones", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(panelito.transform, false);
        var pimg = panel.GetComponent<Image>();
        pimg.sprite = null;
        
        pimg.color = new Color(0.16f, 0.2f, 0.12f, 1f);
        var prt = panel.GetComponent<RectTransform>();
        prt.sizeDelta = new Vector2(900, 600);
        Texto(panel.transform, "TxtOpciones", "Opciones", 72, new Vector2(0, 200), new Vector2(800, 100));
        Texto(panel.transform, "TxtControles",
            "Moverse: A / D o flechas\nSaltar: Espacio\nAtacar: clic izquierdo o Ctrl\nRecolecta abejas, pisa caracoles\ny evita a los puerquitos",
            40, new Vector2(0, 20), new Vector2(800, 280));
        var cerrar = Boton(panel.transform, recursos, "Cerrar", new Vector2(0, -200), "BLANK_BOTTOM");
        UnityEventTools.AddBoolPersistentListener(btnOpciones.onClick, panelito.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(cerrar.onClick, panelito.SetActive, false);
        panelito.SetActive(false);

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
    }

    // ------------------------------------------------------------ Pantallas
    static TMP_DefaultControls.Resources RecursosUI() => new TMP_DefaultControls.Resources
    {
        standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
        background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
    };

    // Panel a pantalla completa con fondo oscuro, un título y un subtítulo
    static (GameObject panel, TextMeshProUGUI subtitulo) PanelPantalla(Transform canvas, string nombre, string titulo, Color colorTitulo)
    {
        var panel = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas, false);
        Estirar(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.06f, 0.82f);
        var t = Texto(panel.transform, "Titulo", titulo, 130, new Vector2(0, 230), new Vector2(1600, 180));
        t.fontStyle = FontStyles.Bold;
        t.color = colorTitulo;
        var sub = Texto(panel.transform, "Subtitulo", "", 50, new Vector2(0, 90), new Vector2(1400, 120));
        return (panel, sub);
    }

    // ------------------------------------------------------------ Utilidades
    // Copia la animación generada por el importador de Aseprite a Animaciones/<carpeta>
    static AnimationClip ExportarClip(string rutaAse, string dir, string nombre, bool loop)
    {
        Carpeta(dir);
        var original = AssetDatabase.LoadAllAssetsAtPath(rutaAse).OfType<AnimationClip>().First();
        var clip = ClipEnRuta($"{dir}/{nombre}.anim", original.frameRate);
        foreach (var b in AnimationUtility.GetObjectReferenceCurveBindings(original))
        {
            if (b.type != typeof(SpriteRenderer)) continue;
            var curva = AnimationUtility.GetObjectReferenceCurve(original, b);
            AnimationUtility.SetObjectReferenceCurve(clip,
                EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), curva);
            break;
        }
        var ajustes = AnimationUtility.GetAnimationClipSettings(clip);
        ajustes.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, ajustes);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    static Sprite PrimerSprite(AnimationClip clip) =>
        (Sprite)AnimationUtility.GetObjectReferenceCurve(clip, AnimationUtility.GetObjectReferenceCurveBindings(clip)[0])[0].value;

    static AudioClip Audio(string ruta) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + ruta);

    // ------------------------------------------------------- Decoración
    static void ConstruirDecoracion((int x0, int x1, int y)[] suelos, (int x0, int x1, int y)[] flotantes, float celda)
    {
        var raiz = new GameObject("Decoracion").transform;
        var azar = new System.Random(11);
        // (nombre, orden de dibujo, peso)
        var piezas = new (string n, int orden, int peso)[]
        {
            ("Arbusto_1", -2, 3), ("Arbusto_2", -2, 3), ("Arbusto_3", -2, 2),
            ("Hongo_Grande", -1, 2), ("Hongo_Alto", -1, 1), ("Hongo_Chico_1", -1, 2), ("Hongo_Chico_2", -1, 2), ("Hongo_Chico_3", -1, 2),
            ("Flor_Azul", -1, 2), ("Flor_Lila", -1, 2), ("Junco", -1, 1), ("Pasto", 3, 4), ("Palo", -1, 1),
            ("Roca_Musgo_Grande", -3, 1), ("Roca_Musgo_1", -2, 2), ("Roca_Musgo_2", -1, 2), ("Roca_Musgo_3", -1, 1),
            ("Roca_Musgo_4", -1, 1), ("Roca_Chica", -1, 2),
        };
        int total = piezas.Sum(p => p.peso);
        int k = 0;
        void Poner(int x0, int x1, int fila, int pasoMin, int pasoMax)
        {
            for (int x = x0 + 1 + azar.Next(3); x < x1 - 1; x += pasoMin + azar.Next(pasoMax - pasoMin + 1))
            {
                int r = azar.Next(total);
                var pieza = piezas.First(p => (r -= p.peso) < 0);
                var sr = new GameObject($"{pieza.n}_{k++}").AddComponent<SpriteRenderer>();
                sr.sprite = SpriteDecoracion(pieza.n);
                sr.sortingOrder = pieza.orden;
                sr.flipX = azar.Next(2) == 0;
                sr.transform.SetParent(raiz, false);
                sr.transform.position = new Vector3((x + 0.5f) * celda, (fila + 1) * celda - 0.02f, 0);
            }
        }
        foreach (var (x0, x1, y) in suelos) Poner(x0, x1, y, 2, 5);
        foreach (var (x0, x1, y) in flotantes) Poner(x0, x1, y, 3, 5);
    }

    static Sprite SpriteDecoracion(string nombre) => SpriteSuelto("Assets/Sprites/Decoracion/" + nombre + ".png", false);

    // ---------------------------------------------------------- Paisaje
    const string RutaPaisaje = "Assets/Sprites/Paisaje/";

    static void ConstruirPaisaje(float xMin, float xMax, float sueloY)
    {
        var raiz = new GameObject("Paisaje").transform;
        float ancho = (xMax - xMin) + 30f; // margen para el desplazamiento del parallax
        float centro = (xMin + xMax) / 2f;

        // Capas que se repiten a lo ancho (de la más lejana a la más cercana)
        CapaRepetida(raiz, "Montanas_Claras", centro, sueloY - 0.9f, ancho, -9, 0.85f, 0.9f, 1.0f, new Color(0.85f, 0.95f, 0.95f));
        CapaRepetida(raiz, "Montanas_Oscuras", centro, sueloY - 1.2f, ancho, -8, 0.75f, 0.8f, 0.9f, new Color(0.8f, 0.92f, 0.92f));
        CapaRepetida(raiz, "Bosque_Claro", centro, sueloY - 1.4f, ancho, -7, 0.6f, 0.65f, 1.0f, Color.white);
        CapaRepetida(raiz, "Bosque_Oscuro", centro, sueloY - 1.5f, ancho, -6, 0.4f, 0.45f, 1.0f, Color.white);

        // Pinos sueltos justo detrás del piso
        var pinos = new GameObject("Pinos").transform;
        pinos.SetParent(raiz, false);
        string[] colores = { "Green", "Dark", "Green", "Golden", "Green", "Dark", "Yellow", "Green", "Red" };
        string[] tamanos = { "Grande", "Mediano", "Chico" };
        var azar = new System.Random(7);
        int i = 0;
        for (float x = xMin - 1.5f; x < xMax + 1.5f; x += 0.7f + (float)azar.NextDouble() * 0.9f, i++)
        {
            string tam = tamanos[azar.Next(tamanos.Length)];
            var sr = new GameObject($"Pino_{i}").AddComponent<SpriteRenderer>();
            sr.sprite = SpritePaisaje($"Pino_{tam}_{colores[azar.Next(colores.Length)]}", false);
            sr.sortingOrder = -5;
            sr.color = new Color(0.78f, 0.85f, 0.8f); // un poco más apagados que el primer plano
            sr.transform.SetParent(pinos, false);
            sr.transform.position = new Vector3(x, sueloY - 0.1f, 0);
            sr.flipX = azar.Next(2) == 0;
        }
        var px = pinos.gameObject.AddComponent<Parallax>();
        px.factorX = 0.15f; px.factorY = 0.15f;
    }

    static void CapaRepetida(Transform raiz, string nombre, float x, float y, float ancho, int orden,
        float factorX, float factorY, float escala, Color color)
    {
        var sr = new GameObject(nombre).AddComponent<SpriteRenderer>();
        sr.sprite = SpritePaisaje(nombre, true);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(ancho / escala, sr.sprite.bounds.size.y);
        sr.sortingOrder = orden;
        sr.color = color;
        sr.transform.SetParent(raiz, false);
        sr.transform.position = new Vector3(x, y, 0);
        sr.transform.localScale = new Vector3(escala, escala, 1);
        var p = sr.gameObject.AddComponent<Parallax>();
        p.factorX = factorX; p.factorY = factorY;
    }

    // Importa un PNG del paisaje como sprite pixel art con pivote abajo al centro
    static Sprite SpritePaisaje(string nombre, bool repetible) => SpriteSuelto(RutaPaisaje + nombre + ".png", repetible);

    static Sprite SpriteSuelto(string ruta, bool repetible)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
        var ajustes = new TextureImporterSettings();
        imp.ReadTextureSettings(ajustes);
        bool cambiar = imp.textureType != TextureImporterType.Sprite || imp.filterMode != FilterMode.Point
            || ajustes.spriteAlignment != (int)SpriteAlignment.BottomCenter
            || (repetible && ajustes.spriteMeshType != SpriteMeshType.FullRect);
        if (cambiar)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.wrapMode = repetible ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            imp.ReadTextureSettings(ajustes);
            ajustes.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            ajustes.spriteMeshType = repetible ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
            imp.SetTextureSettings(ajustes);
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    static Sprite SpriteBoton(string nombre)
    {
        string ruta = Directory.GetFiles("Assets/Sprites/Botones", nombre + ".png", SearchOption.AllDirectories)[0].Replace('\\', '/');
        var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
        if (imp.textureType != TextureImporterType.Sprite || imp.filterMode != FilterMode.Point)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    static AnimationClip ClipDeSprites(Sprite[] cuadros, string dir, string nombre, float fps, bool loop)
    {
        Carpeta(dir);
        var clip = ClipEnRuta($"{dir}/{nombre}.anim", fps);
        var claves = cuadros.Select((s, i) => new ObjectReferenceKeyframe { time = i / fps, value = s }).ToArray();
        AnimationUtility.SetObjectReferenceCurve(clip,
            EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), claves);
        var ajustes = AnimationUtility.GetAnimationClipSettings(clip);
        ajustes.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, ajustes);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    static readonly Dictionary<string, TileBase> cacheTiles = new();
    static TileBase Tile(int fila, int col)
    {
        string nombre = $"Tiles_{fila * 25 + col}";
        if (cacheTiles.TryGetValue(nombre, out var t)) return t;
        string ruta = $"Assets/Tiles/{nombre}.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(ruta);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = AssetDatabase.LoadAllAssetsAtPath(RutaTiles).OfType<Sprite>().First(s => s.name == nombre);
            AssetDatabase.CreateAsset(tile, ruta);
        }
        return cacheTiles[nombre] = tile;
    }

    static Tilemap NuevoTilemap(Grid grid, string nombre, int orden)
    {
        var go = new GameObject(nombre, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(grid.transform, false);
        go.GetComponent<TilemapRenderer>().sortingOrder = orden;
        return go.GetComponent<Tilemap>();
    }

    static void Pared(GameObject piso, Vector2 centro)
    {
        var box = piso.AddComponent<BoxCollider2D>();
        box.offset = centro;
        box.size = new Vector2(Celda, 6f);
    }

    static void Colocar(GameObject prefab, Transform padre, float x, float y)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
        go.transform.position = new Vector3(x, y, 0);
    }

    static Canvas NuevoCanvas()
    {
        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.layer = LayerMask.NameToLayer("UI");
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        return canvas;
    }

    static void Esquina(RectTransform rt, Vector2 pos, Vector2 tam)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
    }

    static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static TextMeshProUGUI Texto(Transform padre, string nombre, string texto, float tam, Vector2 pos, Vector2 caja)
    {
        var t = new GameObject(nombre, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        t.transform.SetParent(padre, false);
        t.text = texto;
        t.fontSize = tam;
        t.alignment = TextAlignmentOptions.Center;
        t.rectTransform.anchoredPosition = pos;
        t.rectTransform.sizeDelta = caja;
        return t;
    }

    // Imagen del pack de botones: _1 normal, _2 al pasar el mouse o presionar.
    // Si la imagen ya trae el texto (PLAY, OPTIONS, EXIT) se oculta el texto del botón.
    static Button Boton(Transform padre, TMP_DefaultControls.Resources recursos, string texto, Vector2 pos, string imagen)
    {
        var go = TMP_DefaultControls.CreateButton(recursos);
        go.name = "Boton" + texto;
        go.transform.SetParent(padre, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(512, 128);
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = 52;
        tmp.color = Color.white;
        var boton = go.GetComponent<Button>();
        var img = go.GetComponent<Image>();
        img.sprite = SpriteBoton(imagen + "_1");
        img.type = Image.Type.Simple;
        img.preserveAspect = true;
        var alterno = SpriteBoton(imagen + "_2");
        boton.transition = Selectable.Transition.SpriteSwap;
        boton.spriteState = new SpriteState { highlightedSprite = alterno, pressedSprite = alterno, selectedSprite = img.sprite };
        bool conTexto = imagen.StartsWith("BLANK");
        tmp.gameObject.SetActive(conTexto);
        // En el botón en blanco el texto es blanco: se usa tinte en vez de cambiar a la imagen blanca
        if (conTexto) boton.transition = Selectable.Transition.ColorTint;
        return boton;
    }

    static void Transicion(AnimatorState de, AnimatorState a, params (string p, AnimatorConditionMode m, float v)[] conds)
    {
        var t = de.AddTransition(a);
        t.hasExitTime = false;
        t.duration = 0;
        foreach (var c in conds) t.AddCondition(c.m, c.v, c.p);
    }

    static GameObject GuardarPrefab(GameObject go, string ruta)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, ruta);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // Abre una copia de SampleScene y la guarda encima de la escena destino
    static UnityEngine.SceneManagement.Scene EscenaDesdePlantilla(string ruta)
    {
        var plantilla = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EditorSceneManager.SaveScene(plantilla, ruta, true);
        return EditorSceneManager.OpenScene(ruta);
    }

    // Reutiliza el .anim si ya existe para no cambiar su GUID
    static AnimationClip ClipEnRuta(string ruta, float fps)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ruta);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, ruta);
        }
        clip.ClearCurves();
        foreach (var b in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            AnimationUtility.SetObjectReferenceCurve(clip, b, null);
        clip.frameRate = fps;
        return clip;
    }

    // Reutiliza el .controller si ya existe, vaciando parámetros y estados
    static AnimatorController Controlador(string ruta)
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ruta);
        if (ctrl == null) return AnimatorController.CreateAnimatorControllerAtPath(ruta);
        for (int i = ctrl.parameters.Length - 1; i >= 0; i--) ctrl.RemoveParameter(i);
        var sm = ctrl.layers[0].stateMachine;
        foreach (var t in sm.anyStateTransitions) sm.RemoveAnyStateTransition(t);
        foreach (var e in sm.states) sm.RemoveState(e.state);
        return ctrl;
    }

    static void Guardar(Object obj, string ruta)
    {
        var existente = AssetDatabase.LoadMainAssetAtPath(ruta);
        if (existente != null && existente.GetType() == obj.GetType())
        {
            EditorUtility.CopySerialized(obj, existente);
            return;
        }
        AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(obj, ruta);
    }

    static void Carpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        var padre = Path.GetDirectoryName(ruta).Replace('\\', '/');
        Carpeta(padre);
        AssetDatabase.CreateFolder(padre, Path.GetFileName(ruta));
    }
}
