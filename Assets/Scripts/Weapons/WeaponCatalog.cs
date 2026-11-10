using System.Collections.Generic;
using UnityEngine;

namespace Sanatorio
{
    public enum WeaponClass { Pistol, Rifle, Sniper, Smg, Shotgun, Lmg, Launcher, Flame, Ray, Equipment }

    public enum FireMode
    {
        Hitscan,      // bala instantânea (com traçante)
        Projectile,   // foguete / granada de fuzil / raio, explode no impacto
        Flame,        // cone curto contínuo
    }

    public class WeaponDef
    {
        public string id;
        public string name;
        public WeaponClass cls;
        public FireMode mode = FireMode.Hitscan;
        public int damage;
        public int pellets = 1;
        public float interval;          // segundos entre disparos
        public bool auto;
        public int mag;
        public int reserve;
        public float reload;
        public float spread;            // graus (meio-ângulo)
        public float range = 16f;       // tiles
        public int penetration = 1;     // quantos zumbis a bala atravessa
        public float projectileSpeed = 14f;
        public float splashRadius;
        public int splashDamage;
        public float shake = 0.05f;
        public float moveMultiplier = 1f;
        public int wallCost;            // 0 = não é vendida na parede
        public float boxWeight = 1f;    // 0 = não sai na caixa (arma de parede também nunca sai)
        public bool overheat;           // munição infinita, mas superaquece (lança-chamas)
        public string sound = "ShotPistol";
        public Color tracer = new Color(1f, 0.92f, 0.55f);

        public int AmmoCost => wallCost / 2;
        public string Icon => "Gun_" + id;
    }

