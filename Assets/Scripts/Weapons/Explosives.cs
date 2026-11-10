using System.Collections;
using UnityEngine;

namespace Sanatorio
{
    public static class Explosion
    {
        /// <summary>Dano em área com queda pela distância; paredes bloqueiam.</summary>
        public static void At(Vector2 pos, float radius, int damage, bool hurtsPlayer = true, string sound = "Explosion")
        {
            Fx.Explosion(pos, radius);
            AudioManager.PlayAt(sound, pos, 1f, 0.08f);
            Game game = Game.I;

            foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, radius, Layers.ZombieMask))
            {
                Zombie z = c.GetComponent<Zombie>();
                if (z == null || !z.Targetable) continue;
                Vector2 to = (Vector2)z.transform.position - pos;
                if (Physics2D.Linecast(pos, z.transform.position, Layers.WallMask)) continue;
                float fall = 1f - 0.6f * Mathf.Clamp01(to.magnitude / radius);
                int dmg = game.PowerUps.InstaKill ? 999999 : Mathf.RoundToInt(damage * fall);
                z.TakeDamage(dmg, to.sqrMagnitude > 0.001f ? to.normalized : Random.insideUnitCircle.normalized, DamageSource.Explosion);
            }

            // Explosivo próprio machuca (como em WaW), mas nunca derruba sozinho de vida cheia
            Player p = game.Player;
            if (hurtsPlayer && p != null)
            {
                float d = Vector2.Distance(p.transform.position, pos);
                if (d < radius * 0.8f && !Physics2D.Linecast(pos, p.transform.position, Layers.WallMask))
                {
                    p.Damage(Mathf.Min(45, p.Health - 1), pos);
                }
            }
        }
    }

    /// <summary>Foguete, granada de fuzil e raio: voam em linha e explodem no impacto.</summary>
    public class Projectile : MonoBehaviour
    {
        private WeaponDef def;
        private float mult = 1f;
        private Vector2 dir;
        private float life;
        private bool done;

        public static void Fire(WeaponDef def, Vector2 origin, Vector2 dir, float damageMult = 1f)
        {
            string sprite = def.cls == WeaponClass.Ray ? "RayBolt" : def.id == "panzerschreck" ? "Rocket" : "RifleGrenade";
            GameObject go = Res.Make("Projectile", null, origin);
            Projectile p = go.AddComponent<Projectile>();
            p.def = def;
            p.mult = damageMult;
            p.dir = dir;
            p.life = def.range / def.projectileSpeed + 0.5f;
            SpriteRenderer sr = Res.AddSprite(go, sprite, Order.Projectile, false);
            go.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            LightKit.Point(go.transform, origin, def.cls == WeaponClass.Ray ? 2.5f : 2f, 1f, def.tracer);
            if (def.cls == WeaponClass.Ray) sr.color = new Color(0.6f, 1f, 0.6f);
        }

        private void Update()
        {
            if (done) return;
            float step = def.projectileSpeed * Time.deltaTime;
            Vector2 pos = transform.position;
            RaycastHit2D hit = Physics2D.CircleCast(pos, 0.12f, dir, step, Layers.ShotMask);
            if (hit.collider != null)
            {
                Detonate(hit.point - dir * 0.05f, hit.collider.GetComponent<Zombie>());
                return;
            }

            transform.position = pos + dir * step;
            if (def.cls == WeaponClass.Ray)
            {
                Fx.Burst(pos, new Color(0.4f, 1f, 0.4f), 1, 0.5f, 0.25f, 2f / 16f, true);
            }
            else
            {
                Fx.Burst(pos - dir * 0.2f, new Color(0.6f, 0.6f, 0.6f), 1, 0.6f, 0.5f, 2f / 16f);
            }

            life -= Time.deltaTime;
            if (life <= 0f) Detonate(transform.position, null);
        }

        private void Detonate(Vector2 at, Zombie direct)
        {
            done = true;
            Game game = Game.I;
            if (direct != null && direct.Targetable)
            {
                bool crit = Balance.IsCrit(game.Rng.NextDouble());
                int dmg = game.PowerUps.InstaKill ? 999999 : Mathf.RoundToInt(def.damage * mult * (crit ? Balance.CritMultiplier : 1f));
                if (crit) Fx.Crit(at);
                direct.TakeDamage(dmg, dir, DamageSource.Bullet);
            }

            if (def.cls == WeaponClass.Ray)
            {
                Fx.Burst(at, new Color(0.4f, 1f, 0.4f), 16, 4f, 0.35f, 2f / 16f, true);
                Fx.Flash(at, new Color(0.4f, 1f, 0.4f), 3.5f, 1.5f, 0.2f);
                AudioManager.PlayAt("RayHit", at, 0.8f, 0.1f);
                foreach (Collider2D c in Physics2D.OverlapCircleAll(at, def.splashRadius, Layers.ZombieMask))
                {
                    Zombie z = c.GetComponent<Zombie>();
                    if (z != null && z != direct && z.Targetable)
                    {
                        z.TakeDamage(game.PowerUps.InstaKill ? 999999 : Mathf.RoundToInt(def.splashDamage * mult),
                            ((Vector2)z.transform.position - at).normalized, DamageSource.Explosion);
                    }
                }

                // o raio também respinga em quem atira de perto
                Player p = game.Player;
                if (Vector2.Distance(p.transform.position, at) < def.splashRadius * 0.7f) p.Damage(30, at);
            }
            else
            {
                Explosion.At(at, def.splashRadius, Mathf.RoundToInt(def.splashDamage * mult));
            }

            Destroy(gameObject);
        }
    }

    /// <summary>Granada de cabo (Stielhandgranate) ou molotov: arco no ar e efeito no chão.</summary>
    public class Throwable : MonoBehaviour
    {
        private Vector2 from, to;
        private bool molotov;
        private float t;
        private float flight;
        private SpriteRenderer sr;

        public static void Throw(Vector2 from, Vector2 to, bool molotov)
        {
            // não deixa o alvo atravessar parede: para no primeiro obstáculo
            RaycastHit2D wall = Physics2D.Linecast(from, to, Layers.WallMask);
            if (wall.collider != null) to = wall.point - (to - from).normalized * 0.3f;

            GameObject go = Res.Make(molotov ? "Molotov" : "Grenade", null, from);
            Throwable th = go.AddComponent<Throwable>();
            th.from = from;
            th.to = to;
            th.molotov = molotov;
            th.flight = Mathf.Clamp(Vector2.Distance(from, to) / 9f, 0.25f, 0.8f);
            th.sr = Res.AddSprite(go, molotov ? "Molotov" : "Grenade", Order.Projectile);
            AudioManager.Play("Throw", 0.7f, 0.1f);
        }

        private void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / flight);
            Vector2 p = Vector2.Lerp(from, to, k);
            float h = Mathf.Sin(k * Mathf.PI) * 0.6f;
            transform.position = p;
            sr.transform.localScale = Vector3.one * (1f + h);
            transform.Rotate(0, 0, 720f * Time.deltaTime);

            if (k < 1f) return;

            if (molotov)
            {
                StartCoroutine(Burn(to));
                enabled = false;
                sr.enabled = false;
            }
            else if (t >= Balance.GrenadeFuse)
            {
                Explosion.At(to, Balance.GrenadeRadius, Balance.GrenadeDamage);
                Destroy(gameObject);
            }
            else if (Mathf.Repeat(t, 0.4f) < Time.deltaTime)
            {
                AudioManager.PlayAt("Tick", to, 0.5f);
            }
        }

        private IEnumerator Burn(Vector2 at)
        {
            AudioManager.PlayAt("Molotov", at, 1f);
            Fx.Decal(at, "Scorch", 10f);
            float r = Balance.GrenadeRadius * 0.85f;
            float time = 0f;
            float tick = 0f;
            while (time < Balance.MolotovDuration)
            {
                time += Time.deltaTime;
                tick -= Time.deltaTime;
                Fx.Burst(at + Random.insideUnitCircle * r, new Color(1f, 0.5f, 0.1f), 2, 1.5f, 0.5f, 3f / 16f, true, Vector2.up, 60f);
                if (tick <= 0f)
                {
                    tick = 0.25f;
                    Fx.Flash(at, new Color(1f, 0.55f, 0.2f), r * 2.2f, 1f, 0.3f);
                    foreach (Collider2D c in Physics2D.OverlapCircleAll(at, r, Layers.ZombieMask))
                    {
                        Zombie z = c.GetComponent<Zombie>();
                        if (z != null && z.Targetable) z.TakeDamage(Balance.MolotovTickDamage, Vector2.zero, DamageSource.Fire);
                    }
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }

    /// <summary>Mina saltadora: arma em 1 s, pula quando um zumbi chega perto.</summary>
    public class Betty : MonoBehaviour
    {
        private float armTime;
        private bool triggered;
        private SpriteRenderer sr;

        public static void Place(Vector2 pos)
        {
            GameObject go = Res.Make("Betty", null, pos);
            Betty b = go.AddComponent<Betty>();
            b.armTime = Time.time + 1f;
            b.sr = Res.AddSprite(go, "Betty", Order.Decal + 1);
            AudioManager.Play("BettyPlace", 0.8f);
        }

        private void Update()
        {
            if (triggered || Time.time < armTime) return;
            // luzinha piscando: armada
            sr.color = Mathf.Repeat(Time.time, 1f) < 0.15f ? new Color(1f, 0.4f, 0.4f) : Color.white;

            if (Physics2D.OverlapCircle(transform.position, 1.1f, Layers.ZombieMask) != null)
            {
                triggered = true;
                StartCoroutine(Jump());
            }
        }

        private IEnumerator Jump()
        {
            AudioManager.PlayAt("BettyJump", transform.position, 1f);
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                sr.transform.localScale = Vector3.one * (1f + t * 2f);
                yield return null;
            }

            Explosion.At(transform.position, Balance.BettyRadius, Balance.BettyDamage, false);
            Destroy(gameObject);
        }
    }
}
