using UnityEngine;

namespace Sanatorio
{
    public enum DamageSource { Bullet, BulletNoPoints, Knife, Explosion, Fire, Trap, Nuke }

    public enum EnemyKind { Zombie, Dog, Panzer }

    /// <summary>
    /// Zumbi de Verrückt: nasce no pátio, vai até uma janela, arranca as tábuas
    /// (e bate em quem estiver colado do lado de dentro), entra e persegue pelo
    /// campo de fluxo. O ataque é telegrafado (braços sobem) e dá para esquivar.
    /// Explosão que não mata pode virar o zumbi em rastejante.
    /// </summary>
    public class Zombie : MonoBehaviour
    {
        private enum State { Rising, ToWindow, AtWindow, Entering, Chase, Windup, Recover, Dead }

        public bool Targetable => state != State.Dead && state != State.Rising;
        public bool IsCrawler { get; private set; }
        public EnemyKind Kind { get; private set; } = EnemyKind.Zombie;
        public int Hp => hp;

        /// <summary>Habilidade especial em curso (Panzer): fica parado e não ataca corpo a corpo.</summary>
        public bool Hold { get; set; }

        /// <summary>Dano recebido (o Panzer usa para soltar a garra).</summary>
        public event System.Action<int> Damaged;

        private Game game;
        private Rigidbody2D body;
        private CircleCollider2D col;
        private SpriteRenderer sr;
        private int variant;
        private int hp;
        private int maxHp;
        private bool runner;
        private float speed;
        private State state;
        private float stateTime;
        private MapData.Window window;
        private Barricade barricade;
        private float tearTimer;
        private float attackCooldown;
        private float walkPhase;
        private float hitFlash;
        private float groanTimer;
        private float lastFirePoints;
        private Vector2 knock;
        private float knockTime;
        private float progressTimer;
        private int bestDist;
        private float unreachableTimer;
        private Vector2 facing = Vector2.down;

        public static Zombie Spawn(Game game, MapData.Window w, int hp, bool runner)
        {
            GameObject go = new GameObject("Zombie");
            go.layer = Layers.Zombie;
            Zombie z = go.AddComponent<Zombie>();
            z.game = game;
            z.variant = game.Rng.Next(3);
            z.Build();
            z.maxHp = hp;
            z.runner = runner;
            z.Restart(w);
            return z;
        }

        /// <summary>Cão infernal da rodada especial: nasce num raio perto do jogador.</summary>
        public static Zombie SpawnDog(Game game, Vector2 pos, int hp)
        {
            GameObject go = new GameObject("Dog");
            go.layer = Layers.Zombie;
            Zombie z = go.AddComponent<Zombie>();
            z.game = game;
            z.Kind = EnemyKind.Dog;
            z.Build();
            z.col.radius = 0.28f;
            z.maxHp = hp;
            z.runner = true;
            z.Relocate(pos);
            return z;
        }

        /// <summary>Panzer: chega voando de mochila-foguete perto do jogador.</summary>
        public static Zombie SpawnPanzer(Game game, Vector2 pos, int hp)
        {
            GameObject go = new GameObject("Panzer");
            go.layer = Layers.Zombie;
            Zombie z = go.AddComponent<Zombie>();
            z.game = game;
            z.Kind = EnemyKind.Panzer;
            z.Build();
            z.col.radius = 0.45f;
            z.body.mass = 20f;
            z.maxHp = hp;
            z.Relocate(pos);
            go.AddComponent<PanzerBrain>().Init(z);
            return z;
        }

        /// <summary>Cão / Panzer (re)aparecem num ponto do mapa, sem passar por janela.</summary>
        public void Relocate(Vector2 pos)
        {
            window = null;
            barricade = null;
            hp = maxHp;
            Hold = false;
            speed = (Kind == EnemyKind.Dog ? Balance.DogSpeed : Balance.PanzerSpeed) *
                    Balance.ZombieSpeedMultiplier(game.Difficulty) * Random.Range(0.95f, 1.05f);
            transform.position = pos;
            body.linearVelocity = Vector2.zero;
            col.enabled = false;
            SetState(State.Rising);
            progressTimer = 0f;
            bestDist = NavGrid.Unreachable;
            unreachableTimer = 0f;
            groanTimer = Random.Range(0.5f, 2f);
            if (Kind == EnemyKind.Dog)
            {
                Fx.Lightning(pos);
            }
            else
            {
                AudioManager.PlayAt("PanzerLand", pos, 1f, 0f);
            }
        }

