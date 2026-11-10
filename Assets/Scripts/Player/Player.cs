using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Sanatorio
{
    /// <summary>
    /// O soldado: anda, mira em 360°, atira (Gunner), esfaqueia, joga granada,
    /// planta Betty, compra coisas e leva dano. Regras de vida de WaW:
    /// 2 golpes derrubam (5 com Ferro-Nog), vida regenera depois de um tempo,
    /// cair sem Revive-Já é fim de jogo; cair perde os refrigerantes.
    /// </summary>
    public class Player : MonoBehaviour
    {
        public PlayerInput Input { get; private set; }
        public Inventory Inventory { get; private set; }
        public Gunner Gun { get; private set; }
        public Vector2 Aim { get; private set; } = Vector2.right;
        public Vector2 AimPoint { get; private set; }
        public int Health { get; private set; }
        public int MaxHealth { get; private set; } = Balance.PlayerHealth;
        public bool Downed { get; private set; }
        public bool Dead { get; private set; }
        public Interactable Focus { get; private set; }
        public float HoldProgress { get; private set; }
        public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;
        public bool Grabbed { get; private set; }
        private Vector2 pull;

        /// <summary>Garra do Panzer: puxa o jogador (ele ainda anda, bem devagar).</summary>
        public void Grab(Vector2 pullVelocity, bool grabbed)
        {
            Grabbed = grabbed;
            pull = grabbed ? pullVelocity : Vector2.zero;
        }

        private Game game;
        private Rigidbody2D body;
        private Transform visual;
        private SpriteRenderer sprite;
        private SpriteRenderer slash;
        private Light2D flashlight;
        private Vector2 move;
        private float lastHurt = -99f;
        private float invulnerableUntil;
        private float knifeReady;
        private float walkPhase;
        private float footstep;
        private float regenAccum;

        public static Player Create(Game game, Vector2 pos)
        {
            GameObject go = new GameObject("Player");
            go.layer = Layers.Player;
            go.transform.position = pos;
            Player p = go.AddComponent<Player>();
            p.game = game;
            p.Build();
            return p;
        }

        private void Build()
        {
            body = gameObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.mass = 4f;
            body.linearDamping = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = Balance.PlayerRadius;

            GameObject v = new GameObject("Visual");
            v.transform.SetParent(transform, false);
            visual = v.transform;
            sprite = Res.AddSprite(v, "Player_0", Order.Player);

            GameObject s = new GameObject("Slash");
            s.transform.SetParent(visual, false);
            s.transform.localPosition = new Vector3(0.7f, 0f, 0f);
            slash = Res.AddSprite(s, "KnifeSlash", Order.Fx, false);
            slash.enabled = false;

            // luz ambiente em volta + lanterna em cone para onde ele mira
            LightKit.Point(transform, pos2, 3.2f, 0.45f, new Color(0.85f, 0.85f, 1f), 0.1f);
            flashlight = LightKit.Spot(visual, 10f, 0.95f, new Color(1f, 0.95f, 0.8f), 62f);
            flashlight.transform.localRotation = Quaternion.Euler(0, 0, -90f);

            Input = new PlayerInput();
            Input.Enable();
            Inventory = new Inventory();
            Inventory.Give(WeaponCatalog.Get("m1911"));
            Gun = gameObject.AddComponent<Gunner>();
            Gun.Init(this);
            Health = MaxHealth;

            CameraRig.I.target = transform;
            CameraRig.I.SnapTo(transform.position);
        }

        private Vector2 pos2 => transform.position;

        private void OnDestroy()
        {
            Input?.Disable();
        }

        private void Update()
        {
            Input.Poll();

            if (Input.Pause.WasPressedThisFrame())
            {
                if (game.State == GameState.Playing) game.SetPaused(true);
                else if (game.State == GameState.Paused) game.SetPaused(false);
            }

            if (!game.IsPlaying || Dead)
            {
                move = Vector2.zero;
                return;
            }

            UpdateAim();

            if (Downed)
            {
                move = Vector2.zero;
                return;
            }

            move = Vector2.ClampMagnitude(Input.Move.ReadValue<Vector2>(), 1f);
            UpdateInteraction();

            if (Input.Swap.WasPressedThisFrame()) Gun.SwapWeapon();
            if (Input.Slot1.WasPressedThisFrame()) Gun.SelectSlot(0);
            if (Input.Slot2.WasPressedThisFrame()) Gun.SelectSlot(1);
            if (Input.Reload.WasPressedThisFrame()) Gun.StartReload();
            if (Input.Knife.WasPressedThisFrame()) KnifeAttack();
            if (Input.Grenade.WasPressedThisFrame()) ThrowLethal();
            if (Input.Betty.WasPressedThisFrame()) PlaceBetty();

            WeaponState cur = Inventory.Current;
            bool fireHeld = Input.Fire.IsPressed();
            bool firePressed = Input.Fire.WasPressedThisFrame();
            if (cur != null && (cur.def.auto ? fireHeld : firePressed)) Gun.TryFire();

            Regen();
            Animate();
        }

        private void FixedUpdate()
        {
            if (body.bodyType != RigidbodyType2D.Dynamic) return;
            float mult = Inventory.Current != null ? Inventory.Current.def.moveMultiplier : 1f;
            body.linearVelocity = move * Balance.PlayerSpeed * mult * (Grabbed ? 0.25f : 1f) + pull;
        }

        // ------------------------------------------------------------ mira
        private void UpdateAim()
        {
            Vector2 pos = transform.position;
            Vector2 stick = Input.AimStick.ReadValue<Vector2>();
            Camera cam = Camera.main;

            if (Input.UsingGamepad)
            {
                if (stick.sqrMagnitude > 0.08f)
                {
                    Aim = stick.normalized;
                }
                else if (move.sqrMagnitude > 0.05f)
                {
                    Aim = Vector2.Lerp(Aim, move.normalized, 0.2f).normalized;
                }

                if (SaveData.AimAssist) Aim = AssistAim(pos, Aim);
                AimPoint = pos + Aim * 6f;
            }
            else if (cam != null)
            {
                Vector2 screen = Input.Point.ReadValue<Vector2>();
                Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
                Vector2 d = (Vector2)world - pos;
                if (d.sqrMagnitude > 0.04f) Aim = d.normalized;
                AimPoint = world;
            }

            float angle = Mathf.Atan2(Aim.y, Aim.x) * Mathf.Rad2Deg;
            visual.rotation = Quaternion.Euler(0, 0, angle);

            if (CameraRig.I != null)
            {
                Vector2 ahead = Input.UsingGamepad ? Aim * 2.2f : Vector2.ClampMagnitude((AimPoint - pos) * 0.3f, 3.5f);
                CameraRig.I.lookAhead = Vector2.Lerp(CameraRig.I.lookAhead, ahead, 0.12f);
            }
        }

        /// <summary>No controle, puxa a mira levemente para o zumbi mais próximo do cone.</summary>
        private Vector2 AssistAim(Vector2 pos, Vector2 aim)
        {
            Zombie best = null;
            float bestScore = float.MaxValue;
            foreach (Zombie z in game.Rounds.Alive)
            {
                if (!z.Targetable) continue;
                Vector2 to = (Vector2)z.transform.position - pos;
                float dist = to.magnitude;
                if (dist > 10f || dist < 0.01f) continue;
                float ang = Vector2.Angle(aim, to);
                if (ang > 14f) continue;
                float score = ang * 0.5f + dist;
                if (score < bestScore && !Physics2D.Linecast(pos, z.transform.position, Layers.WallMask))
                {
                    bestScore = score;
                    best = z;
                }
            }

            if (best == null) return aim;
            Vector2 target = ((Vector2)best.transform.position - pos).normalized;
            return Vector2.Lerp(aim, target, 0.35f).normalized;
        }

        // ------------------------------------------------------------ interação
        private void UpdateInteraction()
        {
            Vector2 pos = transform.position;
            Interactable best = null;
            float bestD = float.MaxValue;
            foreach (Interactable it in Interactable.All)
            {
                if (it == null || !it.isActiveAndEnabled) continue;
                // perto do ponto de acesso OU encostado no próprio objeto (máquina, caixa, porta)
                float d = Mathf.Min(Vector2.Distance(pos, it.Point), Vector2.Distance(pos, it.transform.position) - 0.4f);
                if (d > it.Radius || d >= bestD) continue;
                if (it.Prompt(this, out _) == null) continue;
                best = it;
                bestD = d;
            }

            if (best != Focus)
            {
                Focus = best;
                HoldProgress = 0f;
            }

            if (Focus == null) return;

            if (Focus.Hold)
            {
                if (Input.Interact.IsPressed())
                {
                    Focus.HoldTick(this, Time.deltaTime);
                }
            }
            else if (Input.Interact.WasPressedThisFrame())
            {
                Focus.Use(this);
            }
        }

        // ------------------------------------------------------------ faca e equipamentos
        private void KnifeAttack()
        {
            if (Time.time < knifeReady) return;
            knifeReady = Time.time + Balance.KnifeCooldown;
            StartCoroutine(SlashFx());
            AudioManager.Play("KnifeSwing", 0.7f, 0.1f);

            Vector2 pos = transform.position;
            body.AddForce(Aim * 8f, ForceMode2D.Impulse);
            bool hit = false;
            foreach (Collider2D c in Physics2D.OverlapCircleAll(pos + Aim * 0.55f, Balance.KnifeRange, Layers.ZombieMask))
            {
                Zombie z = c.GetComponent<Zombie>();
                if (z == null || !z.Targetable) continue;
                Vector2 to = (Vector2)z.transform.position - pos;
                if (Vector2.Angle(Aim, to) > Balance.KnifeArc * 0.5f && to.magnitude > 0.6f) continue;
                int dmg = game.PowerUps.InstaKill ? 999999 : Balance.KnifeDamage;
                z.TakeDamage(dmg, Aim, DamageSource.Knife);
                hit = true;
            }

            if (hit)
            {
                AudioManager.Play("KnifeHit", 0.9f, 0.1f);
                CameraRig.Shake(0.12f);
            }
        }

        private IEnumerator SlashFx()
        {
            slash.enabled = true;
            yield return new WaitForSeconds(0.09f);
            slash.enabled = false;
        }

        private void ThrowLethal()
        {
            if (Inventory.Lethals <= 0)
            {
                AudioManager.Play("Deny", 0.5f);
                return;
            }

            Inventory.Lethals--;
            Inventory.NotifyChanged();
            Vector2 pos = transform.position;
            float dist = Input.UsingGamepad ? 5.5f : Mathf.Clamp(Vector2.Distance(pos, AimPoint), 1.5f, 7.5f);
            Throwable.Throw(pos + Aim * 0.4f, pos + Aim * dist, Inventory.LethalId == "molotov");
        }

        private void PlaceBetty()
        {
            if (!Inventory.HasBetties || Inventory.Betties <= 0)
            {
                AudioManager.Play("Deny", 0.5f);
                return;
            }

            Inventory.Betties--;
            Inventory.NotifyChanged();
            Betty.Place(transform.position);
        }

        // ------------------------------------------------------------ refrigerantes
        public bool HasPerk(PerkType p) => Inventory.Perks.Contains(p);

        public void AddPerk(PerkType p)
        {
            Inventory.Perks.Add(p);
            if (p == PerkType.FerroNog)
            {
                MaxHealth = Balance.PlayerHealthJugg;
                Health = MaxHealth;
            }

            if (p == PerkType.ReviveJa) Inventory.ReviveBuys++;
            Inventory.NotifyChanged();
            AudioManager.Play("Drink", 1f);
            GameEvents.RaiseToast(PerkInfo.Name(p).ToUpperInvariant() + ": " + PerkInfo.Effect(p).ToUpperInvariant());
        }

        private void LoseAllPerks()
        {
            Inventory.Perks.Clear();
            MaxHealth = Balance.PlayerHealth;
            Inventory.NotifyChanged();
        }

        // ------------------------------------------------------------ vida
        public bool Invulnerable => Time.time < invulnerableUntil || Downed || Dead || game.God;

        public void Damage(int amount, Vector2 from)
        {
            if (amount <= 0 || Invulnerable || !game.IsPlaying) return;

            Health -= amount;
            lastHurt = Time.time;
            invulnerableUntil = Time.time + Balance.HurtInvulnerability;
            Vector2 away = ((Vector2)transform.position - from).normalized;
            body.AddForce(away * 10f, ForceMode2D.Impulse);
            Fx.Blood(transform.position, away, 6);
            CameraRig.Shake(0.3f);
            AudioManager.Play("PlayerHurt", 1f, 0.08f);
            GameEvents.RaisePlayerHurt(Health);

            if (Health <= 0)
            {
                Health = 0;
                GoDown();
            }
        }

        private void Regen()
        {
            if (Health >= MaxHealth || Time.time - lastHurt < Balance.RegenDelay) return;
            regenAccum += Balance.RegenPerSecond * Time.deltaTime;
            int add = Mathf.FloorToInt(regenAccum);
            if (add > 0)
            {
                regenAccum -= add;
                Health = Mathf.Min(MaxHealth, Health + add);
                GameEvents.RaisePlayerHurt(Health);
            }
        }

        private void GoDown()
        {
            sprite.sprite = Res.Sprite("Player_Down");
            body.linearVelocity = Vector2.zero;
            Gun.CancelReload();
            AudioManager.Play("PlayerDown", 1f);

            if (HasPerk(PerkType.ReviveJa))
            {
                StartCoroutine(SelfRevive());
            }
            else
            {
                Dead = true;
                body.bodyType = RigidbodyType2D.Static;
                game.GameOver();
            }
        }

        private IEnumerator SelfRevive()
        {
            Downed = true;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.linearVelocity = Vector2.zero;
            GameEvents.RaiseToast("REVIVE-JÁ: LEVANTANDO...");
            LoseAllPerks();
            float t = 0f;
            while (t < Balance.SelfReviveTime)
            {
                t += Time.deltaTime;
                HoldProgress = t / Balance.SelfReviveTime;
                yield return null;
            }

            HoldProgress = 0f;
            Downed = false;
            body.bodyType = RigidbodyType2D.Dynamic;
            Health = MaxHealth;
            invulnerableUntil = Time.time + Balance.ReviveInvulnerability;
            GameEvents.RaisePlayerHurt(Health);
            sprite.sprite = Res.Sprite(BodySprite(0));
            AudioManager.Play("Revive", 1f);
            Fx.Burst(transform.position, new Color(0.5f, 0.7f, 1f), 30, 5f, 0.5f, 2f / 16f, true);

            // empurra quem estava em cima, para não cair de novo na hora
            foreach (Zombie z in game.Rounds.Alive)
            {
                Vector2 d = z.transform.position - transform.position;
                if (d.magnitude < 3f) z.Knockback(d.normalized * 6f, 0.6f);
            }
        }

        // ------------------------------------------------------------ animação
        /// <summary>Sprite do soldado segurando o tipo de arma atual (pistola, fuzil, escopeta...).</summary>
        private string BodySprite(int frame)
        {
            WeaponState w = Inventory != null ? Inventory.Current : null;
            return $"Player_{HeldKind(w != null ? w.def.cls : WeaponClass.Rifle)}_{frame}";
        }

        public static string HeldKind(WeaponClass c)
        {
            switch (c)
            {
                case WeaponClass.Pistol: return "pistol";
                case WeaponClass.Smg: return "smg";
                case WeaponClass.Shotgun: return "shotgun";
                case WeaponClass.Lmg: return "lmg";
                case WeaponClass.Launcher: return "launcher";
                case WeaponClass.Flame: return "flame";
                case WeaponClass.Ray: return "ray";
                default: return "rifle";     // fuzis e o PTRS-41
            }
        }

        private void Animate()
        {
            float speed = body.linearVelocity.magnitude;
            if (speed > 0.2f)
            {
                walkPhase += speed * Time.deltaTime;
                footstep -= speed * Time.deltaTime;
                if (footstep <= 0f)
                {
                    footstep = 1.1f;
                    AudioManager.Play("Step", 0.25f, 0.15f);
                }
            }

            bool hurtFlash = Time.time < invulnerableUntil && Time.time - lastHurt < 0.4f;
            sprite.sprite = Res.Sprite(BodySprite(Mathf.FloorToInt(walkPhase * 2.2f) % 2));
            sprite.color = hurtFlash ? new Color(1f, 0.4f, 0.4f) : Color.white;

            // a lanterna treme um pouco quando a vida está baixa
            if (flashlight != null)
            {
                float low = 1f - (float)Health / MaxHealth;
                flashlight.intensity = 0.95f - (low > 0.4f ? Random.value * 0.25f * low : 0f);
            }
        }
    }
}