    /// <summary>
    /// Arsenal de Verrückt em World at War: armas iniciais, de parede e da caixa.
    /// Os nomes são os das armas reais da Segunda Guerra; os números são nossos,
    /// equilibrados para a visão de cima (dano por bala contra 150 HP na rodada 1).
    /// </summary>
    public static class WeaponCatalog
    {
        public static readonly List<WeaponDef> All = new List<WeaponDef>
        {
            // ---------------------------------------------------- iniciais
            new WeaponDef { id = "m1911", name = "M1911", cls = WeaponClass.Pistol, damage = 40, interval = 0.16f,
                mag = 8, reserve = 64, reload = 1.6f, spread = 3f, boxWeight = 0f, sound = "ShotPistol" },

            // ---------------------------------------------------- pistola / fuzis
            new WeaponDef { id = "magnum", name = ".357 Magnum", cls = WeaponClass.Pistol, damage = 170, interval = 0.32f,
                mag = 6, reserve = 48, reload = 2.8f, spread = 2f, penetration = 2, shake = 0.12f, sound = "ShotMagnum" },
            new WeaponDef { id = "kar98k", name = "Kar98k", cls = WeaponClass.Rifle, damage = 190, interval = 1.05f,
                mag = 5, reserve = 50, reload = 2.6f, spread = 0.6f, penetration = 3, shake = 0.14f, wallCost = 200, sound = "ShotRifle" },
            new WeaponDef { id = "springfield", name = "Springfield", cls = WeaponClass.Rifle, damage = 190, interval = 1.05f,
                mag = 5, reserve = 50, reload = 2.6f, spread = 0.6f, penetration = 3, shake = 0.14f, wallCost = 200, sound = "ShotRifle" },
            new WeaponDef { id = "gewehr43", name = "Gewehr 43", cls = WeaponClass.Rifle, damage = 120, interval = 0.2f,
                mag = 10, reserve = 170, reload = 2.8f, spread = 1.5f, penetration = 2, shake = 0.09f, wallCost = 600, sound = "ShotRifle" },
            new WeaponDef { id = "garand", name = "M1 Garand", cls = WeaponClass.Rifle, damage = 120, interval = 0.2f,
                mag = 8, reserve = 128, reload = 2.4f, spread = 1.5f, penetration = 2, shake = 0.09f, wallCost = 600, sound = "ShotRifle" },
            new WeaponDef { id = "garand_m7", name = "M1 Garand + M7", cls = WeaponClass.Launcher, mode = FireMode.Projectile,
                damage = 150, splashRadius = 2.2f, splashDamage = 450, interval = 0.6f, mag = 1, reserve = 12, reload = 1.8f,
                spread = 1f, projectileSpeed = 11f, shake = 0.2f, boxWeight = 0.7f, sound = "ShotLauncher", tracer = new Color(0.9f, 0.8f, 0.5f) },
            new WeaponDef { id = "carbine", name = "M1A1 Carbine", cls = WeaponClass.Rifle, damage = 95, interval = 0.12f,
                mag = 15, reserve = 165, reload = 2.2f, spread = 2f, sound = "ShotPistol" },
            new WeaponDef { id = "ptrs41", name = "PTRS-41", cls = WeaponClass.Sniper, damage = 650, interval = 0.6f,
                mag = 5, reserve = 50, reload = 3.5f, spread = 0.2f, range = 26f, penetration = 6, shake = 0.3f,
                moveMultiplier = 0.85f, sound = "ShotSniper" },

            // ---------------------------------------------------- submetralhadoras / fuzis de assalto
            new WeaponDef { id = "stg44", name = "STG-44", cls = WeaponClass.Smg, damage = 100, interval = 0.11f, auto = true,
                mag = 30, reserve = 210, reload = 2.6f, spread = 3f, shake = 0.05f, wallCost = 1200, sound = "ShotAuto" },
            new WeaponDef { id = "thompson", name = "Thompson", cls = WeaponClass.Smg, damage = 100, interval = 0.085f, auto = true,
                mag = 20, reserve = 200, reload = 2.4f, spread = 4f, shake = 0.04f, wallCost = 1200, sound = "ShotSmg" },
            new WeaponDef { id = "mp40", name = "MP40", cls = WeaponClass.Smg, damage = 90, interval = 0.1f, auto = true,
                mag = 32, reserve = 192, reload = 2.4f, spread = 4f, shake = 0.04f, wallCost = 1000, sound = "ShotSmg" },
            new WeaponDef { id = "ppsh", name = "PPSh-41", cls = WeaponClass.Smg, damage = 85, interval = 0.05f, auto = true,
                mag = 71, reserve = 284, reload = 3.0f, spread = 6f, shake = 0.04f, sound = "ShotSmg" },

            // ---------------------------------------------------- escopetas
            new WeaponDef { id = "trenchgun", name = "M1897 Trench Gun", cls = WeaponClass.Shotgun, damage = 45, pellets = 7,
                interval = 0.7f, mag = 6, reserve = 60, reload = 3.0f, spread = 13f, range = 8f, penetration = 2,
                shake = 0.2f, wallCost = 1500, sound = "ShotShotgun" },
            new WeaponDef { id = "doublebarrel", name = "Escopeta Cano Duplo", cls = WeaponClass.Shotgun, damage = 50, pellets = 7,
                interval = 0.25f, mag = 2, reserve = 60, reload = 2.6f, spread = 15f, range = 8f, penetration = 2,
                shake = 0.22f, wallCost = 1200, sound = "ShotShotgun" },
            new WeaponDef { id = "sawedoff", name = "Cano Duplo Serrada", cls = WeaponClass.Shotgun, damage = 50, pellets = 10,
                interval = 0.25f, mag = 2, reserve = 60, reload = 2.4f, spread = 24f, range = 6f, penetration = 2,
                shake = 0.25f, wallCost = 1200, sound = "ShotShotgun" },

            // ---------------------------------------------------- metralhadoras
            new WeaponDef { id = "bar", name = "BAR", cls = WeaponClass.Lmg, damage = 125, interval = 0.13f, auto = true,
                mag = 20, reserve = 180, reload = 2.8f, spread = 2.5f, penetration = 2, shake = 0.07f, wallCost = 2500, sound = "ShotAuto" },
            new WeaponDef { id = "mg42", name = "MG42", cls = WeaponClass.Lmg, damage = 120, interval = 0.05f, auto = true,
                mag = 125, reserve = 375, reload = 5.5f, spread = 5f, penetration = 2, shake = 0.06f,
                moveMultiplier = 0.85f, sound = "ShotAuto" },
            new WeaponDef { id = "fg42", name = "FG42", cls = WeaponClass.Lmg, damage = 115, interval = 0.075f, auto = true,
                mag = 20, reserve = 240, reload = 2.6f, spread = 3.5f, penetration = 2, shake = 0.06f, sound = "ShotAuto" },
            new WeaponDef { id = "m1919", name = "Browning M1919", cls = WeaponClass.Lmg, damage = 130, interval = 0.08f, auto = true,
                mag = 125, reserve = 375, reload = 6.0f, spread = 4f, penetration = 2, shake = 0.07f,
                moveMultiplier = 0.85f, sound = "ShotAuto" },

            // ---------------------------------------------------- especiais
            new WeaponDef { id = "panzerschreck", name = "Panzerschreck", cls = WeaponClass.Launcher, mode = FireMode.Projectile,
                damage = 400, splashRadius = 2.8f, splashDamage = 1100, interval = 0.8f, mag = 1, reserve = 14, reload = 2.5f,
                spread = 0.5f, projectileSpeed = 12f, shake = 0.35f, boxWeight = 0.8f, sound = "ShotLauncher",
                tracer = new Color(1f, 0.6f, 0.2f) },
            new WeaponDef { id = "flamethrower", name = "M2 Lança-Chamas", cls = WeaponClass.Flame, mode = FireMode.Flame,
                damage = 50, interval = 0.05f, auto = true, mag = 100, reserve = 0, reload = 0f, spread = 14f, range = 4.2f,
                penetration = 6, shake = 0.02f, moveMultiplier = 0.9f, boxWeight = 0.8f, sound = "Flame", overheat = true },
            new WeaponDef { id = "raygun", name = "Arma de Raios", cls = WeaponClass.Ray, mode = FireMode.Projectile,
                damage = 1000, splashRadius = 1.5f, splashDamage = 300, interval = 0.18f, mag = 20, reserve = 160, reload = 3.0f,
                spread = 1f, projectileSpeed = 22f, shake = 0.06f, boxWeight = 0.35f, sound = "ShotRay",
                tracer = new Color(0.4f, 1f, 0.4f) },

            // ---------------------------------------------------- equipamento (não ocupam slot)
            new WeaponDef { id = "grenades", name = "Stielhandgranate", cls = WeaponClass.Equipment, wallCost = 250, boxWeight = 0.9f },
            new WeaponDef { id = "molotov", name = "Coquetel Molotov", cls = WeaponClass.Equipment, boxWeight = 0.6f },
            new WeaponDef { id = "betty", name = "Bouncing Betty", cls = WeaponClass.Equipment, wallCost = 1000, boxWeight = 0f },
        };

        private static Dictionary<string, WeaponDef> byId;

        public static WeaponDef Get(string id)
        {
            if (byId == null)
            {
                byId = new Dictionary<string, WeaponDef>();
                foreach (WeaponDef w in All)
                {
                    byId[w.id] = w;
                }
            }

            return byId.TryGetValue(id, out WeaponDef def) ? def : null;
        }

        /// <summary>Sai na caixa: tem peso e NÃO é vendida na parede.</summary>
        public static bool InBox(WeaponDef w) => w.boxWeight > 0f && w.wallCost == 0;

        /// <summary>Armas que podem sair da caixa e que o jogador ainda não tem.</summary>
        public static WeaponDef RollBox(System.Func<WeaponDef, bool> owned, System.Random rng)
        {
            float total = 0f;
            foreach (WeaponDef w in All)
            {
                if (InBox(w) && !owned(w)) total += w.boxWeight;
            }

            double r = rng.NextDouble() * total;
            foreach (WeaponDef w in All)
            {
                if (!InBox(w) || owned(w)) continue;
                r -= w.boxWeight;
                if (r <= 0) return w;
            }

            return Get("mp40");
        }
    }
}