        private float RiseTime => Kind == EnemyKind.Panzer ? 1.6f : Kind == EnemyKind.Dog ? 0.6f : 0.8f;

        private void Build()
        {
            body = gameObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.mass = 1f;
            body.linearDamping = 4f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.33f;

            GameObject v = new GameObject("Visual");
            v.transform.SetParent(transform, false);
            sr = Res.AddSprite(v, Kind == EnemyKind.Dog ? "Dog_0" : Kind == EnemyKind.Panzer ? "Panzer_0" : $"Zombie{variant}_0", Order.Zombie);

            // olhos que brilham no escuro (o Panzer tem um farol na testa)
            UnityEngine.Rendering.Universal.Light2D eyes = Kind == EnemyKind.Panzer
                ? LightKit.Point(v.transform, transform.position, 2.4f, 0.9f, new Color(1f, 0.75f, 0.3f), 0.1f)
                : LightKit.Point(v.transform, transform.position, 0.6f, 0.5f,
                    Kind == EnemyKind.Dog ? new Color(1f, 0.35f, 0.2f) : new Color(1f, 0.85f, 0.3f), 0.1f);
            eyes.transform.localPosition = new Vector3(Kind == EnemyKind.Panzer ? 0.45f : 0.25f, 0f, 0f);
        }

        /// <summary>Recomeça do pátio, numa janela (também usado pelo anti-travamento).</summary>
        public void Restart(MapData.Window w)
        {
            window = w;
            barricade = game.World.BarricadeAt(w.tile);
            hp = maxHp;
            IsCrawler = false;
            speed = (runner ? Balance.ZombieRunSpeed : Balance.ZombieWalkSpeed) *
                    Balance.ZombieSpeedMultiplier(game.Difficulty) * Random.Range(0.9f, 1.1f);
            transform.position = game.Map.TileToWorld(w.spawn) + Random.insideUnitCircle * 0.3f;
            body.linearVelocity = Vector2.zero;
            col.enabled = false;
            SetState(State.Rising);
            progressTimer = 0f;
            bestDist = NavGrid.Unreachable;
            unreachableTimer = 0f;
            groanTimer = Random.Range(1f, 5f);
            Fx.Dust(transform.position, 8, new Color(0.35f, 0.3f, 0.22f));
        }

        private void SetState(State s)
        {
            state = s;
            stateTime = 0f;
        }

        private void Update()
        {
            if (state == State.Dead || !game.IsPlaying) return;
            stateTime += Time.deltaTime;
            attackCooldown -= Time.deltaTime;
            if (hitFlash > 0f) hitFlash -= Time.deltaTime;

            groanTimer -= Time.deltaTime;
            if (groanTimer <= 0f)
            {
                groanTimer = Random.Range(3f, 8f);
                string voice = Kind == EnemyKind.Dog ? "DogGrowl" : Kind == EnemyKind.Panzer ? "PanzerRoar" : runner ? "GroanRun" : "Groan" + Random.Range(0, 3);
                AudioManager.PlayAt(voice, transform.position, 0.55f, 0.12f);
            }

            switch (state)
            {
                case State.Rising:
                    float k = Mathf.Clamp01(stateTime / RiseTime);
                    // Panzer desce do céu (encolhendo até o chão, com chama da mochila); os outros "brotam"
                    sr.transform.localScale = Vector3.one * (Kind == EnemyKind.Panzer ? Mathf.Lerp(2.2f, 1f, k) : k);
                    if (Kind == EnemyKind.Panzer)
                    {
                        Fx.Burst(transform.position, new Color(1f, 0.6f, 0.2f), 2, 3f, 0.3f, 3f / 16f, true, Vector2.down, 60f);
                    }

                    if (stateTime > RiseTime)
                    {
                        sr.transform.localScale = Vector3.one;
                        col.enabled = true;
                        if (Kind == EnemyKind.Panzer)
                        {
                            Fx.Dust(transform.position, 30);
                            CameraRig.Shake(0.6f);
                            AudioManager.PlayAt("Explosion", transform.position, 0.6f);
                        }

                        SetState(Kind == EnemyKind.Zombie ? State.ToWindow : State.Chase);
                    }

                    break;
                case State.AtWindow:
                    UpdateAtWindow();
                    break;
                case State.Windup:
                    if (stateTime >= Windup) Strike();
                    break;
                case State.Recover:
                    if (stateTime >= 0.35f) SetState(State.Chase);
                    break;
            }

            CheckStuck();
            Animate();
        }

