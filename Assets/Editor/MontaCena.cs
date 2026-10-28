using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Monta o projeto do zero: importacao dos sprites, tiles, prefabs, cenas e build.
// Menu: Mansao > Montar tudo. ATENCAO: rodar de novo sobrescreve as cenas.
public static class MontaCena
{
    const string SPR = "Assets/Sprites/";
    const string AUD = "Assets/Audio/";
    const string CENA_MENU = "Assets/Scenes/MainMenu.unity";
    const string CENA_SALA = "Assets/Scenes/Sala01_Saguao.unity";
    const string CENA_FIM = "Assets/Scenes/FimDemo.unity";
    const int PPU = 16;

    static Font fonte;
    static Material matLit;
    static Material matUnlit;

    static readonly Color CREME = new Color32(242, 230, 200, 255);
    static readonly Color OURO = new Color32(232, 190, 80, 255);
    static readonly Color TINTA = new Color32(46, 30, 22, 255);

    [MenuItem("Mansao/Montar tudo")]
    public static void Run()
    {
        try
        {
            ConfigurarImportacao();
            ConfigurarProjeto();
            matLit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
            matUnlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            fonte = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fontes/PixelDHS.ttf");
            if (matLit == null || matUnlit == null || fonte == null) throw new Exception("material ou fonte nao encontrados");

            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Prefabs");
            Directory.CreateDirectory("Assets/Tiles");
            Directory.CreateDirectory("Assets/Dados");

            DocumentoDados doc = DocOkafor();
            DocumentoDados bilhete = DocMordomo();
            MontarMenu();
            MontarSala(doc, bilhete);
            MontarFim();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(CENA_MENU, true),
                new EditorBuildSettingsScene(CENA_SALA, true),
                new EditorBuildSettingsScene(CENA_FIM, true),
            };
            AssetDatabase.SaveAssets();
            Debug.Log("MONTA_OK");
        }
        catch (Exception e)
        {
            Debug.LogError("MONTA_ERRO " + e);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw;
        }
    }

    // usado na linha de comando: monta as cenas e ja gera o build
    public static void MontarEBuild()
    {
        Run();
        BuildWebGL();
    }

    [MenuItem("Mansao/Build WebGL")]
    public static void BuildWebGL()
    {
        string saida = Environment.GetEnvironmentVariable("WEBGL_OUT");
        if (string.IsNullOrEmpty(saida)) saida = "Build/WebGL";
        var opcoes = new BuildPlayerOptions
        {
            scenes = new[] { CENA_MENU, CENA_SALA, CENA_FIM },
            locationPathName = saida,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };
        var relatorio = BuildPipeline.BuildPlayer(opcoes);
        Debug.Log("BUILD_RESULT " + relatorio.summary.result + " " + relatorio.summary.totalSize);
        if (Application.isBatchMode) EditorApplication.Exit(relatorio.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }

    // ================================================================ projeto e importacao

    static void ConfigurarProjeto()
    {
        PlayerSettings.productName = "Mansão Varkoff";
        PlayerSettings.companyName = "Insper";
        PlayerSettings.defaultWebScreenWidth = 960;
        PlayerSettings.defaultWebScreenHeight = 540;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        Physics2D.queriesHitTriggers = true;
        Physics2D.queriesStartInColliders = false;
        Physics2D.gravity = Vector2.zero;
    }

    static void ConfigurarImportacao()
    {
        foreach (string caminho in Directory.GetFiles("Assets/Sprites", "*.png"))
        {
            string arq = caminho.Replace('\\', '/');
            string nome = Path.GetFileNameWithoutExtension(arq);
            var ti = (TextureImporter)AssetImporter.GetAtPath(arq);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = PPU;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;

            var ajustes = new TextureImporterSettings();
            ti.ReadTextureSettings(ajustes);
            ajustes.spriteAlignment = (int)SpriteAlignment.Custom;
            ajustes.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(ajustes);

            ti.spritePivot = new Vector2(0.5f, 0.5f);   // visao de cima: tudo gira/encaixa pelo centro
            if (nome.StartsWith("ui_")) ti.spriteBorder = new Vector4(4, 4, 4, 4);
            ti.SaveAndReimport();
        }

        var fi = (TrueTypeFontImporter)AssetImporter.GetAtPath("Assets/Fontes/PixelDHS.ttf");
        fi.fontRenderingMode = FontRenderingMode.HintedRaster;
        fi.SaveAndReimport();
        AssetDatabase.Refresh();
    }

    static Sprite S(string nome)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(SPR + nome + ".png");
        if (s == null) throw new Exception("sprite nao encontrado: " + nome);
        return s;
    }

    static AudioClip A(string nome)
    {
        AudioClip c = AssetDatabase.LoadAssetAtPath<AudioClip>(AUD + nome + ".wav");
        if (c == null) throw new Exception("som nao encontrado: " + nome);
        return c;
    }

    static Sprite[] Quadros(string prefixo)
    {
        return new[] { S(prefixo + "_0"), S(prefixo + "_1"), S(prefixo + "_2") };
    }

    static int Camada(string nome)
    {
        int c = LayerMask.NameToLayer(nome);
        if (c < 0) throw new Exception("camada nao existe: " + nome);
        return c;
    }

    static DocumentoDados Documento(string arquivo, string titulo, string texto)
    {
        string caminho = "Assets/Dados/" + arquivo + ".asset";
        DocumentoDados doc = AssetDatabase.LoadAssetAtPath<DocumentoDados>(caminho);
        if (doc == null)
        {
            doc = ScriptableObject.CreateInstance<DocumentoDados>();
            AssetDatabase.CreateAsset(doc, caminho);
        }
        doc.titulo = titulo;
        doc.texto = texto;
        EditorUtility.SetDirty(doc);
        return doc;
    }

    static DocumentoDados DocOkafor()
    {
        return Documento("Doc_Okafor", "Anotações do Agente Okafor",
            "Dia 3 de vigilância na Mansão Varkoff.\n\n" +
            "As portas do saguão estão trancadas com fechaduras de brasão. A chave fica no cofre da galeria, no primeiro andar.\n\n" +
            "O mordomo disse que o velho Varkoff troca a combinação toda semana. Desta vez é \"o ano em que ele nasceu de novo\". " +
            "Ainda contou que o patrão mandou gravar isso no próprio retrato, aqui no saguão. Excêntrico.\n\n" +
            "Ouvi passos arrastados no corredor. Vou verificar.\n\n- D. Okafor, DHS");
    }

    static DocumentoDados DocMordomo()
    {
        return Documento("Doc_Mordomo", "Bilhete do mordomo",
            "Ao novo funcionário,\n\n" +
            "O Sr. Varkoff não confia em bancos, nem em cofres, nem em ninguém. Ele esconde coisas pela casa inteira: " +
            "munição, remédios, papéis. Dentro dos móveis, atrás dos livros, até nos relógios.\n\n" +
            "Se precisar de algo, procure com calma. E nunca, jamais, toque no retrato dele.\n\n- Gerard, mordomo");
    }

    // ================================================================ objetos basicos

    static GameObject Novo(string nome, Transform pai = null, Vector2 pos = default)
    {
        var go = new GameObject(nome);
        if (pai != null) go.transform.SetParent(pai, false);
        go.transform.localPosition = pos;
        return go;
    }

    static SpriteRenderer Sprite(GameObject go, Sprite s, int ordem = 0, bool brilha = false)
    {
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = ordem;
        sr.sharedMaterial = brilha ? matUnlit : matLit;
        return sr;
    }

    static SpriteRenderer Enfeite(Transform pai, string nome, string sprite, Vector2 pos, int ordem = 0)
    {
        return Sprite(Novo(nome, pai, pos), S(sprite), ordem);
    }

    static BoxCollider2D Solido(GameObject go, Vector2 tamanho, Vector2 offset)
    {
        go.layer = Camada("Parede");
        var bc = go.AddComponent<BoxCollider2D>();
        bc.size = tamanho;
        bc.offset = offset;
        return bc;
    }

    static GameObject AreaInteracao(GameObject dono, Vector2 centroLocal, Vector2 tamanho)
    {
        var go = Novo("Interacao", dono.transform, centroLocal);
        go.layer = Camada("Interagivel");
        var bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = tamanho;
        return go;
    }

    static Light2D LuzPonto(GameObject go, Color cor, float raio, float intensidade, float raioInterno = 0f)
    {
        var l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = cor;
        l.intensity = intensidade;
        l.pointLightOuterRadius = raio;
        l.pointLightInnerRadius = raioInterno;
        l.falloffIntensity = 0.6f;
        return l;
    }

    static AudioSource Som(GameObject go)
    {
        var a = go.AddComponent<AudioSource>();
        a.playOnAwake = false;
        a.spatialBlend = 0;
        return a;
    }

    static GameObject Instanciar(GameObject prefab, Vector2 pos)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = pos;
        return go;
    }

    static void Gravar(Component c)
    {
        PrefabUtility.RecordPrefabInstancePropertyModifications(c);
    }

    // ================================================================ prefabs (visao de cima: pivo no centro)

    static GameObject PrefabItem()
    {
        var go = Novo("ItemNoChao");
        go.layer = Camada("Interagivel");
        var sr = Sprite(go, S("item_pistola"), 5);
        var cc = go.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.4f;

        var br = Sprite(Novo("Brilho", go.transform, new Vector2(0.25f, 0.25f)), S("brilho"), 6, true);
        var luz = Novo("Luz", go.transform);
        LuzPonto(luz, new Color(1f, 0.95f, 0.8f), 0.9f, 0.5f);

        var item = go.AddComponent<ItemNoChao>();
        item.sr = sr;
        item.brilho = br;
        item.item = new ItemInstancia();
        item.acordarAoPegar = new Zumbi[0];
        item.somPegar = A("pegar");
        item.alturaAviso = 0.6f;
        return Salvar(go, "Assets/Prefabs/ItemNoChao.prefab");
    }

    static Rigidbody2D Corpo2D(GameObject go, float massa)
    {
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.mass = massa;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        return rb;
    }

    static GameObject PrefabZumbi()
    {
        var go = Novo("Zumbi");
        go.layer = Camada("Zumbi");
        Corpo2D(go, 4);
        var fisico = go.AddComponent<CircleCollider2D>();
        fisico.radius = 0.3f;

        var visual = Sprite(Novo("Visual", go.transform), S("zum_0"), 10);

        var hb = Novo("Hurtbox", go.transform);
        hb.layer = Camada("Zumbi");
        var hbc = hb.AddComponent<CircleCollider2D>();
        hbc.isTrigger = true;
        hbc.radius = 0.45f;

        var poca = Novo("Poca", go.transform);
        Sprite(poca, S("poca_0"), -85);

        Som(go);
        var z = go.AddComponent<Zumbi>();
        z.spritesAndar = new[] { S("zum_0"), S("zum_1"), S("zum_2") };
        z.spriteCaido = S("zum_caido");
        z.spriteLevantando = S("zum_levantando");
        z.pocaDeSangue = poca;
        z.visual = visual;
        z.corpoFisico = fisico;
        z.hurtbox = hbc;
        z.camadaParede = LayerMask.GetMask("Parede");
        z.alcanceAtaque = 0.75f;
        z.somGrunhido = A("grunhido");
        z.somGemido = A("gemido");
        z.somImpacto = A("impacto");
        return Salvar(go, "Assets/Prefabs/Zumbi.prefab");
    }

    static GameObject PrefabJogador(GameObject prefabItem)
    {
        var go = Novo("Jogador");
        go.layer = Camada("Jogador");
        Corpo2D(go, 1);
        var fisico = go.AddComponent<CircleCollider2D>();
        fisico.radius = 0.28f;
        var grupo = go.AddComponent<SortingGroup>();
        grupo.sortingOrder = 10;
        Som(go);

        // "Corpo" gira para a mira e leva junto o sprite, o cano e a lanterna
        var corpo = Novo("Corpo", go.transform);
        var visual = Sprite(Novo("Visual", corpo.transform), S("jog_maos_0"), 0);
        var boca = Novo("BocaDoCano", corpo.transform, new Vector2(0.5f, 0f));
        var clarao = Novo("Clarao", boca.transform, new Vector2(0.12f, 0));
        Sprite(clarao, S("clarao"), 3, true);
        LuzPonto(clarao, new Color(1f, 0.85f, 0.5f), 3.5f, 1.4f);

        var lanterna = Novo("Lanterna", corpo.transform);
        lanterna.transform.localRotation = Quaternion.Euler(0, 0, -90);   // o cone da Light2D aponta para o "cima" local
        var luzLanterna = LuzPonto(lanterna, new Color(1f, 0.96f, 0.85f), 6.5f, 1.1f, 0.4f);
        luzLanterna.pointLightInnerAngle = 28;
        luzLanterna.pointLightOuterAngle = 58;
        luzLanterna.shadowsEnabled = true;
        luzLanterna.shadowIntensity = 0.85f;
        var aura = Novo("Aura", go.transform);
        LuzPonto(aura, new Color(0.8f, 0.85f, 1f), 1.6f, 0.4f);

        var rastroGo = Novo("Rastro", go.transform);
        var rastro = rastroGo.AddComponent<LineRenderer>();
        rastro.useWorldSpace = true;
        rastro.positionCount = 2;
        rastro.startWidth = 0.05f;
        rastro.endWidth = 0.02f;
        rastro.sharedMaterial = matUnlit;
        rastro.startColor = new Color(1f, 0.95f, 0.7f, 1f);
        rastro.endColor = new Color(1f, 0.8f, 0.4f, 0.4f);
        rastro.sortingOrder = 4;

        var j = go.AddComponent<Jogador>();
        j.spritesPistola = Quadros("jog_pistola");
        j.spritesFaca = Quadros("jog_faca");
        j.spritesMaos = Quadros("jog_maos");
        j.spriteMorto = S("jog_morto");
        j.corpo = corpo.transform;
        j.visual = visual;
        j.lanterna = lanterna;
        j.somDano = A("dano");
        j.somCura = A("cura");

        var inv = go.AddComponent<Inventario>();
        inv.prefabItemNoChao = prefabItem;
        inv.itens.Add(new ItemInstancia { tipo = TipoItem.Faca, durabilidade = 5 });

        var arma = go.AddComponent<Arma>();
        arma.camadasTiro = LayerMask.GetMask("Parede", "Zumbi");
        arma.camadaZumbi = LayerMask.GetMask("Zumbi");
        arma.bocaDoCano = boca.transform;
        arma.rastro = rastro;
        arma.clarao = clarao;
        arma.somTiro = A("tiro");
        arma.somVazio = A("vazio");
        arma.somRecarga = A("recarga");
        arma.somFaca = A("faca");
        arma.somFacaQuebra = A("faca_quebra");

        var inter = go.AddComponent<Interacao>();
        inter.camadaInteragivel = LayerMask.GetMask("Interagivel");
        return Salvar(go, "Assets/Prefabs/Jogador.prefab");
    }

    static GameObject Salvar(GameObject go, string caminho)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, caminho);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ================================================================ sala 01: saguao (terreo) + galeria (1o andar)

    static Tile CriarTile(string sprite, bool solido)
    {
        string caminho = "Assets/Tiles/" + sprite + ".asset";
        Tile t = AssetDatabase.LoadAssetAtPath<Tile>(caminho);
        if (t == null)
        {
            t = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(t, caminho);
        }
        t.sprite = S(sprite);
        t.colliderType = solido ? Tile.ColliderType.Grid : Tile.ColliderType.None;
        EditorUtility.SetDirty(t);
        return t;
    }

    static Tilemap NovoTilemap(Transform grid, string nome, int ordem)
    {
        var go = Novo(nome, grid);
        var tm = go.AddComponent<Tilemap>();
        var tr = go.AddComponent<TilemapRenderer>();
        tr.sortingOrder = ordem;
        tr.sharedMaterial = matLit;
        return tm;
    }

    // Terreo em (0,0); a galeria fica mais acima no mundo, em (0, GY). A escada leva de um para o outro.
    // Paredes tem 1 tile de espessura (visao de cima).
    const int SW = 32;   // saguao: paredes em x=0 e x=31, piso x 1..30
    const int SH = 14;   // saguao: paredes em y=0 e y=13, piso y 1..12
    const int GW = 38;   // galeria: paredes em x=0 e x=37
    const int GH = 22;   // galeria: paredes em gy=0 e gy=21
    const int GY = 30;

    static void MontarSala(DocumentoDados doc, DocumentoDados bilhete)
    {
        var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject prefabItem = PrefabItem();
        GameObject prefabZumbi = PrefabZumbi();
        GameObject prefabJogador = PrefabJogador(prefabItem);

        // ---------------- tiles
        Tile[] pisos = { CriarTile("piso_0", false), CriarTile("piso_1", false), CriarTile("piso_2", false) };
        Tile tapT = CriarTile("tapete_t", false), tapC = CriarTile("tapete_c", false), tapB = CriarTile("tapete_b", false);
        Tile topo = CriarTile("parede_topo", true);
        Tile corrH = CriarTile("corrimao_h", true), corrH2 = CriarTile("corrimao_h2", true), corrV = CriarTile("corrimao_v", true);
        Tile[] vaos = { CriarTile("vao_0", true), CriarTile("vao_1", true) };
        Tile sombraN = CriarTile("sombra_n", false), sombraW = CriarTile("sombra_w", false), sombraNW = CriarTile("sombra_nw", false);

        var grid = Novo("Grid");
        grid.AddComponent<Grid>();
        Tilemap piso = NovoTilemap(grid.transform, "Piso", -100);
        Tilemap tapete = NovoTilemap(grid.transform, "Tapete", -95);
        Tilemap sombras = NovoTilemap(grid.transform, "Sombras", -90);
        Tilemap paredes = NovoTilemap(grid.transform, "Paredes", -50);
        paredes.gameObject.layer = Camada("Parede");
        var rbParede = paredes.gameObject.AddComponent<Rigidbody2D>();
        rbParede.bodyType = RigidbodyType2D.Static;
        var tc = paredes.gameObject.AddComponent<TilemapCollider2D>();
        tc.compositeOperation = Collider2D.CompositeOperation.Merge;
        var composto = paredes.gameObject.AddComponent<CompositeCollider2D>();
        composto.geometryType = CompositeCollider2D.GeometryType.Polygons;   // poligonos solidos, nao so contornos

        var rnd = new System.Random(4);
        var muros = new System.Collections.Generic.HashSet<Vector2Int>();   // paredes que jogam sombra
        void Piso(int x, int y) => piso.SetTile(new Vector3Int(x, y, 0), pisos[rnd.Next(pisos.Length)]);
        void Muro(int x, int y)
        {
            paredes.SetTile(new Vector3Int(x, y, 0), topo);
            muros.Add(new Vector2Int(x, y));
        }
        void Abrir(int x, int y) => paredes.SetTile(new Vector3Int(x, y, 0), null);   // vao de porta (continua jogando sombra)

        // ======== TERREO: saguao
        for (int x = 1; x < SW - 1; x++)
            for (int y = 1; y < SH - 1; y++)
                Piso(x, y);
        for (int x = 0; x < SW; x++) { Muro(x, 0); Muro(x, SH - 1); }
        for (int y = 0; y < SH; y++) { Muro(0, y); Muro(SW - 1, y); }
        // passadeira da entrada (leste) ate o pe da escada (oeste)
        for (int x = 10; x < SW - 1; x++)
        {
            tapete.SetTile(new Vector3Int(x, 6, 0), tapT);
            tapete.SetTile(new Vector3Int(x, 5, 0), tapC);
            tapete.SetTile(new Vector3Int(x, 4, 0), tapB);
        }
        // portas: norte (16-17), sul (15-16), leste/entrada (y 6-7)
        Abrir(16, SH - 1); Abrir(17, SH - 1); Abrir(15, 0); Abrir(16, 0); Abrir(SW - 1, 6); Abrir(SW - 1, 7);

        // ======== 1o ANDAR: galeria em U em volta do vao do saguao
        for (int x = 1; x < GW - 1; x++)
        {
            for (int y = 1; y <= 4; y++) Piso(x, GY + y);          // ala sul
            for (int y = 17; y <= 20; y++) Piso(x, GY + y);        // ala norte
        }
        for (int x = 1; x <= 5; x++)
            for (int y = 5; y <= 16; y++)
                Piso(x, GY + y);                                   // ala oeste
        for (int x = 0; x < GW; x++) { Muro(x, GY); Muro(x, GY + GH - 1); }
        for (int y = 0; y < GH; y++) { Muro(0, GY + y); Muro(GW - 1, GY + y); }
        // vao sobre o saguao, cercado por balaustrada; a escada chega pela ala oeste (gy 9..11)
        for (int x = 6; x < GW - 1; x++)
            for (int y = 5; y <= 16; y++)
            {
                bool chegadaEscada = (x == 6 || x == 7) && y >= 9 && y <= 11;
                if (chegadaEscada) continue;
                Tile t;
                if (y == 16) t = corrH;            // vao fica ao sul: sombra para baixo
                else if (y == 5) t = corrH2;       // vao fica ao norte: sombra para cima
                else if (x == 6) t = corrV;        // vao fica a leste
                else t = vaos[rnd.Next(vaos.Length)];
                paredes.SetTile(new Vector3Int(x, GY + y, 0), t);
            }
        // portas da galeria: norte (22-23), oeste (gy 10-11), sul (21-22)
        Abrir(22, GY + GH - 1); Abrir(23, GY + GH - 1); Abrir(0, GY + 10); Abrir(0, GY + 11); Abrir(21, GY); Abrir(22, GY);

        // montando por script o Unity nao gera a geometria do colisor composto sozinho:
        // sem isso a cena e salva com as paredes sem colisao
        tc.ProcessTilemapChanges();
        composto.GenerateGeometry();
        if (composto.pathCount == 0) throw new Exception("colisor das paredes ficou vazio");
        Debug.Log("MONTA paredes: " + composto.pathCount + " contornos de colisao");

        // sombras das paredes no piso (luz vinda de cima/esquerda)
        foreach (var pos in piso.cellBounds.allPositionsWithin)
        {
            if (!piso.HasTile(pos)) continue;
            bool n = muros.Contains(new Vector2Int(pos.x, pos.y + 1));
            bool w = muros.Contains(new Vector2Int(pos.x - 1, pos.y));
            if (n && w) sombras.SetTile(pos, sombraNW);
            else if (n) sombras.SetTile(pos, sombraN);
            else if (w) sombras.SetTile(pos, sombraW);
        }

        // ---------------- camera proxima, luz global e vinheta
        var camGo = Novo("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(26, 7, -10);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.2f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGo.AddComponent<AudioListener>();
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        var ppc = camGo.AddComponent<PixelPerfectCamera>();
        ppc.assetsPPU = PPU;
        ppc.refResolutionX = 240;    // 15 x 8,4 tiles na tela: camera bem proxima
        ppc.refResolutionY = 135;
        ppc.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
        var segue = camGo.AddComponent<CameraSegue>();
        segue.limiteMin = Vector2.zero;
        segue.limiteMax = new Vector2(SW, SH);
        segue.puxaMira = 0.15f;

        var volumeGo = Novo("Pos-processamento");
        var volume = volumeGo.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = PerfilSombrio();

        var globalGo = Novo("Luz Global");
        var global = globalGo.AddComponent<Light2D>();
        global.lightType = Light2D.LightType.Global;
        global.intensity = 0.06f;

        // ---------------- jogador entra pela porta leste
        var jogador = Instanciar(prefabJogador, new Vector2(29.3f, 6.5f));
        segue.alvo = jogador.transform;
        Gravar(segue);

        var terreo = Novo("Terreo - Saguao").transform;
        var andar1 = Novo("1o Andar - Galeria").transform;

        // ---------------- portas do terreo
        var pN = PortaH(terreo, "Porta Norte", new Vector2(17, SH - 0.5f), false);
        ConfigPorta(pN, "porta_h_fechada", "porta_h_aberta");
        var pS = PortaH(terreo, "Porta Sul", new Vector2(16, 0.5f), true);
        ConfigPorta(pS, "porta_h_fechada", "porta_h_aberta");
        var pE = PortaV(terreo, "Porta da Frente (leste)", new Vector2(SW - 0.5f, 7), true);
        pE.tipo = Porta.Tipo.Emperrada;
        pE.somTrancada = A("trancada");

        // ---------------- energia, interruptor e luzes
        var energiaGo = Novo("Energia Saguao", terreo);
        var secao = energiaGo.AddComponent<SecaoEnergia>();
        secao.id = "Saguao";
        secao.luzGlobal = global;
        secao.intensidadeApagada = 0.06f;
        secao.intensidadeAcesa = 0.38f;
        var luzes = new System.Collections.Generic.List<LuzComodo>();

        foreach (float x in new[] { 4f, 11f, 22f, 27f })
            luzes.Add(Lampada(Novo("Candelabro", terreo, new Vector2(x, SH - 1.45f)), "candelabro_apagado", "candelabro_aceso", -40, false, 4f, 0.85f));
        foreach (var p in new[] { new Vector2(30.2f, 11.6f), new Vector2(12.6f, 1.6f) })
            luzes.Add(Lampada(Novo("Abajur", terreo, p), "abajur_apagado", "abajur_aceso", 0, true, 4f, 0.9f));

        var interruptor = Novo("Interruptor", terreo, new Vector2(SW - 1.05f, 9.5f));
        var srI = Sprite(interruptor, S("interruptor_off"), -40);
        interruptor.transform.localScale = new Vector3(0.7f, 0.7f, 1);
        AreaInteracao(interruptor, new Vector2(-0.6f, 0), new Vector2(0.8f, 1.2f));
        var inte = interruptor.AddComponent<Interruptor>();
        inte.secao = secao;
        inte.sr = srI;
        inte.spriteDesligado = S("interruptor_off");
        inte.spriteLigado = S("interruptor_on");
        inte.somClique = A("interruptor");
        inte.alturaAviso = 0.7f;

        // ---------------- retrato na parede sul (pista do cofre), com vela ao lado
        var retrato = Novo("Retrato", terreo, new Vector2(24, 1.2f));
        Sprite(retrato, S("retrato"), -40);
        AreaInteracao(retrato, new Vector2(0, 0.4f), new Vector2(2f, 0.7f));
        var ex = retrato.AddComponent<Examinavel>();
        ex.acao = "Examinar retrato";
        ex.titulo = "Retrato a óleo";
        ex.texto = "Aldous Varkoff, o dono da mansão. O olhar parece seguir você.\n\n" +
                   "Na placa dourada da moldura:\n\n\"A. VARKOFF\nNASCIDO EM 1941\nRENASCIDO EM 1987\"";
        ex.imagem = S("retrato_close");
        ex.alturaAviso = 1.0f;

        var mesaRet = Movel(terreo, "Mesa do Retrato", "mesa_pequena", new Vector2(26.6f, 1.6f), new Vector2(0.8f, 0.8f));
        Vela(mesaRet.transform, Vector2.zero);

        // ---------------- moveis do saguao (alguns guardam coisas)
        var relogio = Movel(terreo, "Relogio", "relogio", new Vector2(7, SH - 1.6f), new Vector2(0.7f, 0.8f));
        Esconder(relogio, "relógio", "no", new ItemInstancia { tipo = TipoItem.Municao, quantidade = 3 },
                 "Só engrenagens e o pêndulo parado.");
        var sofa = Movel(terreo, "Sofa", "sofa", new Vector2(20.5f, 1.6f), new Vector2(1.9f, 0.85f));
        sofa.AddComponent<ShadowCaster2D>();
        Esconder(sofa, "sofá", "no", null, "Só poeira e moedas antigas entre as almofadas.");
        foreach (var p in new[] { new Vector2(1.6f, SH - 1.6f), new Vector2(14.4f, SH - 1.6f), new Vector2(19.6f, SH - 1.6f), new Vector2(30.3f, 1.6f) })
            Esconder(Movel(terreo, "Vaso", "vaso", p, new Vector2(0.6f, 0.6f)), "vaso", "no", null, "Só terra e folhas secas.");

        // bilhete do mordomo na mesinha da entrada: avisa que ha coisas escondidas pela casa
        var mesaEntrada = Movel(terreo, "Mesa da Entrada", "mesa_pequena", new Vector2(29.6f, 2.8f), new Vector2(0.8f, 0.8f));
        var bil = Instanciar(prefabItem, mesaEntrada.transform.position);
        var itB = bil.GetComponent<ItemNoChao>();
        itB.item = new ItemInstancia { tipo = TipoItem.Documento, documento = bilhete };
        itB.idUnico = "bilhete";
        Gravar(itB);

        // ---------------- escada (lado oeste): 9 degraus subindo para a esquerda
        var escada = Novo("Escada", terreo, new Vector2(5.5f, 5.5f));
        Sprite(escada, S("escada_terreo"), -88);
        Solido(Novo("Corrimao Norte", escada.transform, new Vector2(0, 1.31f)), new Vector2(9, 0.38f), Vector2.zero);
        Solido(Novo("Corrimao Sul", escada.transform, new Vector2(0, -1.31f)), new Vector2(9, 0.38f), Vector2.zero);

        // ---------------- zumbi "morto" no pe da escada; levanta quando a pistola e pega
        var zumbi = Instanciar(prefabZumbi, new Vector2(11.6f, 5.5f));
        var z = zumbi.GetComponent<Zumbi>();

        // ---------------- corpo do Okafor (canto nordeste) com o documento; a pistola dele ao lado
        var corpoGo = Novo("Corpo do Agente Okafor", terreo, new Vector2(24.6f, 10.6f));
        Sprite(corpoGo, S("corpo_agente"), -70);
        AreaInteracao(corpoGo, Vector2.zero, new Vector2(1.4f, 1.2f));
        var corpo = corpoGo.AddComponent<Corpo>();
        corpo.itemGuardado = new ItemInstancia { tipo = TipoItem.Documento, documento = doc };
        corpo.somPegar = A("papel");
        corpo.alturaAviso = 0.9f;

        var pistola = Instanciar(prefabItem, new Vector2(22.4f, 10.0f));
        var itP = pistola.GetComponent<ItemNoChao>();
        itP.item = new ItemInstancia { tipo = TipoItem.Pistola, balasNoPente = 7 };
        itP.idUnico = "pistola";
        itP.acordarAoPegar = new[] { z };
        Gravar(itP);

        var spray = Instanciar(prefabItem, new Vector2(2.5f, 2.0f));
        var itS = spray.GetComponent<ItemNoChao>();
        itS.item = new ItemInstancia { tipo = TipoItem.Spray };
        itS.idUnico = "spray";
        Gravar(itS);

        // ---------------- transicoes da escada
        var chegadaGaleria = Novo("Chegada Galeria", andar1, new Vector2(4.3f, GY + 10.5f));
        var chegadaTerreo = Novo("Chegada Terreo", terreo, new Vector2(3.6f, 5.5f));
        var sobe = Novo("Escada Sobe", terreo, new Vector2(1.6f, 5.5f));
        Gatilho(sobe, new Vector2(1.2f, 2.2f));
        var eSobe = sobe.AddComponent<Escada>();
        eSobe.destino = chegadaGaleria.transform;
        eSobe.limiteMin = new Vector2(0, GY);
        eSobe.limiteMax = new Vector2(GW, GY + GH);
        eSobe.mensagem = "1º andar - Galeria";
        eSobe.somPassos = A("porta");
        var desce = Novo("Escada Desce", andar1, new Vector2(7.6f, GY + 10.5f));
        Gatilho(desce, new Vector2(0.8f, 2.2f));
        var eDesce = desce.AddComponent<Escada>();
        eDesce.destino = chegadaTerreo.transform;
        eDesce.limiteMin = Vector2.zero;
        eDesce.limiteMax = new Vector2(SW, SH);
        eDesce.mensagem = "Térreo - Saguão";
        eDesce.somPassos = A("porta");

        // ======== objetos do 1o andar
        var escadaG = Novo("Escada (topo)", andar1, new Vector2(7, GY + 10.5f));
        Sprite(escadaG, S("escada_galeria"), -88);
        Solido(Novo("Corrimao Norte", escadaG.transform, new Vector2(0, 1.31f)), new Vector2(2, 0.38f), Vector2.zero);
        Solido(Novo("Corrimao Sul", escadaG.transform, new Vector2(0, -1.31f)), new Vector2(2, 0.38f), Vector2.zero);

        foreach (float x in new[] { 6f, 14f, 30f })
            luzes.Add(Lampada(Novo("Candelabro", andar1, new Vector2(x, GY + GH - 1.45f)), "candelabro_apagado", "candelabro_aceso", -40, false, 4f, 0.85f));
        luzes.Add(Lampada(Novo("Abajur", andar1, new Vector2(2.2f, GY + 1.6f)), "abajur_apagado", "abajur_aceso", 0, true, 4f, 0.9f));
        secao.luzes = luzes.ToArray();

        foreach (float x in new[] { 10f, 27f, 33f })
        {
            var est = Movel(andar1, "Estante", "estante", new Vector2(x, GY + GH - 1.5f), new Vector2(1.9f, 0.8f));
            est.AddComponent<ShadowCaster2D>();
            bool comMunicao = x == 27f;
            Esconder(est, "estante", "na", comMunicao ? new ItemInstancia { tipo = TipoItem.Municao, quantidade = 4 } : null,
                     "Só livros de química e genética. Muitas páginas sublinhadas.");
        }
        foreach (var p in new[] { new Vector2(1.6f, GY + GH - 1.6f), new Vector2(36.4f, GY + GH - 1.6f), new Vector2(1.6f, GY + 5.6f) })
            Esconder(Movel(andar1, "Vaso", "vaso", p, new Vector2(0.6f, 0.6f)), "vaso", "no", null, "Só terra e folhas secas.");

        // vela no fim da ala sul, perto do cofre
        var mesaCofre = Movel(andar1, "Mesinha", "mesa_pequena", new Vector2(34.4f, GY + 1.6f), new Vector2(0.8f, 0.8f));
        Vela(mesaCofre.transform, Vector2.zero);

        // cofre no fim da ala sul: guarda a chave das portas do terreo
        var cofreGo = Movel(andar1, "Cofre", "cofre_fechado", new Vector2(36.3f, GY + 2.5f), new Vector2(0.8f, 0.8f));
        AreaInteracao(cofreGo, new Vector2(-0.4f, 0), new Vector2(1.2f, 1.2f));
        var cofre = cofreGo.AddComponent<Cofre>();
        cofre.codigo = "1987";
        cofre.sr = cofreGo.GetComponent<SpriteRenderer>();
        cofre.spriteFechado = S("cofre_fechado");
        cofre.spriteAberto = S("cofre_aberto");
        cofre.somAbrir = A("cofre");
        cofre.somPegar = A("pegar");
        cofre.conteudo = new ItemInstancia { tipo = TipoItem.Chave, idChave = "brasao" };
        cofre.alturaAviso = 0.8f;

        // portas da galeria: fechaduras diferentes, para os proximos puzzles
        PortaFutura(PortaH(andar1, "Porta Galeria Norte", new Vector2(23, GY + GH - 0.5f), false), "lua");
        PortaFutura(PortaV(andar1, "Porta Galeria Oeste", new Vector2(0.5f, GY + 11), false), "sol");
        PortaFutura(PortaH(andar1, "Porta Galeria Sul", new Vector2(22, GY + 0.5f), true), "estrela");

        // ---------------- sistemas
        var sangue = Novo("PixelsSangue");
        var ps = sangue.AddComponent<PixelsSangue>();
        ps.pixel = S("pixel");
        ps.material = matLit;

        var musicaGo = Novo("Musica");
        var mus = musicaGo.AddComponent<Musica>();
        mus.ambiente = Som(musicaGo);
        mus.ambiente.clip = A("musica_ambiente");
        mus.ambiente.loop = true;
        mus.tensa = Som(musicaGo);
        mus.tensa.clip = A("musica_tensa");
        mus.tensa.loop = true;

        var gcGo = Novo("GameController");
        Som(gcGo);
        var gc = gcGo.AddComponent<GameController>();
        gc.somMorte = A("morte");
        var cat = gcGo.AddComponent<CatalogoItens>();
        cat.iconePistola = S("item_pistola");
        cat.iconeMunicao = S("item_municao");
        cat.iconeFaca = S("item_faca");
        cat.iconeSpray = S("item_spray");
        cat.iconeDocumento = S("item_documento");
        cat.iconeChave = S("item_chave");
        var rest = gcGo.AddComponent<RestauradorSala>();
        rest.prefabItemNoChao = prefabItem;

        MontarUISala(gc, jogador);

        EditorSceneManager.SaveScene(cena, CENA_SALA);
    }

    static VolumeProfile PerfilSombrio()
    {
        const string caminho = "Assets/Settings/PerfilSombrio.asset";
        AssetDatabase.DeleteAsset(caminho);
        var perfil = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(perfil, caminho);
        var vinheta = perfil.Add<Vignette>(true);
        vinheta.intensity.Override(0.45f);
        vinheta.smoothness.Override(0.45f);
        vinheta.color.Override(Color.black);
        vinheta.name = "Vinheta";
        AssetDatabase.AddObjectToAsset(vinheta, perfil);
        EditorUtility.SetDirty(perfil);
        AssetDatabase.SaveAssets();
        return perfil;
    }

    // movel solido com sprite (pivo no centro)
    static GameObject Movel(Transform pai, string nome, string sprite, Vector2 pos, Vector2 tamanhoColisor)
    {
        var go = Novo(nome, pai, pos);
        Sprite(go, S(sprite));
        Solido(go, tamanhoColisor, Vector2.zero);
        return go;
    }

    static void Esconder(GameObject movel, string nome, string artigo, ItemInstancia item, string textoVazio)
    {
        Vector2 tam = movel.GetComponent<BoxCollider2D>().size + new Vector2(0.6f, 0.6f);
        AreaInteracao(movel, Vector2.zero, tam);
        var e = movel.AddComponent<Esconderijo>();
        e.nomeObjeto = nome;
        e.artigo = artigo;
        e.temItem = item != null;
        e.item = item ?? new ItemInstancia();
        e.textoVazio = textoVazio;
        e.somPegar = A("pegar");
        e.somVasculhar = A("papel");
        e.alturaAviso = 0.8f;
    }

    // porta numa parede horizontal (norte/sul). abreParaCima: folhas abrem para o norte (porta na parede sul)
    static Porta PortaH(Transform pai, string nome, Vector2 centro, bool abreParaCima)
    {
        var go = Novo(nome, pai, centro);
        var sr = Sprite(go, S("porta_h_fechada"), -40);
        sr.flipY = abreParaCima;
        Solido(go, new Vector2(2, 1), Vector2.zero);
        AreaInteracao(go, new Vector2(0, abreParaCima ? 0.8f : -0.8f), new Vector2(2, 0.6f));
        var p = go.AddComponent<Porta>();
        p.sr = sr;
        p.alturaAviso = abreParaCima ? 1.2f : -0.6f;
        return p;
    }

    // porta numa parede vertical (leste/oeste). abreParaEsquerda: porta na parede leste
    static Porta PortaV(Transform pai, string nome, Vector2 centro, bool abreParaEsquerda)
    {
        var go = Novo(nome, pai, centro);
        var sr = Sprite(go, S("porta_v_fechada"), -40);
        sr.flipX = abreParaEsquerda;
        Solido(go, new Vector2(1, 2), Vector2.zero);
        AreaInteracao(go, new Vector2(abreParaEsquerda ? -0.8f : 0.8f, 0), new Vector2(0.6f, 2));
        var p = go.AddComponent<Porta>();
        p.sr = sr;
        p.alturaAviso = 1.3f;
        return p;
    }

    static void ConfigPorta(Porta p, string fechada, string aberta)
    {
        p.tipo = Porta.Tipo.Trancada;
        p.idChave = "brasao";
        p.cenaDestino = "FimDemo";
        p.spriteFechada = S(fechada);
        p.spriteAberta = S(aberta);
        p.somTrancada = A("trancada");
        p.somAbrir = A("porta");
    }

    static void PortaFutura(Porta p, string simbolo)
    {
        p.tipo = Porta.Tipo.Trancada;
        p.idChave = simbolo;
        p.spriteFechada = p.sr.sprite;
        p.spriteAberta = p.sr.sprite;
        p.somTrancada = A("trancada");
        p.somAbrir = A("porta");
        string nome = simbolo == "lua" ? "uma lua" : simbolo == "sol" ? "um sol" : "uma estrela";
        p.textoTrancada = "Trancada. A fechadura tem o símbolo de " + nome + ".";
    }

    static LuzComodo Lampada(GameObject go, string apagado, string aceso, int ordem, bool solido, float raio, float intensidade)
    {
        var sr = Sprite(go, S(apagado), ordem);
        if (solido) Solido(go, new Vector2(0.8f, 0.8f), Vector2.zero);
        var luz = LuzPonto(Novo("Luz", go.transform), new Color(1f, 0.8f, 0.5f), raio, intensidade);
        luz.enabled = false;
        var lc = go.AddComponent<LuzComodo>();
        lc.sr = sr;
        lc.apagado = S(apagado);
        lc.aceso = S(aceso);
        lc.luz = luz;
        return lc;
    }

    static void Vela(Transform pai, Vector2 pos)
    {
        var vela = Novo("Vela", pai, pos);
        var srVela = Sprite(vela, S("vela_0"), 1);
        var luzVela = LuzPonto(Novo("Luz", vela.transform), new Color(1f, 0.7f, 0.35f), 3f, 0.9f);
        var tremula = vela.AddComponent<LuzTremula>();
        tremula.luz = luzVela;
        tremula.sr = srVela;
        tremula.quadros = new[] { S("vela_0"), S("vela_1") };
    }

    static void Gatilho(GameObject go, Vector2 tamanho)
    {
        var bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = tamanho;
    }

    // ================================================================ UI (helpers)

    static RectTransform R(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        return rt != null ? rt : go.AddComponent<RectTransform>();
    }

    static RectTransform NovoUI(string nome, Transform pai)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        go.layer = Camada("UI");
        return R(go);
    }

    // ancora e pivo no mesmo ponto (0..1), posicao em pixels de referencia (960x540)
    static RectTransform Coloca(RectTransform rt, Vector2 ancora, Vector2 pos, Vector2 tam)
    {
        rt.anchorMin = rt.anchorMax = ancora;
        rt.pivot = ancora;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        return rt;
    }

    static RectTransform Estica(RectTransform rt, float margem = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margem, margem);
        rt.offsetMax = new Vector2(-margem, -margem);
        return rt;
    }

    static Image Img(RectTransform rt, Sprite s, Color cor, bool fatiada = true)
    {
        var im = rt.gameObject.AddComponent<Image>();
        im.sprite = s;
        im.color = cor;
        im.type = fatiada && s != null ? Image.Type.Sliced : Image.Type.Simple;
        if (fatiada && s != null) im.pixelsPerUnitMultiplier = 0.5f;   // borda de 2px vira 4px na tela
        return im;
    }

    static Text Txt(RectTransform rt, string conteudo, int tam, Color cor, TextAnchor alin = TextAnchor.MiddleCenter, bool sombra = true)
    {
        var t = rt.gameObject.AddComponent<Text>();
        t.font = fonte;
        t.fontSize = tam;
        t.color = cor;
        t.alignment = alin;
        t.text = conteudo;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.lineSpacing = 1.1f;
        if (sombra)
        {
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.9f);
            sh.effectDistance = new Vector2(2, -2);
        }
        return t;
    }

    static Text Rotulo(Transform pai, string nome, string conteudo, int tam, Color cor, Vector2 ancora, Vector2 pos, Vector2 tam2,
                       TextAnchor alin = TextAnchor.MiddleCenter, bool sombra = true)
    {
        return Txt(Coloca(NovoUI(nome, pai), ancora, pos, tam2), conteudo, tam, cor, alin, sombra);
    }

    static Button Botao(Transform pai, string rotulo, Vector2 ancora, Vector2 pos, Vector2 tam, UnityAction acao, out Text texto)
    {
        var rt = Coloca(NovoUI("Botao " + rotulo, pai), ancora, pos, tam);
        var im = Img(rt, S("ui_botao"), Color.white);
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = im;
        var cores = b.colors;
        cores.highlightedColor = new Color(1.3f, 1.2f, 1f);
        cores.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        cores.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.8f);
        b.colors = cores;
        texto = Txt(Estica(NovoUI("Texto", rt)), rotulo, 20, CREME);
        if (acao != null) UnityEventTools.AddPersistentListener(b.onClick, acao);
        return b;
    }

    static Button Botao(Transform pai, string rotulo, Vector2 ancora, Vector2 pos, Vector2 tam, UnityAction acao)
    {
        return Botao(pai, rotulo, ancora, pos, tam, acao, out _);
    }

    static Canvas NovoCanvas()
    {
        var go = new GameObject("Canvas", typeof(RectTransform));
        go.layer = Camada("UI");
        var c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.pixelPerfect = true;
        var cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(960, 540);
        cs.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        var modulo = es.AddComponent<InputSystemUIInputModule>();
        modulo.AssignDefaultActions();
        return c;
    }

    static readonly Vector2 C = new Vector2(0.5f, 0.5f);

    // ================================================================ UI da sala

    static void MontarUISala(GameController gc, GameObject jogador)
    {
        var canvas = NovoCanvas().transform;
        var inv = jogador.GetComponent<Inventario>();
        var arma = jogador.GetComponent<Arma>();
        var jog = jogador.GetComponent<Jogador>();

        // ---------------- HUD
        var hudRt = Estica(NovoUI("HUD", canvas));
        var hud = hudRt.gameObject.AddComponent<HUD>();
        hud.inventario = inv;
        hud.arma = arma;

        var flash = Estica(NovoUI("FlashDano", hudRt));
        hud.flash = Img(flash, null, new Color(0.6f, 0, 0, 0), false);
        hud.flash.raycastTarget = false;

        var fade = Estica(NovoUI("Fade", hudRt));
        hud.fade = Img(fade, null, new Color(0, 0, 0, 0), false);
        hud.fade.raycastTarget = false;

        var grupo = Coloca(NovoUI("Pistola", hudRt), new Vector2(0, 0), new Vector2(16, 44), new Vector2(260, 40));
        hud.grupoPistola = grupo.gameObject;
        var icone = Coloca(NovoUI("Icone", grupo), new Vector2(0, 0.5f), Vector2.zero, new Vector2(40, 40));
        Img(icone, S("item_pistola"), Color.white, false).raycastTarget = false;
        hud.textoMunicao = Rotulo(grupo, "Municao", "7 / 0", 20, CREME, new Vector2(0, 0.5f), new Vector2(48, 0), new Vector2(210, 30), TextAnchor.MiddleLeft);
        hud.textoFaca = Rotulo(hudRt, "Faca", "FACA", 20, CREME, new Vector2(0, 0), new Vector2(20, 14), new Vector2(300, 28), TextAnchor.MiddleLeft);

        hud.textoMensagem = Rotulo(hudRt, "Mensagem", "", 20, CREME, new Vector2(0.5f, 0), new Vector2(0, 90), new Vector2(860, 60));

        hud.aviso = Coloca(NovoUI("Aviso", hudRt), C, Vector2.zero, new Vector2(360, 30));
        hud.aviso.pivot = new Vector2(0.5f, 0);
        hud.textoAviso = Txt(Estica(NovoUI("Texto", hud.aviso)), "[E]", 20, OURO);

        hud.textoIntro = Rotulo(hudRt, "Intro",
            "MANSÃO VARKOFF - 23:47\nO Agente Okafor sumiu aqui dentro há dois dias. Encontre-o.\nEstá escuro... deve haver um interruptor perto da entrada.",
            20, CREME, new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(900, 100));

        // ---------------- inventario
        var invUI = canvas.gameObject.AddComponent<InventarioUI>();
        invUI.inventario = inv;
        invUI.jogador = jog;
        invUI.arma = arma;
        invUI.somAbrir = A("papel");
        invUI.slotNormal = S("ui_slot");
        invUI.slotSelecionado = S("ui_slot_sel");

        var pInv = Coloca(NovoUI("Inventario", canvas), C, Vector2.zero, new Vector2(660, 340));
        Img(pInv, S("ui_painel"), new Color(1, 1, 1, 0.96f));
        invUI.painel = pInv.gameObject;
        Rotulo(pInv, "Titulo", "INVENTÁRIO", 20, OURO, new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(400, 30));
        invUI.fundos = new Image[6];
        invUI.icones = new Image[6];
        invUI.quantidades = new Text[6];
        for (int i = 0; i < 6; i++)
        {
            int col = i % 3, lin = i / 3;
            var slot = Coloca(NovoUI("Slot " + (i + 1), pInv), new Vector2(0, 1), new Vector2(28 + col * 84, -58 - lin * 84), new Vector2(76, 76));
            invUI.fundos[i] = Img(slot, S("ui_slot"), Color.white);
            var b = slot.gameObject.AddComponent<Button>();
            b.targetGraphic = invUI.fundos[i];
            UnityEventTools.AddIntPersistentListener(b.onClick, invUI.Selecionar, i);
            var ic = Coloca(NovoUI("Icone", slot), C, Vector2.zero, new Vector2(48, 48));
            invUI.icones[i] = Img(ic, null, Color.white, false);
            invUI.icones[i].raycastTarget = false;
            invUI.quantidades[i] = Rotulo(slot, "Qtd", "", 20, CREME, new Vector2(1, 0), new Vector2(-6, 4), new Vector2(60, 24), TextAnchor.LowerRight);
            Rotulo(slot, "Num", (i + 1).ToString(), 10, new Color(1, 1, 1, 0.4f), new Vector2(0, 1), new Vector2(6, -4), new Vector2(20, 14), TextAnchor.UpperLeft, false);
        }
        invUI.textoNome = Rotulo(pInv, "Nome", "", 20, OURO, new Vector2(0, 1), new Vector2(300, -58), new Vector2(340, 30), TextAnchor.UpperLeft);
        invUI.textoDescricao = Rotulo(pInv, "Descricao", "", 20, CREME, new Vector2(0, 1), new Vector2(300, -96), new Vector2(340, 140), TextAnchor.UpperLeft);
        invUI.botaoUsar = Botao(pInv, "USAR", new Vector2(0, 0), new Vector2(300, 52), new Vector2(160, 40), invUI.Usar, out invUI.textoUsar);
        invUI.botaoLargar = Botao(pInv, "LARGAR", new Vector2(0, 0), new Vector2(476, 52), new Vector2(160, 40), invUI.Largar);
        Rotulo(pInv, "Ajuda", "[TAB] fechar   [1-6] escolher   O jogo NÃO pausa!", 10, new Color(1, 1, 1, 0.6f),
               new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(620, 20), TextAnchor.MiddleCenter, false);

        // ---------------- leitura (documentos e retrato)
        var leituraGo = Novo("Leitura", canvas);
        var leitura = leituraGo.AddComponent<LeituraUI>();
        var pLei = Coloca(NovoUI("Painel", canvas), C, Vector2.zero, new Vector2(760, 420));
        Img(pLei, S("ui_papel"), Color.white);
        leitura.painel = pLei.gameObject;
        leitura.textoTitulo = Rotulo(pLei, "Titulo", "", 20, new Color32(120, 30, 24, 255), new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(700, 30), TextAnchor.MiddleCenter, false);
        var img = Coloca(NovoUI("Imagem", pLei), new Vector2(0, 0.5f), new Vector2(26, 8), new Vector2(192, 256));
        leitura.imagem = Img(img, null, Color.white, false);
        leitura.imagem.preserveAspect = true;
        var area = NovoUI("Texto", pLei);
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(30, 50);
        area.offsetMax = new Vector2(-30, -64);
        leitura.areaTexto = area;
        leitura.textoCorpo = Txt(area, "", 20, TINTA, TextAnchor.UpperLeft, false);
        Rotulo(pLei, "Ajuda", "[E] fechar", 10, new Color32(90, 70, 50, 255), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(300, 20), TextAnchor.MiddleCenter, false);
        leitura.audioSource = Som(leituraGo);
        leitura.somPapel = A("papel");
        pLei.gameObject.SetActive(false);

        // ---------------- teclado do cofre
        var tecladoGo = Novo("Teclado", canvas);
        var tec = tecladoGo.AddComponent<TecladoUI>();
        var pTec = Coloca(NovoUI("Painel", canvas), C, Vector2.zero, new Vector2(300, 430));
        Img(pTec, S("ui_painel"), new Color(0.75f, 0.8f, 0.9f, 1f));
        tec.painel = pTec.gameObject;
        Rotulo(pTec, "Titulo", "COFRE", 20, CREME, new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(260, 28));
        var visor = Coloca(NovoUI("Visor", pTec), new Vector2(0.5f, 1), new Vector2(0, -52), new Vector2(220, 56));
        tec.fundoVisor = Img(visor, null, new Color32(20, 40, 24, 255), false);
        tec.visor = Txt(Estica(NovoUI("Texto", visor)), "____", 40, new Color32(120, 255, 140, 255), TextAnchor.MiddleCenter, false);
        string[] teclas = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", "OK" };
        for (int i = 0; i < teclas.Length; i++)
        {
            int col = i % 3, lin = i / 3;
            var pos = new Vector2(-76 + col * 76, -136 - lin * 62);
            Button b = Botao(pTec, teclas[i], new Vector2(0.5f, 1), pos, new Vector2(68, 54), null);
            R(b.gameObject).pivot = new Vector2(0.5f, 1);
            if (teclas[i] == "C") UnityEventTools.AddPersistentListener(b.onClick, tec.Apagar);
            else if (teclas[i] == "OK") UnityEventTools.AddPersistentListener(b.onClick, tec.Confirmar);
            else UnityEventTools.AddIntPersistentListener(b.onClick, tec.Digitar, int.Parse(teclas[i]));
        }
        Rotulo(pTec, "Ajuda", "[ESC] sair", 10, new Color(1, 1, 1, 0.6f), new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(260, 20), TextAnchor.MiddleCenter, false);
        tec.audioSource = Som(tecladoGo);
        tec.somBip = A("bip");
        tec.somErro = A("erro");
        pTec.gameObject.SetActive(false);

        // ---------------- pausa
        var pPausa = Estica(NovoUI("Pausa", canvas));
        Img(pPausa, null, new Color(0, 0, 0, 0.78f), false);
        Rotulo(pPausa, "Titulo", "PAUSADO", 40, OURO, C, new Vector2(0, 120), new Vector2(600, 60));
        Botao(pPausa, "CONTINUAR", C, new Vector2(0, 30), new Vector2(280, 44), gc.Continuar);
        Botao(pPausa, "REINICIAR SALA", C, new Vector2(0, -26), new Vector2(280, 44), gc.ReiniciarSala);
        Botao(pPausa, "MENU PRINCIPAL", C, new Vector2(0, -82), new Vector2(280, 44), gc.IrMenu);
        Rotulo(pPausa, "Controles", "WASD mover   MOUSE mirar   CLIQUE atirar   BOTÃO DIR. faca\nR recarregar   E interagir   TAB inventário   ESC pausa",
               10, new Color(1, 1, 1, 0.7f), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(900, 40));
        gc.painelPausa = pPausa.gameObject;

        // ---------------- morte
        var pMorte = Estica(NovoUI("Morte", canvas));
        Img(pMorte, null, new Color(0.18f, 0, 0, 0.88f), false);
        Rotulo(pMorte, "Titulo", "VOCÊ MORREU", 50, new Color32(230, 50, 40, 255), C, new Vector2(0, 110), new Vector2(800, 70));
        Rotulo(pMorte, "Sub", "A mansão fez mais uma vítima.", 20, CREME, C, new Vector2(0, 50), new Vector2(800, 30));
        Botao(pMorte, "TENTAR DE NOVO", C, new Vector2(0, -20), new Vector2(280, 44), gc.ReiniciarSala);
        Botao(pMorte, "MENU PRINCIPAL", C, new Vector2(0, -76), new Vector2(280, 44), gc.IrMenu);
        gc.painelMorte = pMorte.gameObject;

        // a mira fica por cima de tudo
        hud.mira = Coloca(NovoUI("Mira", canvas), C, Vector2.zero, new Vector2(48, 48));
        Img(hud.mira, S("mira"), Color.white, false).raycastTarget = false;
    }

    // ================================================================ menu e fim

    static void CameraSimples()
    {
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0, 0, -10);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGo.AddComponent<AudioListener>();
    }

    static void MontarMenu()
    {
        var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CameraSimples();
        var canvas = NovoCanvas().transform;

        var fundo = Estica(NovoUI("Fundo", canvas));
        Img(fundo, S("fundo_menu"), Color.white, false);

        var menuGo = Novo("Menu");
        var menu = menuGo.AddComponent<MenuPrincipal>();
        var musica = Som(menuGo);
        musica.clip = A("musica_ambiente");
        musica.loop = true;
        musica.playOnAwake = true;
        musica.volume = 0.5f;

        Rotulo(canvas, "Titulo", "MANSÃO VARKOFF", 50, OURO, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(900, 70));
        Rotulo(canvas, "Sub", "ARQUIVO DHS - CASO OUROBOROS", 20, CREME, new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(900, 30));

        Botao(canvas, "NOVO JOGO", new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(260, 44), menu.NovoJogo);
        Botao(canvas, "CONTROLES", new Vector2(0.5f, 0), new Vector2(0, 98), new Vector2(260, 44), menu.AlternarControles);
        menu.botaoSair = Botao(canvas, "SAIR", new Vector2(0.5f, 0), new Vector2(0, 46), new Vector2(260, 44), menu.Sair).gameObject;

        var pCtrl = Coloca(NovoUI("Controles", canvas), C, Vector2.zero, new Vector2(620, 400));
        Img(pCtrl, S("ui_painel"), Color.white);
        Rotulo(pCtrl, "Titulo", "CONTROLES", 20, OURO, new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(560, 30));
        Rotulo(pCtrl, "Lista",
            "WASD / SETAS ....... mover\n" +
            "MOUSE .............. mirar\n" +
            "CLIQUE ESQUERDO .... atirar\n" +
            "CLIQUE DIREITO ..... faca\n" +
            "R .................. recarregar\n" +
            "E .................. interagir\n" +
            "TAB ................ inventário\n" +
            "ESC ................ pausa\n\n" +
            "Munição é escassa. Zumbis caídos podem ser\nfinalizados com a faca. O inventário não pausa.",
            20, CREME, new Vector2(0.5f, 1), new Vector2(0, -64), new Vector2(560, 290), TextAnchor.UpperLeft);
        Botao(pCtrl, "VOLTAR", new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(200, 40), menu.AlternarControles);
        menu.painelControles = pCtrl.gameObject;

        EditorSceneManager.SaveScene(cena, CENA_MENU);
    }

    static void MontarFim()
    {
        var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CameraSimples();
        var canvas = NovoCanvas().transform;
        var fimGo = Novo("Fim");
        var fim = fimGo.AddComponent<FimDemo>();
        var musica = Som(fimGo);
        musica.clip = A("musica_ambiente");
        musica.loop = true;
        musica.playOnAwake = true;
        musica.volume = 0.4f;

        Rotulo(canvas, "Titulo", "FIM DA DEMONSTRAÇÃO", 40, OURO, C, new Vector2(0, 90), new Vector2(900, 60));
        Rotulo(canvas, "Texto", "A porta se abre para um corredor escuro.\nO cheiro de formol fica mais forte.\n\nA investigação continua...",
               20, CREME, C, new Vector2(0, 0), new Vector2(800, 120));
        Botao(canvas, "VOLTAR AO MENU", C, new Vector2(0, -110), new Vector2(300, 44), fim.Voltar);

        EditorSceneManager.SaveScene(cena, CENA_FIM);
    }
}
