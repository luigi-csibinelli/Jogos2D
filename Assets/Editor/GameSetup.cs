using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Sanatorio.EditorTools
{
    /// <summary>
    /// Prepara o projeto inteiro a partir do código: importação da arte e dos
    /// sons gerados pelos scripts em Tools/, materiais, prefabs de luz, camadas,
    /// as duas cenas e o Build Settings. Rodar de novo é seguro.
    ///
    /// Menu: Sanatório > Montar projeto
    /// Linha de comando: -executeMethod Sanatorio.EditorTools.GameSetup.BuildFromCommandLine
    /// </summary>
    public static class GameSetup
    {
        private const string ArtDir = "Assets/Resources/Art";
        private const string MatDir = "Assets/Resources/Materials";
        private const string PrefabDir = "Assets/Resources/Prefabs";
        private const string MenuScene = "Assets/Scenes/MainMenu.unity";
        private const string GameScene = "Assets/Scenes/Game.unity";

        [MenuItem("Sanatório/Montar projeto", false, 10)]
        public static void Menu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string report = Run();
            EditorUtility.DisplayDialog("Sanatório", report, "Ok");
            EditorSceneManager.OpenScene(MenuScene);
        }

        public static void BuildFromCommandLine()
        {
            string report = Run();
            Debug.Log("[GameSetup] " + report);
            if (report.StartsWith("ERRO")) EditorApplication.Exit(1);
        }

        public static string Run()
        {
            MapData map = MapData.Load();
            if (!map.Validate(out string mapReport))
            {
                Debug.LogError("[GameSetup] mapa inválido:\n" + mapReport);
                return "ERRO no mapa:\n" + mapReport;
            }

            ConfigureProject();
            ConfigureLayers();
            ConfigureArt();
            CreateMaterials();
            CreateLightPrefabs();
            CreateScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "ok - " + mapReport;
        }

        // ------------------------------------------------------------ projeto
        private static void ConfigureProject()
        {
            PlayerSettings.productName = "Sanatório 8-bit";
            PlayerSettings.companyName = "Insper Jogos Digitais";
            PlayerSettings.defaultWebScreenWidth = 1152;
            PlayerSettings.defaultWebScreenHeight = 648;
            PlayerSettings.runInBackground = false;
            PlayerSettings.WebGL.template = "APPLICATION:Default";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.Minimal);
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
        }

        private static void ConfigureLayers()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;
            SerializedObject tm = new SerializedObject(assets[0]);
            SerializedProperty layers = tm.FindProperty("layers");
            void Set(int i, string name)
            {
                SerializedProperty p = layers.GetArrayElementAtIndex(i);
                if (p.stringValue != name) p.stringValue = name;
            }

            Set(Layers.Wall, "Wall");
            Set(Layers.Zombie, "Zombie");
            Set(Layers.Player, "Player");
            Set(Layers.PlayerBlock, "PlayerBlock");
            Set(Layers.Pickup, "Pickup");
            tm.ApplyModifiedProperties();
        }

        private static void ConfigureArt()
        {
            if (!Directory.Exists(ArtDir)) return;
            foreach (string path in Directory.GetFiles(ArtDir, "*.png"))
            {
                string p = path.Replace('\\', '/');
                if (!(AssetImporter.GetAtPath(p) is TextureImporter imp)) continue;
                string name = Path.GetFileNameWithoutExtension(p);

                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 16f;
                imp.filterMode = name == "Vignette" ? FilterMode.Bilinear : FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.wrapMode = name == "Scanline" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                imp.isReadable = false;

                TextureImporterSettings s = new TextureImporterSettings();
                imp.ReadTextureSettings(s);
                s.spriteMeshType = SpriteMeshType.FullRect;
                s.spriteExtrude = 0;
                s.spriteAlignment = (int)SpriteAlignment.Center;
                imp.SetTextureSettings(s);
                imp.spriteBorder = name == "Panel" ? new Vector4(3, 3, 3, 3) : Vector4.zero;
                imp.SaveAndReimport();
            }
        }

        private static void CreateMaterials()
        {
            Directory.CreateDirectory(MatDir);
            MakeMaterial("SpriteLit", "Universal Render Pipeline/2D/Sprite-Lit-Default");
            MakeMaterial("SpriteUnlit", "Universal Render Pipeline/2D/Sprite-Unlit-Default");
            MakeMaterial("SpriteAdditive", "Sanatorio/SpriteAdditive");
        }

        private static void MakeMaterial(string name, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError("[GameSetup] shader não encontrado: " + shaderName);
                return;
            }

            string path = $"{MatDir}/{name}.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            else
            {
                m.shader = shader;
            }

            EditorUtility.SetDirty(m);
        }

        private static void CreateLightPrefabs()
        {
            Directory.CreateDirectory(PrefabDir);
            MakeLightPrefab("LightPoint", Light2D.LightType.Point);
            MakeLightPrefab("LightGlobal", Light2D.LightType.Global);
        }

        private static void MakeLightPrefab(string name, Light2D.LightType type)
        {
            GameObject go = new GameObject(name);
            Light2D l = go.AddComponent<Light2D>();
            l.lightType = type;

            // a luz precisa saber em quais camadas de ordenação aplicar
            SerializedObject so = new SerializedObject(l);
            SerializedProperty layers = so.FindProperty("m_ApplyToSortingLayers");
            if (layers != null)
            {
                SortingLayer[] all = SortingLayer.layers;
                layers.arraySize = all.Length;
                for (int i = 0; i < all.Length; i++) layers.GetArrayElementAtIndex(i).intValue = all[i].id;
            }

            SerializedProperty shadows = so.FindProperty("m_ShadowsEnabled");
            if (shadows != null) shadows.boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{name}.prefab");
            Object.DestroyImmediate(go);
        }

        // ------------------------------------------------------------ cenas
        private static void CreateScenes()
        {
            Directory.CreateDirectory("Assets/Scenes");

            Scene menu = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            Camera c = cam.AddComponent<Camera>();
            c.orthographic = true;
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color32(12, 10, 16, 255);
            cam.transform.position = new Vector3(0, 0, -10);
            new GameObject("Menu").AddComponent<MainMenu>();
            EditorSceneManager.SaveScene(menu, MenuScene);

            Scene game = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Game").AddComponent<Game>();
            EditorSceneManager.SaveScene(game, GameScene);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScene, true),
                new EditorBuildSettingsScene(GameScene, true),
            };
        }
    }
}