        private void FixedUpdate()
        {
            if (state == State.Dead) return;
            Vector2 desired = Vector2.zero;
            Vector2 pos = transform.position;

            switch (state)
            {
                case State.ToWindow:
                {
                    Vector2 outside = game.Map.TileToWorld(window.tile + window.outward);
                    desired = (outside - pos);
                    // com fila na janela, basta chegar perto (ou a janela já estar aberta)
                    bool open = barricade == null || barricade.Planks <= 0;
                    if (desired.magnitude < 0.55f || (open && desired.magnitude < 1.3f))
                    {
                        SetState(State.AtWindow);
                        desired = Vector2.zero;
                    }

                    break;
                }
                case State.Entering:
                {
                    Vector2 inside = game.Map.TileToWorld(window.inside);
                    desired = inside - pos;
                    gameObject.layer = Layers.Zombie;
                    if (desired.magnitude < 0.3f || stateTime > 2.5f)
                    {
                        SetState(State.Chase);
                    }

                    break;
                }
                case State.Chase:
                    desired = ChaseDirection(pos);
                    TryStartAttack(pos);
                    break;
            }

            if (Hold && state == State.Chase) desired = Vector2.zero;
            float spd = IsCrawler ? Balance.CrawlerSpeed : speed;
            Vector2 vel = desired.sqrMagnitude > 0.0001f ? desired.normalized * spd : Vector2.zero;
            if (knockTime > 0f)
            {
                knockTime -= Time.fixedDeltaTime;
                vel = knock;
                knock *= 0.85f;
            }

            body.linearVelocity = vel;
            if (Hold) desired = (Vector2)game.Player.transform.position - pos;   // parado, mas mirando
            if (desired.sqrMagnitude > 0.01f) facing = Vector2.Lerp(facing, desired.normalized, 0.25f).normalized;
        }

        private Vector2 ChaseDirection(Vector2 pos)
        {
            Player p = game.Player;
            Vector2 target = p.transform.position;
            Vector2 to = target - pos;

            // perto e com visão direta: vai reto (mais natural que seguir a grade)
            if (to.magnitude < 4.5f && !Physics2D.CircleCast(pos, 0.2f, to.normalized, to.magnitude, Layers.WallMask))
            {
                return to;
            }

            Vector2Int tile = game.Map.WorldToTile(pos);
            if (game.Nav.NextStep(tile, out Vector2Int step))
            {
                return game.Map.TileToWorld(step) - pos;
            }

            return Vector2.zero;
        }

        private float Windup => Kind == EnemyKind.Dog ? Balance.DogWindup : Kind == EnemyKind.Panzer ? Balance.PanzerWindup : Balance.ZombieWindup;
        private float AttackRange => Kind == EnemyKind.Panzer ? Balance.PanzerMeleeRange : Balance.ZombieAttackRange;

        private void TryStartAttack(Vector2 pos)
        {
            if (Hold || attackCooldown > 0f || game.Player.Invulnerable) return;
            float d = Vector2.Distance(pos, game.Player.transform.position);
            if (d < AttackRange)
            {
                SetState(State.Windup);
                body.linearVelocity = Vector2.zero;
                AudioManager.PlayAt(Kind == EnemyKind.Dog ? "DogBark" : Kind == EnemyKind.Panzer ? "PanzerRoar" : "ZombieAttack", pos, 0.6f, 0.1f);
            }
        }

