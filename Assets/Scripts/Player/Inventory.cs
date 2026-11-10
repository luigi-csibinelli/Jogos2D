using System.Collections.Generic;
using UnityEngine;

namespace Sanatorio
{
    public class WeaponState
    {
        public readonly WeaponDef def;
        public int mag;
        public int reserve;
        public float heat;          // 0..1, só armas que superaquecem
        public bool overheated;     // travada esfriando
        public bool upgraded;       // passou pelo Soco em Lata

        public WeaponState(WeaponDef def)
        {
            this.def = def;
            mag = def.mag;
            reserve = def.reserve;
        }

        // números efetivos (com a melhoria do Soco em Lata)
        public string Name => upgraded ? def.name + "+" : def.name;
        public int Damage => Mathf.RoundToInt(def.damage * (upgraded ? Balance.PackDamage : 1f));
        public int SplashDamage => Mathf.RoundToInt(def.splashDamage * (upgraded ? Balance.PackDamage : 1f));
        public int MagSize => upgraded ? Mathf.CeilToInt(def.mag * Balance.PackMag) : def.mag;
        public int ReserveSize => upgraded ? Mathf.RoundToInt(def.reserve * Balance.PackReserve) : def.reserve;
        public int Penetration => def.penetration + (upgraded ? 1 : 0);
        public Color Tracer => upgraded ? new Color(0.85f, 0.45f, 1f) : def.tracer;
        public float HeatTime => upgraded ? Balance.FlameHeatTimePacked : Balance.FlameHeatTime;
        public float LockTime => upgraded ? Balance.FlameLockPacked : Balance.FlameOverheatLock;
        public int AmmoCost => upgraded ? Balance.PackAmmoCost : def.AmmoCost;

        public int TotalAmmo => mag + reserve;
        public bool Full => mag == MagSize && reserve == ReserveSize;

        public void Upgrade()
        {
            upgraded = true;
            Refill();
        }

        public void Refill()
        {
            mag = MagSize;
            reserve = ReserveSize;
            heat = 0f;
            overheated = false;
        }
    }

    /// <summary>
    /// Duas armas (como em WaW, sem Mule Kick), granadas, Bouncing Betties
    /// e refrigerantes. Comprar uma terceira arma troca a que está na mão.
    /// </summary>
    public class Inventory
    {
        public const int Slots = 2;

        public readonly List<WeaponState> Weapons = new List<WeaponState>();
        public int CurrentIndex { get; private set; }
        public string LethalId { get; private set; } = "grenades";
        public int Lethals { get; set; } = 2;
        public bool HasBetties { get; private set; }
        public int Betties { get; set; }
        public readonly HashSet<PerkType> Perks = new HashSet<PerkType>();
        public int ReviveBuys { get; set; }

        public WeaponState Current => Weapons.Count > 0 ? Weapons[Mathf.Clamp(CurrentIndex, 0, Weapons.Count - 1)] : null;

        public event System.Action Changed;

        public void NotifyChanged() => Changed?.Invoke();

        /// <summary>Tira a arma (vai para dentro do Soco em Lata).</summary>
        public void Remove(WeaponState w)
        {
            int i = Weapons.IndexOf(w);
            if (i < 0) return;
            Weapons.RemoveAt(i);
            CurrentIndex = Mathf.Clamp(CurrentIndex > i ? CurrentIndex - 1 : CurrentIndex, 0, Mathf.Max(0, Weapons.Count - 1));
            NotifyChanged();
        }

        /// <summary>Devolve a arma melhorada (num slot livre, ou no lugar da atual).</summary>
        public void Return(WeaponState w)
        {
            if (Weapons.Count < Slots)
            {
                Weapons.Add(w);
                CurrentIndex = Weapons.Count - 1;
            }
            else
            {
                Weapons[CurrentIndex] = w;
            }

            NotifyChanged();
        }

        public bool Has(string id)
        {
            foreach (WeaponState w in Weapons)
            {
                if (w.def.id == id) return true;
            }

            return false;
        }

        public WeaponState Find(string id) => Weapons.Find(w => w.def.id == id);

        public void Give(WeaponDef def)
        {
            if (def.cls == WeaponClass.Equipment)
            {
                GiveEquipment(def);
                return;
            }

            WeaponState existing = Find(def.id);
            if (existing != null)
            {
                existing.Refill();
                CurrentIndex = Weapons.IndexOf(existing);
            }
            else if (Weapons.Count < Slots)
            {
                Weapons.Add(new WeaponState(def));
                CurrentIndex = Weapons.Count - 1;
            }
            else
            {
                Weapons[CurrentIndex] = new WeaponState(def);
            }

            NotifyChanged();
        }

        public void GiveEquipment(WeaponDef def)
        {
            if (def.id == "betty")
            {
                HasBetties = true;
                Betties = Mathf.Min(Balance.BettyMax, Betties + Balance.BettyPerRound);
            }
            else
            {
                LethalId = def.id;
                Lethals = Balance.GrenadeMax;
            }

            NotifyChanged();
        }

        /// <summary>Arma primária (0) ou secundária (1). Devolve false se não mudou.</summary>
        public bool Select(int slot)
        {
            if (slot < 0 || slot >= Weapons.Count || slot == CurrentIndex) return false;
            CurrentIndex = slot;
            NotifyChanged();
            return true;
        }

        public void Swap()
        {
            if (Weapons.Count < 2) return;
            CurrentIndex = (CurrentIndex + 1) % Weapons.Count;
            NotifyChanged();
        }

        public void MaxAmmo()
        {
            foreach (WeaponState w in Weapons) w.Refill();
            Lethals = Balance.GrenadeMax;
            if (HasBetties) Betties = Mathf.Max(Betties, Balance.BettyPerRound);
            NotifyChanged();
        }

        /// <summary>Como em WaW: quem tem Bouncing Betty ganha mais 2 por rodada, e +2 granadas.</summary>
        public void OnRoundEnd()
        {
            if (HasBetties) Betties = Mathf.Min(Balance.BettyMax, Betties + Balance.BettyPerRound);
            Lethals = Mathf.Min(Balance.GrenadeMax, Lethals + 2);
            NotifyChanged();
        }
    }
}
