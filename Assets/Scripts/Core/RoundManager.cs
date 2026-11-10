using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sanatorio
{
    /// <summary>
    /// Rodadas sem fim no estilo World at War: cada rodada tem N zumbis
    /// (no máximo 24 vivos), a vida deles cresce por rodada e cada vez mais
    /// deles correm. Entre rodadas há um respiro de 10 s.
    ///
    /// Rodadas especiais:
    /// - cães: a primeira cai na rodada 6 ou 7 e as próximas a cada 7 ou 8
    ///   rodadas. Os cães nascem em raios perto do jogador; o último sempre
    ///   deixa uma Munição Máxima;
    /// - Panzer: aparece no meio da rodada 20 e volta a cada 4 rodadas
    ///   (empurrado para a seguinte se cair numa rodada de cães).
    /// </summary>
    public class RoundManager : MonoBehaviour
    {
        public int Round { get; private set; }
        public int ToSpawn { get; private set; }
        public int Spawned { get; private set; }
        public int KilledThisRound { get; private set; }
        public bool Intermission { get; private set; } = true;
        public bool IsDogRound { get; private set; }
        public int NextDogRound => nextDogRound;
        public readonly List<Zombie> Alive = new List<Zombie>();

        private Game game;
        private float spawnTimer;
        private int nextDogRound;
        private int dogRoundsDone;
        private int nextPanzerRound = Balance.PanzerFirstRound;
        private bool panzerPending;

        public int Remaining => ToSpawn - KilledThisRound;

        private void Start()
        {
            game = Game.I;
            nextDogRound = game.Rng.Next(Balance.DogFirstRoundMin, Balance.DogFirstRoundMax + 1);
            StartCoroutine(NextRound(3.5f));
        }

        private IEnumerator NextRound(float delay)
        {
            Intermission = true;
            yield return new WaitForSeconds(delay);
            Round++;
            Spawned = 0;
            KilledThisRound = 0;
            game.PlankPointsThisRound = 0;
            spawnTimer = 1.2f;
            panzerPending = false;

            IsDogRound = Round == nextDogRound;
            if (IsDogRound)
            {
                ToSpawn = Balance.DogCount(dogRoundsDone);
                nextDogRound = Round + game.Rng.Next(Balance.DogIntervalMin, Balance.DogIntervalMax + 1);
                spawnTimer = 3f;
                SetDogAtmosphere(true);
                AudioManager.Sting("DogRound");
                GameEvents.RaiseToast("RODADA DOS CÃES");
            }
            else
            {
                ToSpawn = Balance.ZombieCount(Round, game.Difficulty);
                if (Round >= nextPanzerRound)
                {
                    // o Panzer entra no meio da rodada e conta como um inimigo dela
                    panzerPending = true;
                    ToSpawn++;
                    nextPanzerRound = Round + Balance.PanzerEvery;
                }

                AudioManager.Sting(Round == 1 ? "RoundFirst" : "RoundStart");
            }

            Intermission = false;
            GameEvents.RaiseRoundStarted(Round);
        }

        private void Update()
        {
            if (game == null || !game.IsPlaying || Intermission) return;

            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f || Spawned >= ToSpawn || Alive.Count >= Balance.MaxAlive) return;

            if (IsDogRound)
            {
                spawnTimer = Balance.DogSpawnDelay(dogRoundsDone);
                if (SpecialSpawnPoint(6, 14, out Vector2 p))
                {
                    Alive.Add(Zombie.SpawnDog(game, p, Balance.DogHealth(Round)));
                    Spawned++;
                }

                return;
            }

            spawnTimer = Balance.SpawnDelay(Round);
            if (panzerPending && Spawned >= (ToSpawn - 1) / 3)
            {
                if (SpawnPanzer())
                {
                    panzerPending = false;
                    Spawned++;
                }

                return;
            }

            if (TrySpawn())
            {
                Spawned++;
            }
        }

        private bool TrySpawn()
        {
            MapData.Window w = PickWindow();
            if (w == null) return false;

            float hp = Balance.ZombieHealth(Round);
            bool runner = game.Rng.NextDouble() < Balance.RunnerChance(Round, game.Difficulty);
            Zombie z = Zombie.Spawn(game, w, Mathf.RoundToInt(hp), runner);
            Alive.Add(z);
            return true;
        }

        private bool SpawnPanzer()
        {
            if (!SpecialSpawnPoint(7, 15, out Vector2 p)) return false;
            Alive.Add(Zombie.SpawnPanzer(game, p, Balance.PanzerHealth(Round)));
            GameEvents.RaiseToast("PANZER!");
            AudioManager.Sting("PanzerRoar");
            return true;
        }

        /// <summary>
        /// Escolhe uma janela ativa entre as mais próximas do jogador (pela
        /// distância de caminhada), para a ação acontecer perto dele.
        /// </summary>
        public MapData.Window PickWindow()
        {
            List<MapData.Window> active = game.Zones.ActiveWindows();
            if (active.Count == 0) return null;

            active.Sort((a, b) => game.Nav.DistanceAt(a.inside).CompareTo(game.Nav.DistanceAt(b.inside)));
            int pool = Mathf.Min(active.Count, 4);
            return active[game.Rng.Next(pool)];
        }

        private readonly List<Vector2Int> candidates = new List<Vector2Int>();

        /// <summary>
        /// Piso livre numa zona aberta, a uma distância de caminhada entre
        /// min e max tiles do jogador (perto, mas nunca em cima dele).
        /// </summary>
        public bool SpecialSpawnPoint(int min, int max, out Vector2 pos)
        {
            candidates.Clear();
            MapData map = game.Map;
            Vector2Int fallback = new Vector2Int(-1, -1);
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    Vector2Int t = new Vector2Int(x, y);
                    if (!game.Nav.IsWalkable(t) || !game.Zones.IsUnlocked(map.ZoneAt(t))) continue;
                    int d = game.Nav.DistanceAt(t);
                    if (d == NavGrid.Unreachable) continue;
                    if (d >= min && d <= max) candidates.Add(t);
                    else if (d > max && (fallback.x < 0 || d < game.Nav.DistanceAt(fallback))) fallback = t;
                }
            }

            if (candidates.Count > 0)
            {
                pos = map.TileToWorld(candidates[game.Rng.Next(candidates.Count)]);
                return true;
            }

            pos = fallback.x >= 0 ? map.TileToWorld(fallback) : Vector2.zero;
            return fallback.x >= 0;
        }

        public void OnZombieDied(Zombie z)
        {
            Alive.Remove(z);
            KilledThisRound++;
            game.Kills++;
            bool last = !Intermission && KilledThisRound >= ToSpawn && Spawned >= ToSpawn;

            // recompensa da rodada dos cães: o último sempre deixa Munição Máxima
            if (last && IsDogRound) game.PowerUps.DropMaxAmmo(z.transform.position);
            if (last) EndRound();
        }

        private void EndRound()
        {
            Intermission = true;
            if (IsDogRound)
            {
                dogRoundsDone++;
                SetDogAtmosphere(false);
            }

            AudioManager.Sting("RoundEnd");
            GameEvents.RaiseRoundEnded(Round);
            game.Player.Inventory.OnRoundEnd();
            StartCoroutine(NextRound(Balance.BetweenRounds));
        }

        /// <summary>Céu avermelhado durante a rodada dos cães.</summary>
        private void SetDogAtmosphere(bool on)
        {
            UnityEngine.Rendering.Universal.Light2D g = game.World.Global;
            if (g == null) return;
            if (on)
            {
                g.color = new Color(1f, 0.45f, 0.4f);
                g.intensity = game.Power ? 0.32f : 0.12f;
            }
            else
            {
                g.color = game.Power ? new Color(1f, 0.92f, 0.8f) : new Color(0.5f, 0.55f, 0.85f);
                g.intensity = game.Power ? 0.42f : 0.09f;
            }
        }

        /// <summary>Inimigo preso há muito tempo volta a nascer (zumbi na janela; cão e Panzer perto do jogador).</summary>
        public void Respawn(Zombie z)
        {
            if (z.Kind != EnemyKind.Zombie)
            {
                if (SpecialSpawnPoint(6, 14, out Vector2 p)) z.Relocate(p);
                return;
            }

            MapData.Window w = PickWindow();
            if (w != null) z.Restart(w);
        }

        public void DebugSetRound(int r)
        {
            StopAllCoroutines();
            Intermission = true;
            KillAll(false);
            StopAllCoroutines();
            if (IsDogRound) SetDogAtmosphere(false);
            Round = Mathf.Max(0, r - 1);
            StartCoroutine(NextRound(0.5f));
        }

        public void DebugDogRound()
        {
            nextDogRound = Round + 1;
            DebugSetRound(Round + 1);
        }

        public void DebugSpawnPanzer()
        {
            if (Intermission) return;
            if (SpawnPanzer())
            {
                ToSpawn++;
                Spawned++;
            }
        }

        public void KillAll(bool countPoints)
        {
            foreach (Zombie z in Alive.ToArray())
            {
                z.Kill(DamageSource.Nuke, Vector2.zero, countPoints);
            }
        }
    }
}