        private void Strike()
        {
            attackCooldown = Balance.ZombieAttackCooldown;
            Vector2 pos = transform.position;
            Vector2 playerPos = game.Player.transform.position;
            bool inReach = barricadeAttack
                ? Vector2.Distance(playerPos, game.Map.TileToWorld(window.inside)) <= 1.15f
                : Vector2.Distance(pos, playerPos) <= AttackRange + 0.3f;
            if (inReach)
            {
                game.Player.Damage(Kind == EnemyKind.Panzer ? Balance.PanzerMeleeDamage : Balance.ZombieHitDamage, pos);
            }

            SetState(barricadeAttack ? State.AtWindow : State.Recover);
            barricadeAttack = false;
        }

        private bool barricadeAttack;

        private void UpdateAtWindow()
        {
            body.linearVelocity = Vector2.zero;
            Vector2 inside = game.Map.TileToWorld(window.inside);
            facing = (inside - (Vector2)transform.position).normalized;

            // Verrückt: bate através da barricada em quem estiver colado nela
            float pd = Vector2.Distance(game.Player.transform.position, inside);
            if (pd < 0.9f && attackCooldown <= 0f && !game.Player.Invulnerable)
            {
                barricadeAttack = true;
                SetState(State.Windup);
                AudioManager.PlayAt("ZombieAttack", transform.position, 0.6f, 0.1f);
                return;
            }

            if (barricade == null || barricade.Planks <= 0)
            {
                SetState(State.Entering);
                return;
            }

            tearTimer += Time.deltaTime;
            if (tearTimer >= Balance.PlankTearInterval)
            {
                tearTimer = 0f;
                barricade.Tear();
            }
        }

        // ------------------------------------------------------------ anti-travamento
        private void CheckStuck()
        {
            if (state == State.ToWindow && stateTime > 12f)
            {
                game.Rounds.Respawn(this);
                return;
            }

            if (state != State.Chase)
            {
                progressTimer = 0f;
                return;
            }

            int d = game.Nav.DistanceAt(game.Map.WorldToTile(transform.position));
            if (d == NavGrid.Unreachable)
            {
                unreachableTimer += Time.deltaTime;
                if (unreachableTimer > 4f) game.Rounds.Respawn(this);
                return;
            }

            unreachableTimer = 0f;
            if (d < bestDist || d <= 2)
            {
                bestDist = d;
                progressTimer = 0f;
                return;
            }

            progressTimer += Time.deltaTime;
            if (progressTimer > Balance.StuckTimeout)
            {
                // sem progresso: volta ao pátio (o jogador nunca fica esperando o último zumbi)
                bestDist = NavGrid.Unreachable;
                game.Rounds.Respawn(this);
            }
        }

        // ------------------------------------------------------------ dano
        public void Knockback(Vector2 v, float time)
        {
            knock = v;
            knockTime = time;
        }

        public void TakeDamage(int dmg, Vector2 dir, DamageSource src)
        {
            if (!Targetable) return;
            // armadura do Panzer: balas e fogo pegam pela metade, explosivo pega cheio
            if (Kind == EnemyKind.Panzer && src != DamageSource.Explosion && src != DamageSource.Nuke && dmg < 999999)
            {
                dmg = Mathf.RoundToInt(dmg * Balance.PanzerArmor);
                Fx.Sparks(transform.position, -dir, 3, new Color(0.9f, 0.9f, 1f));
            }

            hp -= dmg;
            Damaged?.Invoke(dmg);
            hitFlash = 0.08f;
            Vector2 pos = transform.position;

            if (src != DamageSource.Fire && src != DamageSource.Trap)
            {
                Fx.Blood(pos, dir, src == DamageSource.Explosion ? 14 : 6);
                if (dir != Vector2.zero) Knockback(dir * (src == DamageSource.Explosion ? 7f : 1.8f), 0.08f);
            }
            else if (src == DamageSource.Fire)
            {
                Fx.Burst(pos, new Color(1f, 0.5f, 0.1f), 1, 1f, 0.4f, 2f / 16f, true, Vector2.up, 50f);
            }

            if (hp <= 0)
            {
                Kill(src, dir, true);
                return;
            }

            if (src == DamageSource.Bullet || src == DamageSource.Knife)
            {
                game.AddPoints(Balance.PointsPerHit);
                AudioManager.PlayAt("Hit", pos, 0.45f, 0.15f);
            }
            else if (src == DamageSource.Fire && Time.time - lastFirePoints > 0.5f)
            {
                lastFirePoints = Time.time;
                game.AddPoints(Balance.PointsPerHit);
            }
            else if (src == DamageSource.Explosion)
            {
                game.AddPoints(Balance.PointsPerHit);
                // explosão que não mata pode arrancar as pernas
                if (Kind == EnemyKind.Zombie && !IsCrawler && hp < maxHp * 0.6f && game.Rng.NextDouble() < 0.4)
                {
                    IsCrawler = true;
                    col.radius = 0.28f;
                    AudioManager.PlayAt("GroanRun", pos, 0.6f);
                }
            }
        }

