using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sanatorio
{
    public enum GameState { Playing, Paused, Reading, GameOver }

    /// <summary>Escolhas do menu que atravessam a troca de cena.</summary>
    public static class Session
    {
        public static Difficulty Difficulty = Difficulty.Normal;
        public static int Seed = 0;
    }

    /// <summary>
    /// O "diretor" da partida: guarda mapa, grade, zonas, pontos e estado
    /// (jogando / pausado / lendo ficha / fim). Cada sistema conversa com os
    /// outros por aqui ou pelos GameEvents.
    /// </summary>
    public class Game : MonoBehaviour
    {
        public static Game I { get; private set; }

        public MapData Map { get; private set; }
        public NavGrid Nav { get; private set; }
        public ZoneManager Zones { get; private set; }
        public RoundManager Rounds { get; private set; }
        public PowerUpManager PowerUps { get; private set; }
        public World World { get; private set; }
        public Player Player { get; private set; }
        public Hud Hud { get; private set; }
        public Difficulty Difficulty { get; private set; }
        public System.Random Rng { get; private set; }

        public GameState State { get; private set; } = GameState.Playing;
        public bool Power { get; private set; }
        public int Points { get; private set; }
        public int Kills { get; set; }
        public int ShotsFired { get; set; }
        public int ShotsHit { get; set; }
        public int TotalPointsEarned { get; private set; }
        public float StartTime { get; private set; }
        public int PlankPointsThisRound { get; set; }

        public bool IsPlaying => State == GameState.Playing;

        private void Awake()
        {
            I = this;
            GameEvents.Clear();
            Time.timeScale = 1f;
            Layers.Apply();

            Difficulty = Session.Difficulty;
            int seed = Session.Seed != 0 ? Session.Seed : System.Environment.TickCount;
            Rng = new System.Random(seed);
            Random.InitState(seed);

            Map = MapData.Load();
            if (!Map.Validate(out string report))
            {
                Debug.LogError("[Game] mapa inválido:\n" + report);
            }

            AudioManager.Ensure();
            EnsureCamera();
            Nav = new NavGrid(Map);
            Zones = new ZoneManager(this);
            Fx.Create(transform);

            World = WorldBuilder.Build(this);

            // Lado do spawn sorteado, como um jogador sozinho em Verrückt.
            bool west = Rng.Next(2) == 0;
            Vector2Int spawn = west ? Map.SpawnWest : Map.SpawnEast;
            Player = Player.Create(this, Map.TileToWorld(spawn));
            Zones.Unlock(Map.ZoneAt(spawn), false);

            Nav.Update(Map.WorldToTile(Player.transform.position));
            Rounds = gameObject.AddComponent<RoundManager>();
            PowerUps = gameObject.AddComponent<PowerUpManager>();
            Hud = Hud.Create(this);
            gameObject.AddComponent<Hints>();

            Points = Balance.StartingPoints;
            StartTime = Time.time;
            SanDebug_Expose();
        }

        /// <summary>Câmera ortográfica com Pixel Perfect: 384x216 pixels de jogo, 16 px por tile.</summary>
        public static Camera EnsureCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }

            cam.orthographic = true;
            cam.orthographicSize = 216f / 16f / 2f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(12, 10, 16, 255);
            cam.transform.position = new Vector3(0, 0, -10);

            var ppc = cam.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
            if (ppc == null) ppc = cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
            ppc.assetsPPU = 16;
            ppc.refResolutionX = 384;
            ppc.refResolutionY = 216;
            ppc.gridSnapping = UnityEngine.Rendering.Universal.PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            ppc.cropFrame = UnityEngine.Rendering.Universal.PixelPerfectCamera.CropFrame.None;

            if (cam.GetComponent<CameraRig>() == null) cam.gameObject.AddComponent<CameraRig>();
            return cam;
        }

        private void Start()
        {
            GameEvents.RaisePoints(Points, 0);
            AudioManager.Music("MusicAmbient", 0.55f);
            AudioManager.Ambience("AmbienceAsylum");
            GameEvents.RaiseToast(Zones.SideName(Map.ZoneAt(Map.WorldToTile(Player.transform.position))));
        }

        private float navTimer;

        private void Update()
        {
            // campo de fluxo segue o jogador (só recalcula se ele mudou de tile ou uma porta abriu)
            navTimer -= Time.deltaTime;
            if (navTimer <= 0f && Player != null)
            {
                navTimer = 0.2f;
                Nav.Update(Map.WorldToTile(Player.transform.position));
            }
        }

        // ------------------------------------------------------------ pontos
        public void AddPoints(int amount)
        {
            if (amount <= 0) return;
            if (PowerUps != null && PowerUps.DoublePoints) amount *= 2;
            Points += amount;
            TotalPointsEarned += amount;
            GameEvents.RaisePoints(Points, amount);
        }

        /// <summary>Devolução (ursinho da caixa): não dobra com Pontos em Dobro.</summary>
        public void AddPointsRefund(int amount)
        {
            Points += amount;
            GameEvents.RaisePoints(Points, amount);
        }

        public bool TrySpend(int cost)
        {
            if (cost > Points)
            {
                AudioManager.Play("Deny", 0.8f);
                return false;
            }

            Points -= cost;
            GameEvents.RaisePoints(Points, -cost);
            if (cost > 0) AudioManager.Play("Buy", 0.9f);
            return true;
        }

        // ------------------------------------------------------------ energia
        public void TurnOnPower()
        {
            if (Power) return;
            Power = true;
            AudioManager.Sting("PowerOn");
            CameraRig.Shake(0.5f);
            GameEvents.RaisePowerOn();
            GameEvents.RaiseToast("ENERGIA LIGADA");
        }

        // ------------------------------------------------------------ estados
        public void SetPaused(bool paused)
        {
            if (State == GameState.GameOver || State == GameState.Reading) return;
            State = paused ? GameState.Paused : GameState.Playing;
            Time.timeScale = paused ? 0f : 1f;
            AudioManager.MusicVolume(paused ? 0.2f : 0.55f);
            Hud.ShowPause(paused);
        }

        public void BeginReading()
        {
            if (State != GameState.Playing) return;
            State = GameState.Reading;
            Time.timeScale = 0f;
        }

        public void EndReading()
        {
            if (State != GameState.Reading) return;
            State = GameState.Playing;
            Time.timeScale = 1f;
        }

        public void GameOver()
        {
            if (State == GameState.GameOver) return;
            State = GameState.GameOver;
            int round = Rounds != null ? Rounds.Round : 0;
            bool record = SaveData.RecordGame(Difficulty, round, Kills, TotalPointsEarned);
            AudioManager.StopMusic();
            AudioManager.Sting("GameOver");
            StartCoroutine(GameOverRoutine(record));
        }

        private IEnumerator GameOverRoutine(bool record)
        {
            // câmera lenta antes do painel: dá tempo de ver o que aconteceu
            float t = 0f;
            while (t < 1.4f)
            {
                t += Time.unscaledDeltaTime;
                Time.timeScale = Mathf.Lerp(1f, 0.15f, t / 1.4f);
                yield return null;
            }

            Time.timeScale = 0f;
            Hud.ShowGameOver(record);
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            GameEvents.Clear();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void QuitToMenu()
        {
            Time.timeScale = 1f;
            GameEvents.Clear();
            SceneManager.LoadScene("MainMenu");
        }

        private void OnApplicationFocus(bool focus)
        {
            // No navegador, trocar de aba não pode matar o jogador.
            if (!focus && State == GameState.Playing && Time.time - StartTime > 1f)
            {
                SetPaused(true);
            }
        }

        // ------------------------------------------------------------ depuração
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void SanDebug_Expose();
#else
        private static void SanDebug_Expose() { }
#endif

        public bool God { get; private set; }

        /// <summary>
        /// Comandos de teste (console do navegador: sanatorio('points 5000')).
        /// points N | power | round N | tp X Y | zone Z | god | give ID | kill | dogs | panzer | pack
        /// </summary>
        public void DebugCommand(string cmd)
        {
            string[] a = (cmd ?? "").Trim().Split(' ');
            switch (a[0])
            {
                case "points": AddPointsRefund(a.Length > 1 ? int.Parse(a[1]) : 10000); break;
                case "power": TurnOnPower(); break;
                case "round": Rounds.DebugSetRound(a.Length > 1 ? int.Parse(a[1]) : 5); break;
                case "tp": Player.transform.position = Map.TileToWorld(new Vector2Int(int.Parse(a[1]), int.Parse(a[2]))); break;
                case "zone": Player.transform.position = Map.TileToWorld(Map.ZoneSeeds[int.Parse(a[1])]); break;
                case "god": God = !God; break;
                case "give": Player.Inventory.Give(WeaponCatalog.Get(a[1])); break;
                case "kill": Rounds.KillAll(true); break;
                case "dogs": Rounds.DebugDogRound(); break;
                case "panzer": Rounds.DebugSpawnPanzer(); break;
                case "pack": if (Player.Inventory.Current != null) { Player.Inventory.Current.Upgrade(); Player.Inventory.NotifyChanged(); } break;
            }

            Debug.Log("[Debug] " + cmd + $" -> pontos {Points}, rodada {Rounds.Round}, energia {Power}, god {God}, " +
                      $"zona {Map.ZoneAt(Map.WorldToTile(Player.transform.position))}, vivos {Rounds.Alive.Count}");
        }

        public float Accuracy => ShotsFired == 0 ? 0f : (float)ShotsHit / ShotsFired;
        public float Elapsed => Time.time - StartTime;
    }
}
