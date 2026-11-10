using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sanatorio
{
    /// <summary>
    /// Disparo, recarga e troca de arma. Bala é raycast (com traçante e
    /// penetração), foguete/raio é projétil que explode, lança-chamas é um cone.
    /// Gatilho Duplo encurta o intervalo; Turbo-Cola corta a recarga pela metade.
    /// </summary>
    public class Gunner : MonoBehaviour
    {
        private Player player;
        private Game game;
        private float nextShot;
        private Coroutine reloading;
        private float flameSound;
        private float lastFire;

        public bool Reloading => reloading != null;
        public float ReloadProgress { get; private set; }

        private static readonly RaycastHit2D[] hits = new RaycastHit2D[24];
        private static readonly HashSet<Zombie> hitThisShot = new HashSet<Zombie>();
        private static readonly List<RaycastHit2D> sorted = new List<RaycastHit2D>();

        public void Init(Player p)
        {
            player = p;
            game = Game.I;
        }

        public void SwapWeapon()
        {
            if (player.Inventory.Weapons.Count < 2) return;
            CancelReload();
            player.Inventory.Swap();
            nextShot = Time.time + 0.25f;
            AudioManager.Play("Swap", 0.6f);
        }

        public void SelectSlot(int slot)
        {
            if (slot >= player.Inventory.Weapons.Count || slot == player.Inventory.CurrentIndex) return;
            CancelReload();
            player.Inventory.Select(slot);
            nextShot = Time.time + 0.25f;
            AudioManager.Play("Swap", 0.6f);
        }

        /// <summary>Dano de arma de fogo com chance de crítico (x2). Morte Instantânea ignora tudo.</summary>
        private int RollDamage(float baseDamage, out bool crit)
        {
            crit = Balance.IsCrit(game.Rng.NextDouble());
            if (game.PowerUps.InstaKill) return 999999;
            return Mathf.RoundToInt(baseDamage * (crit ? Balance.CritMultiplier : 1f));
        }

        public void CancelReload()
        {
            if (reloading != null)
            {
                StopCoroutine(reloading);
                reloading = null;
                ReloadProgress = 0f;
            }
        }

        private void Update()
        {
            // lança-chamas esfria quando o gatilho está solto; superaquecido, trava até zerar
            bool doubleTap = player != null && player.HasPerk(PerkType.GatilhoDuplo);
            foreach (WeaponState w in player.Inventory.Weapons)
            {
                if (!w.def.overheat) continue;
                bool firing = !w.overheated && Time.time - lastFire < Balance.FlameCoolDelay && w == player.Inventory.Current;
                if (firing)
                {
                    // aquece pelo tempo de gatilho apertado (não por quadro: igual em PC lento e rápido)
                    w.heat = Mathf.Min(1f, w.heat + Time.deltaTime / w.HeatTime);
                    if (w.heat >= 1f)
                    {
                        w.overheated = true;
                        AudioManager.Play("Overheat", 0.9f);
                        Fx.Burst(player.transform.position, new Color(0.85f, 0.85f, 0.85f), 18, 2.5f, 0.9f, 3f / 16f, false, Vector2.up, 120f);
                        player.Inventory.NotifyChanged();
                    }

                    continue;
                }

                if (w.heat <= 0f) continue;
                float time = w.overheated ? w.LockTime : Balance.FlameCoolTime;
                if (doubleTap) time *= 0.5f;    // como em WaW: Double Tap esfria 2x mais rápido
                w.heat = Mathf.Max(0f, w.heat - Time.deltaTime / time);
                if (w.heat <= 0f && w.overheated)
                {
                    w.overheated = false;
                    AudioManager.Play("ReloadIn", 0.6f);
                }
            }
        }

        public void StartReload()
        {
            WeaponState w = player.Inventory.Current;
            if (w == null || w.def.overheat || reloading != null || w.mag >= w.MagSize || w.reserve <= 0) return;
            reloading = StartCoroutine(Reload(w));
        }

        private IEnumerator Reload(WeaponState w)
        {
            float time = w.def.reload * (player.HasPerk(PerkType.TurboCola) ? Balance.SpeedColaReload : 1f);
            AudioManager.Play("ReloadOut", 0.7f, 0.05f);
            float t = 0f;
            while (t < time)
            {
                t += Time.deltaTime;
                ReloadProgress = t / time;
                yield return null;
            }

            int need = w.MagSize - w.mag;
            int take = Mathf.Min(need, w.reserve);
            w.mag += take;
            w.reserve -= take;
            ReloadProgress = 0f;
            reloading = null;
            AudioManager.Play("ReloadIn", 0.8f, 0.05f);
            player.Inventory.NotifyChanged();
        }

        public void TryFire()
        {
            WeaponState w = player.Inventory.Current;
            if (w == null || Time.time < nextShot || reloading != null) return;

            if (w.def.overheat && w.overheated)
            {
                if (player.Input.Fire.WasPressedThisFrame()) AudioManager.Play("DryFire", 0.7f);
                return;
            }

            if (!w.def.overheat && w.mag <= 0)
            {
                if (w.reserve > 0) StartReload();
                else if (player.Input.Fire.WasPressedThisFrame()) AudioManager.Play("DryFire", 0.7f);
                return;
            }

            WeaponDef d = w.def;
            // Gatilho Duplo: +100% de cadência (no lança-chamas ele acelera o resfriamento)
            bool faster = player.HasPerk(PerkType.GatilhoDuplo) && !d.overheat;
            float interval = d.interval * (faster ? Balance.DoubleTapFireRate : 1f);
            nextShot = Time.time + interval;
            lastFire = Time.time;
            game.ShotsFired++;
            if (!d.overheat) w.mag--;     // o calor do lança-chamas é contado no Update

            Vector2 origin = (Vector2)player.transform.position + player.Aim * 0.55f;
            // não atira de dentro da parede quando colado nela
            if (Physics2D.Linecast(player.transform.position, origin, Layers.WallMask))
            {
                origin = player.transform.position;
            }

            switch (d.mode)
            {
                case FireMode.Hitscan:
                    FireHitscan(w, origin);
                    break;
                case FireMode.Projectile:
                    Projectile.Fire(d, origin, Spread(player.Aim, d.spread), w.upgraded ? Balance.PackDamage : 1f);
                    break;
                case FireMode.Flame:
                    FireFlame(w, origin);
                    break;
            }

            if (d.mode != FireMode.Flame)
            {
                AudioManager.Play(d.sound, 0.75f, 0.06f);
                Fx.Flash(origin, d.cls == WeaponClass.Ray ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.85f, 0.5f), 3.5f, 1.3f, 0.06f);
                Fx.Sparks(origin, player.Aim, 3, w.Tracer);
            }

            CameraRig.Shake(d.shake);
            if (!d.overheat && w.mag == 0 && w.reserve > 0) StartReload();
            player.Inventory.NotifyChanged();
        }

        private static Vector2 Spread(Vector2 aim, float spread)
        {
            float a = Mathf.Atan2(aim.y, aim.x) + Random.Range(-spread, spread) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        private void FireHitscan(WeaponState w, Vector2 origin)
        {
            WeaponDef d = w.def;
            hitThisShot.Clear();
            bool anyHit = false;
            for (int p = 0; p < d.pellets; p++)
            {
                Vector2 dir = Spread(player.Aim, d.spread);
                int n = Physics2D.RaycastNonAlloc(origin, dir, hits, d.range, Layers.ShotMask);
                sorted.Clear();
                for (int i = 0; i < n; i++) sorted.Add(hits[i]);
                sorted.Sort((a, b) => a.distance.CompareTo(b.distance));

                Vector2 end = origin + dir * d.range;
                int pen = w.Penetration;
                float dmg = w.Damage;
                // cada bala/chumbo rola o crítico uma vez; ele vale para todos que ela atravessar
                bool crit = Balance.IsCrit(game.Rng.NextDouble());
                foreach (RaycastHit2D h in sorted)
                {
                    if (h.collider.gameObject.layer == Layers.Wall)
                    {
                        end = h.point;
                        Fx.Sparks(h.point, -dir, 3);
                        break;
                    }

                    Zombie z = h.collider.GetComponent<Zombie>();
                    if (z == null || !z.Targetable) continue;

                    int final = game.PowerUps.InstaKill ? 999999 : Mathf.RoundToInt(dmg * (crit ? Balance.CritMultiplier : 1f));
                    if (crit) Fx.Crit(h.point);
                    // pontos de acerto: 10 por zumbi por disparo (escopeta não multiplica)
                    bool firstHit = hitThisShot.Add(z);
                    z.TakeDamage(final, dir, firstHit ? DamageSource.Bullet : DamageSource.BulletNoPoints);
                    anyHit = true;
                    dmg *= Balance.PenetrationKeep;
                    pen--;
                    if (pen <= 0)
                    {
                        end = h.point;
                        break;
                    }
                }

                Fx.Tracer(origin, end, w.Tracer, d.pellets > 1 ? 0.04f : 0.05f);
            }

            if (anyHit) game.ShotsHit++;
        }

        private void FireFlame(WeaponState w, Vector2 origin)
        {
            WeaponDef d = w.def;
            if (Time.time > flameSound)
            {
                flameSound = Time.time + 0.18f;
                AudioManager.Play(d.sound, 0.5f, 0.1f);
            }

            // labareda grande: núcleo amarelo, corpo laranja, pontas vermelhas e fumaça
            float reach = d.range * 2.6f;
            Fx.Burst(origin, new Color(1f, 0.92f, 0.45f), 4, reach * 0.8f, 0.35f, 3f / 16f, true, player.Aim, d.spread);
            Fx.Burst(origin, new Color(1f, 0.55f, 0.12f), 8, reach, 0.55f, 5f / 16f, true, player.Aim, d.spread * 2f);
            Fx.Burst(origin, new Color(0.9f, 0.2f, 0.05f), 4, reach * 0.9f, 0.7f, 6f / 16f, true, player.Aim, d.spread * 2.4f);
            Fx.Burst(origin + player.Aim * d.range * 0.7f, new Color(0.18f, 0.16f, 0.16f), 2, 1.5f, 1.1f, 6f / 16f, false, Vector2.up, 90f);
            Fx.Flash(origin + player.Aim * 2f, new Color(1f, 0.55f, 0.15f), 6.5f, 1.4f, 0.1f);

            bool anyHit = false;
            foreach (Collider2D c in Physics2D.OverlapCircleAll(origin, d.range, Layers.ZombieMask))
            {
                Zombie z = c.GetComponent<Zombie>();
                if (z == null || !z.Targetable) continue;
                Vector2 to = (Vector2)z.transform.position - origin;
                if (Vector2.Angle(player.Aim, to) > d.spread + 10f) continue;
                if (Physics2D.Linecast(origin, z.transform.position, Layers.WallMask)) continue;
                int dmg = RollDamage(w.Damage, out bool crit);
                if (crit) Fx.Crit(z.transform.position, false);
                z.TakeDamage(dmg, to.normalized, DamageSource.Fire);
                anyHit = true;
            }

            if (anyHit) game.ShotsHit++;
        }
    }
}