        public void Kill(DamageSource src, Vector2 dir, bool givePoints)
        {
            if (state == State.Dead) return;
            SetState(State.Dead);
            Vector2 pos = transform.position;

            if (givePoints && src != DamageSource.Trap && src != DamageSource.Nuke)
            {
                int pts = Kind == EnemyKind.Panzer ? Balance.PanzerKillPoints
                        : src == DamageSource.Knife ? Balance.PointsPerKnifeKill : Balance.PointsPerKill;
                game.AddPoints(pts);
            }

            if (Kind == EnemyKind.Panzer)
            {
                // o Panzer sempre deixa um power-up (do ciclo) ao cair
                game.PowerUps.ForceDrop(pos);
                Explosion.At(pos, 2f, 0, false, "Explosion");
            }
            else if (src != DamageSource.Nuke && src != DamageSource.Trap && Kind == EnemyKind.Zombie)
            {
                game.PowerUps.TryDrop(pos);
            }

            Fx.Blood(pos, dir == Vector2.zero ? Random.insideUnitCircle.normalized : dir, 16);
            Fx.Decal(pos, "Blood" + Random.Range(0, 3), 14f);
            float rot = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            string corpse = Kind == EnemyKind.Dog ? "DogCorpse" : Kind == EnemyKind.Panzer ? "PanzerCorpse"
                          : IsCrawler ? $"Crawler{variant}_0" : $"Corpse{variant}";
            Fx.Decal(pos, corpse, Kind == EnemyKind.Panzer ? 20f : 6f, rot,
                     src == DamageSource.Fire || src == DamageSource.Trap ? new Color(0.35f, 0.3f, 0.3f) : Color.white);
            string dieSound = Kind == EnemyKind.Dog ? "DogDie" : Kind == EnemyKind.Panzer ? "PanzerRoar"
                            : src == DamageSource.Nuke ? "Hit" : "ZombieDie" + Random.Range(0, 2);
            AudioManager.PlayAt(dieSound, pos, 0.8f, 0.12f);
            GameEvents.RaiseZombieKilled(pos, 0);
            game.Rounds.OnZombieDied(this);
            Destroy(gameObject);
        }

        // ------------------------------------------------------------ visual
        private void Animate()
        {
            float spd = body.linearVelocity.magnitude;
            walkPhase += spd * Time.deltaTime;
            string frame;
            if (Kind != EnemyKind.Zombie)
            {
                string baseName = Kind == EnemyKind.Dog ? "Dog" : "Panzer";
                frame = state == State.Windup || Hold ? baseName + "_Attack"
                      : Mathf.FloorToInt(walkPhase * (Kind == EnemyKind.Dog ? 3f : 1.6f)) % 2 == 0 ? baseName + "_0" : baseName + "_1";
            }
            else if (state == State.Windup || state == State.AtWindow && tearTimer > Balance.PlankTearInterval * 0.6f)
            {
                frame = IsCrawler ? $"Crawler{variant}_1" : $"Zombie{variant}_Attack";
            }
            else if (IsCrawler)
            {
                frame = Mathf.FloorToInt(walkPhase * 2f) % 2 == 0 ? $"Crawler{variant}_0" : $"Crawler{variant}_1";
            }
            else
            {
                frame = Mathf.FloorToInt(walkPhase * 2.4f) % 2 == 0 ? $"Zombie{variant}_0" : $"Zombie{variant}_1";
            }

            sr.sprite = Res.Sprite(frame);
            sr.color = hitFlash > 0f ? new Color(1f, 0.5f, 0.5f) : Color.white;
            sr.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg);
        }
    }
}
