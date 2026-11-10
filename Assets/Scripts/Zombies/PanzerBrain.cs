using UnityEngine;

namespace Sanatorio
{
    /// <summary>
    /// Habilidades do Panzer (o soldado blindado que aparece a partir da rodada 20):
    /// - lança-chamas de curto alcance (fica parado queimando o jogador);
    /// - garra disparada de média distância: se pegar, puxa o jogador até ele.
    ///   Dá para se soltar causando dano suficiente no Panzer enquanto está preso;
    /// - o soco corpo a corpo é o ataque normal do Zombie (bem mais forte).
    /// </summary>
    public class PanzerBrain : MonoBehaviour
    {
        private enum Mode { Walk, Flame, ClawWindup, ClawFlying, Pulling }

        private Zombie self;
        private Game game;
        private Mode mode;
        private float modeTime;
        private float flameReady;
        private float clawReady;
        private float flameTick;
        private int damageWhileGrabbed;
        private Vector2 clawPos;
        private Vector2 clawDir;
        private LineRenderer tether;
        private SpriteRenderer clawSprite;

        public void Init(Zombie z)
        {
            self = z;
            game = Game.I;
            flameReady = Time.time + 3f;
            clawReady = Time.time + 5f;
            self.Damaged += OnDamaged;

            GameObject c = new GameObject("Claw");
            c.transform.SetParent(transform, false);
            clawSprite = Res.AddSprite(c, "Claw", Order.Projectile, false);
            clawSprite.enabled = false;

            tether = gameObject.AddComponent<LineRenderer>();
            tether.sharedMaterial = Res.Unlit;
            tether.positionCount = 2;
            tether.widthMultiplier = 2f / 16f;
            tether.startColor = tether.endColor = new Color(0.55f, 0.55f, 0.6f);
            tether.sortingOrder = Order.Projectile;
            tether.enabled = false;
        }

        private void OnDestroy()
        {
            Release();
        }

        private void OnDamaged(int dmg)
        {
            if (mode != Mode.Pulling) return;
            damageWhileGrabbed += dmg;
            if (damageWhileGrabbed >= Balance.PanzerClawBreakDamage)
            {
                Fx.Sparks(clawPos, Vector2.up, 14, new Color(1f, 0.4f, 0.3f));
                AudioManager.PlayAt("PlankBreak", clawPos, 1f);
                GameEvents.RaiseToast("VOCÊ SE SOLTOU!");
                EndAbility(Balance.PanzerClawCooldown);
            }
        }

        private void SetMode(Mode m)
        {
            mode = m;
            modeTime = 0f;
            self.Hold = m != Mode.Walk;
        }

        private void EndAbility(float cooldown)
        {
            if (mode == Mode.Pulling || mode == Mode.ClawFlying || mode == Mode.ClawWindup) clawReady = Time.time + cooldown;
            Release();
            SetMode(Mode.Walk);
        }

        private void Release()
        {
            if (game != null && game.Player != null) game.Player.Grab(Vector2.zero, false);
            if (tether != null) tether.enabled = false;
            if (clawSprite != null) clawSprite.enabled = false;
        }

        private void Update()
        {
            if (self == null || !self.Targetable || !game.IsPlaying) return;
            modeTime += Time.deltaTime;
            Player p = game.Player;
            Vector2 me = transform.position;
            Vector2 pp = p.transform.position;
            Vector2 to = pp - me;
            float dist = to.magnitude;
            bool sight = !Physics2D.Linecast(me, pp, Layers.WallMask);

            switch (mode)
            {
                case Mode.Walk:
                    if (sight && dist < Balance.PanzerFlameRange && Time.time >= flameReady)
                    {
                        SetMode(Mode.Flame);
                        AudioManager.PlayAt("PanzerRoar", me, 0.8f);
                    }
                    else if (sight && dist > 4f && dist < Balance.PanzerClawRange && Time.time >= clawReady && !p.Downed)
                    {
                        SetMode(Mode.ClawWindup);
                    }

                    break;

                case Mode.Flame:
                    FlameTick(me, to);
                    if (modeTime > Balance.PanzerFlameTime)
                    {
                        flameReady = Time.time + Balance.PanzerFlameCooldown;
                        SetMode(Mode.Walk);
                    }

                    break;

                case Mode.ClawWindup:
                    // luz vermelha no braço avisa que a garra vai sair
                    if (Mathf.Repeat(modeTime, 0.15f) < Time.deltaTime) Fx.Sparks(me + to.normalized * 0.4f, to, 2, new Color(1f, 0.2f, 0.2f));
                    if (modeTime > 0.6f)
                    {
                        clawPos = me;
                        clawDir = to.normalized;
                        clawSprite.enabled = true;
                        tether.enabled = true;
                        AudioManager.PlayAt("Claw", me, 1f);
                        SetMode(Mode.ClawFlying);
                    }

                    break;

                case Mode.ClawFlying:
                {
                    float step = 13f * Time.deltaTime;
                    RaycastHit2D wall = Physics2D.Raycast(clawPos, clawDir, step, Layers.WallMask);
                    clawPos += clawDir * step;
                    DrawClaw(me);
                    if (Vector2.Distance(clawPos, pp) < 0.6f && !p.Invulnerable)
                    {
                        damageWhileGrabbed = 0;
                        AudioManager.Play("PlayerHurt", 0.8f);
                        GameEvents.RaiseToast("PRESO NA GARRA! ATIRE NO PANZER");
                        SetMode(Mode.Pulling);
                    }
                    else if (wall.collider != null || Vector2.Distance(clawPos, me) > Balance.PanzerClawRange + 1f)
                    {
                        EndAbility(Balance.PanzerClawCooldown * 0.5f);
                    }

                    break;
                }

                case Mode.Pulling:
                    clawPos = pp;
                    DrawClaw(me);
                    p.Grab(-to.normalized * Balance.PanzerClawPull, true);
                    if (dist < 1.6f || modeTime > 4f || p.Downed)
                    {
                        // trouxe o jogador: emenda no lança-chamas
                        Release();
                        clawReady = Time.time + Balance.PanzerClawCooldown;
                        flameReady = 0f;
                        SetMode(Mode.Walk);
                    }

                    break;
            }
        }

        private void DrawClaw(Vector2 me)
        {
            tether.SetPosition(0, me);
            tether.SetPosition(1, clawPos);
            clawSprite.transform.position = clawPos;
            clawSprite.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(clawDir.y, clawDir.x) * Mathf.Rad2Deg);
        }

        private void FlameTick(Vector2 me, Vector2 to)
        {
            Vector2 dir = to.normalized;
            Fx.Burst(me + dir * 0.5f, new Color(1f, 0.5f, 0.12f), 4, 8f, 0.5f, 4f / 16f, true, dir, 40f);
            Fx.Burst(me + dir * 0.5f, new Color(1f, 0.85f, 0.3f), 2, 6f, 0.35f, 3f / 16f, true, dir, 25f);
            flameTick -= Time.deltaTime;
            if (flameTick > 0f) return;
            flameTick = 0.25f;
            Fx.Flash(me + dir * 1.5f, new Color(1f, 0.55f, 0.2f), 4f, 1f, 0.25f);
            AudioManager.PlayAt("Flame", me, 0.6f, 0.1f);
            if (to.magnitude < Balance.PanzerFlameRange + 0.5f && Vector2.Angle(transform.right, dir) < 360f)
            {
                game.Player.Damage(Balance.PanzerFlameDamage, me);
            }
        }
    }
}
