using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Sanatorio
{
    /// <summary>Arma desenhada a giz na parede. Já tem a arma? Compra munição pela metade.</summary>
    public class WallBuy : Interactable
    {
        public WeaponDef def;
        private SpriteRenderer chalk;

        public void Setup(WeaponDef d, SpriteRenderer chalkSprite)
        {
            def = d;
            chalk = chalkSprite;
        }

        public override string Prompt(Player p, out int cost)
        {
            Inventory inv = p.Inventory;
            if (def.cls == WeaponClass.Equipment)
            {
                if (def.id == "betty")
                {
                    cost = def.wallCost;
                    return inv.HasBetties ? null : $"{Key(p)} - COMPRAR BOUNCING BETTY x2";
                }

                cost = def.wallCost;
                return inv.LethalId == def.id && inv.Lethals >= Balance.GrenadeMax ? null : $"{Key(p)} - COMPRAR GRANADAS x{Balance.GrenadeMax}";
            }

            WeaponState owned = inv.Find(def.id);
            if (owned != null)
            {
                cost = owned.AmmoCost;
                return owned.Full ? null : $"{Key(p)} - MUNIÇÃO {owned.Name}";
            }

            cost = def.wallCost;
            return $"{Key(p)} - COMPRAR {def.name}";
        }

        public override void Use(Player p)
        {
            if (Prompt(p, out int cost) == null || !game.TrySpend(cost)) return;
            WeaponState owned = p.Inventory.Find(def.id);
            if (owned != null)
            {
                owned.Refill();
                p.Inventory.NotifyChanged();
                AudioManager.Play("AmmoBuy", 0.9f);
            }
            else
            {
                p.Gun.CancelReload();
                p.Inventory.Give(def);
                AudioManager.Play("WeaponBuy", 0.9f);
            }

            Fx.Burst(transform.position, new Color(0.95f, 0.95f, 0.9f), 10, 2f, 0.6f, 1.5f / 16f);
        }

        private void Update()
        {
            // o giz "acende" quando o jogador chega perto: mostra que é interativo
            if (chalk == null || game.Player == null) return;
            float d = Vector2.Distance(game.Player.transform.position, Point);
            float a = d < 3f ? 1f : 0.55f;
            chalk.color = Color.Lerp(chalk.color, new Color(0.95f, 0.95f, 0.88f, a), Time.deltaTime * 6f);
        }
    }

    /// <summary>Porta trancada com tábuas e preço; abrir libera a zona de trás.</summary>
    public class Door : Interactable
    {
        public MapData.Door data;
        private readonly List<GameObject> pieces = new List<GameObject>();
        private PixelText price;
        public bool Open { get; private set; }

        public void Setup(MapData.Door d, List<GameObject> tiles, PixelText priceLabel)
        {
            data = d;
            pieces.AddRange(tiles);
            price = priceLabel;
            Radius = 1.6f + Mathf.Max(0, d.tiles.Count - 3) * 0.25f;
        }

        public override string Prompt(Player p, out int cost)
        {
            cost = data.cost;
            if (Open) return null;
            if (data.IsPowerDoor)
            {
                cost = -1;
                return game.Power ? null : "PORTA ELÉTRICA - LIGUE A ENERGIA NO GERADOR";
            }

            return $"{Key(p)} - ABRIR PORTA";
        }

        public override void Use(Player p)
        {
            if (Open || data.IsPowerDoor) return;
            if (!game.TrySpend(data.cost)) return;
            OpenDoor();
        }

        public void OpenDoor()
        {
            if (Open) return;
            Open = true;
            AudioManager.PlayAt(data.IsPowerDoor ? "DoorPower" : "DoorOpen", transform.position, 1f);
            CameraRig.Shake(0.25f);
            foreach (Vector2Int t in data.tiles)
            {
                game.Nav.SetWalkable(t, true);
                Fx.Dust(game.Map.TileToWorld(t), 10);
            }

            if (price != null) Destroy(price.canvas.gameObject);
            StartCoroutine(Vanish());
            game.Zones.Unlock(data.zoneA);
            game.Zones.Unlock(data.zoneB);
        }

        private IEnumerator Vanish()
        {
            foreach (GameObject g in pieces)
            {
                Collider2D c = g.GetComponent<Collider2D>();
                if (c != null) c.enabled = false;
            }

            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                foreach (GameObject g in pieces)
                {
                    SpriteRenderer sr = g.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = new Color(1, 1, 1, 1f - t / 0.5f);
                    g.transform.localScale = Vector3.one * (1f - 0.3f * t);
                }

                yield return null;
            }

            foreach (GameObject g in pieces) g.SetActive(false);
        }
    }

    /// <summary>Disjuntor geral: liga refrigerantes, armadilhas, luzes e a porta elétrica.</summary>
    public class PowerSwitch : Interactable
    {
        private SpriteRenderer sr;

        public void Setup(SpriteRenderer s) => sr = s;

        public override string Prompt(Player p, out int cost)
        {
            cost = -1;
            return game.Power ? null : $"{Key(p)} - LIGAR A ENERGIA";
        }

        public override void Use(Player p)
        {
            if (game.Power) return;
            sr.sprite = Res.Sprite("Breaker_On");
            Fx.Sparks(transform.position, Vector2.down, 20, new Color(0.6f, 0.8f, 1f));
            Fx.Flash(transform.position, new Color(0.6f, 0.8f, 1f), 6f, 2f, 0.4f);
            game.TurnOnPower();
        }
    }

    /// <summary>Máquina de refrigerante. Só funciona com energia.</summary>
    public class PerkMachine : Interactable
    {
        public PerkType perk;
        private SpriteRenderer sr;
        private Light2D glow;
        private float humTimer;

        public void Setup(PerkType p, SpriteRenderer s, Light2D l)
        {
            perk = p;
            sr = s;
            glow = l;
            GameEvents.PowerOn += OnPower;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameEvents.PowerOn -= OnPower;
        }

        private void OnPower()
        {
            sr.sprite = Res.Sprite(PerkInfo.Sprite(perk) + "_On");
            StartCoroutine(PowerUpGlow());
        }

        private IEnumerator PowerUpGlow()
        {
            yield return new WaitForSeconds(0.3f + Random.value * 0.8f);
            float t = 0f;
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                glow.intensity = (Random.value < 0.5f ? 0.2f : 1.1f) * (t / 0.6f);
                yield return null;
            }

            glow.intensity = 1.1f;
        }

        private void Update()
        {
            if (!game.Power || game.Player == null) return;
            humTimer -= Time.deltaTime;
            if (humTimer <= 0f && Vector2.Distance(game.Player.transform.position, transform.position) < 5f)
            {
                humTimer = 9f + Random.value * 6f;
                AudioManager.PlayAt("Jingle" + perk, transform.position, 0.5f, 0f);
            }
        }

        public override string Prompt(Player p, out int cost)
        {
            cost = Balance.PerkCost(perk);
            string name = PerkInfo.Name(perk).ToUpperInvariant();
            if (!game.Power)
            {
                cost = -1;
                return $"{name} - SEM ENERGIA";
            }

            if (p.HasPerk(perk)) return null;
            if (perk == PerkType.ReviveJa && p.Inventory.ReviveBuys >= Balance.SoloReviveMaxBuys)
            {
                cost = -1;
                return "REVIVE-JÁ - ESGOTADO";
            }

            return $"{Key(p)} - BEBER {name}";
        }

        public override void Use(Player p)
        {
            if (!game.Power || p.HasPerk(perk)) return;
            if (perk == PerkType.ReviveJa && p.Inventory.ReviveBuys >= Balance.SoloReviveMaxBuys) return;
            if (!game.TrySpend(Balance.PerkCost(perk))) return;
            p.AddPerk(perk);
            Fx.Burst(transform.position, PerkInfo.Color(perk), 20, 3f, 0.6f, 2f / 16f, true);
            Fx.Flash(transform.position, PerkInfo.Color(perk), 4f, 1.6f, 0.4f);
        }
    }

    /// <summary>Faixa de grade elétrica numa varanda: mata zumbis por 25 s.</summary>
    public class TrapLever : Interactable
    {
        public MapData.Trap trap;
        private readonly List<SpriteRenderer> arcs = new List<SpriteRenderer>();
        private readonly List<Light2D> lights = new List<Light2D>();
        private SpriteRenderer lever;
        private float activeUntil;
        private float readyAt;
        private float tick;
        private float buzz;

        public bool Active => Time.time < activeUntil;

        public void Setup(MapData.Trap t, SpriteRenderer leverSprite, List<SpriteRenderer> arcSprites, List<Light2D> trapLights)
        {
            trap = t;
            lever = leverSprite;
            arcs.AddRange(arcSprites);
            lights.AddRange(trapLights);
        }

        public override string Prompt(Player p, out int cost)
        {
            cost = Balance.TrapCost;
            if (!game.Power)
            {
                cost = -1;
                return "ARMADILHA ELÉTRICA - SEM ENERGIA";
            }

            if (Active) return null;
            if (Time.time < readyAt)
            {
                cost = -1;
                return $"ARMADILHA RECARREGANDO ({Mathf.CeilToInt(readyAt - Time.time)}s)";
            }

            return $"{Key(p)} - LIGAR ARMADILHA";
        }

        public override void Use(Player p)
        {
            if (!game.Power || Active || Time.time < readyAt) return;
            if (!game.TrySpend(Balance.TrapCost)) return;
            activeUntil = Time.time + Balance.TrapDuration;
            readyAt = activeUntil + Balance.TrapCooldown;
            lever.sprite = Res.Sprite("Lever_On");
            AudioManager.PlayAt("TrapOn", transform.position, 1f);
        }

        private void Update()
        {
            bool on = Active;
            for (int i = 0; i < arcs.Count; i++)
            {
                arcs[i].enabled = on && Random.value > 0.25f;
                if (on && Random.value < 0.15f) arcs[i].flipX = !arcs[i].flipX;
            }

            foreach (Light2D l in lights) l.intensity = on ? Random.Range(0.4f, 1.2f) : 0f;

            if (!on)
            {
                if (lever != null && lever.sprite != null && Time.time > activeUntil && lever.sprite.name.EndsWith("On"))
                    lever.sprite = Res.Sprite("Lever_Off");
                return;
            }

            buzz -= Time.deltaTime;
            if (buzz <= 0f)
            {
                buzz = 0.45f;
                AudioManager.PlayAt("TrapBuzz", game.Map.TileToWorld(trap.tiles[trap.tiles.Count / 2]), 0.6f, 0.1f);
            }

            tick -= Time.deltaTime;
            if (tick > 0f) return;
            tick = 0.1f;

            foreach (Zombie z in game.Rounds.Alive.ToArray())
            {
                if (z == null || !z.Targetable) continue;
                if (OnTrap(z.transform.position))
                {
                    Fx.Sparks(z.transform.position, Vector2.up, 10, new Color(0.6f, 0.8f, 1f));
                    z.Kill(DamageSource.Trap, Vector2.zero, false);
                }
            }

            Player p = game.Player;
            if (p != null && OnTrap(p.transform.position))
            {
                p.Damage(Balance.TrapPlayerDamage, p.transform.position + Vector3.up);
            }
        }

        private bool OnTrap(Vector2 pos)
        {
            Vector2Int t = game.Map.WorldToTile(pos);
            foreach (Vector2Int tt in trap.tiles)
            {
                if (tt == t) return true;
            }

            return false;
        }
    }
}
