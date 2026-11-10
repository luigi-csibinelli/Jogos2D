using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Sanatorio.Tests
{
    public class ShuffleBagTests
    {
        [Test]
        public void PowerUpCycle_NoRepeatsInsideCycle_AndNewCycleNeverStartsWithLast()
        {
            PowerUpType[] all = { PowerUpType.InstaKill, PowerUpType.DoublePoints, PowerUpType.Nuke, PowerUpType.MaxAmmo };
            for (int seed = 1; seed <= 200; seed++)
            {
                ShuffleBag<PowerUpType> bag = new ShuffleBag<PowerUpType>(all, seed);
                bag.Begin();
                PowerUpType last = default;
                bool hasLast = false;
                List<PowerUpType> drops = new List<PowerUpType>();
                for (int i = 0; i < 40; i++)
                {
                    last = bag.Next(last, hasLast);
                    hasLast = true;
                    drops.Add(last);
                }

                // cada ciclo de 4 drops tem os 4 tipos, sem repetir
                for (int c = 0; c < drops.Count / 4; c++)
                {
                    CollectionAssert.AreEquivalent(all, drops.Skip(c * 4).Take(4).ToList(), $"seed {seed}, ciclo {c}");
                }

                // nunca o mesmo duas vezes seguidas (nem na virada do ciclo)
                for (int i = 1; i < drops.Count; i++)
                {
                    Assert.AreNotEqual(drops[i - 1], drops[i], $"seed {seed}, drop {i}");
                }
            }
        }

        [Test]
        public void BoxCycle_VisitsEveryOtherSpotBeforeRepeating()
        {
            int[] spots = { 0, 1, 2, 3, 4 };
            for (int seed = 1; seed <= 200; seed++)
            {
                ShuffleBag<int> bag = new ShuffleBag<int>(spots, seed, true);
                int current = 0;   // começa na Sala do Gerador
                bag.Begin(current);
                HashSet<int> visited = new HashSet<int> { current };
                for (int i = 0; i < 4; i++)
                {
                    Assert.IsTrue(bag.TryNext(current, true, null, out int next));
                    Assert.IsFalse(visited.Contains(next), $"seed {seed}: repetiu {next} antes do ciclo acabar");
                    visited.Add(next);
                    current = next;
                }

                Assert.AreEqual(5, visited.Count);
                // ciclo novo: não pode começar onde a caixa está
                Assert.IsTrue(bag.TryNext(current, true, null, out int first));
                Assert.AreNotEqual(current, first, $"seed {seed}");
            }
        }

        [Test]
        public void BoxCycle_SkipsLockedSpotsButKeepsThemForLater()
        {
            int[] spots = { 0, 1, 2, 3, 4 };
            HashSet<int> unlocked = new HashSet<int> { 0, 1 };
            ShuffleBag<int> bag = new ShuffleBag<int>(spots, 7, true);
            bag.Begin(0);

            Assert.IsTrue(bag.TryNext(0, true, s => unlocked.Contains(s), out int a));
            Assert.AreEqual(1, a);
            unlocked.Add(3);
            Assert.IsTrue(bag.TryNext(1, true, s => unlocked.Contains(s), out int b));
            Assert.AreEqual(3, b, "o ponto 3 ficou no ciclo enquanto estava trancado");
        }

        [Test]
        public void BoxCycle_NoAvailableSpot_ReturnsFalse()
        {
            ShuffleBag<int> bag = new ShuffleBag<int>(new[] { 0, 1, 2 }, 3, true);
            bag.Begin(0);
            Assert.IsFalse(bag.TryNext(0, true, s => s == 0, out _));
        }
    }

    public class MapTests
    {
        [Test]
        public void VerrucktMap_IsValid()
        {
            MapData map = MapData.Load();
            Assert.IsTrue(map.Validate(out string report), report);
        }

        [Test]
        public void VerrucktMap_HasWaWLayout()
        {
            MapData map = MapData.Load();
            Assert.AreEqual(10, map.Doors.Count, "9 portas pagas + porta elétrica");
            Assert.AreEqual(4, map.Traps.Count, "duas armadilhas por varanda");
            Assert.AreEqual(5, map.Markers.Count(m => m.c == 'X' || m.c == '$'), "5 pontos da caixa");
            Assert.AreEqual(1, map.Markers.Count(m => m.c == '$'));
            Assert.AreEqual(3, map.ZoneAt(map.Markers.First(m => m.c == '$').tile), "caixa começa na Sala do Gerador");
            Assert.AreEqual(1, map.Markers.Count(m => m.c == 'P'));
            Assert.AreEqual(4, map.Markers.Count(m => VerrucktMap.Perks.ContainsKey(m.c)));
            Assert.AreEqual(8, map.Markers.Count(m => m.c == '?'));
            MapData.Marker pack = map.Markers.Single(m => m.c == 'Z');
            Assert.AreEqual(5, pack.zone, "Soco em Lata fica no Necrotério");

            MapData.Door power = map.Doors.Single(d => d.IsPowerDoor);
            CollectionAssert.AreEquivalent(new[] { 0, 4 }, new[] { power.zoneA, power.zoneB });
            MapData.Door kitchen = map.Doors.Single(d => d.id == 'i');
            Assert.AreEqual(1000, kitchen.cost, "Cozinha -> Gerador custa 1000");
        }

        [Test]
        public void EveryWallBuy_IsInCatalogWithPrice()
        {
            foreach (string id in VerrucktMap.WallBuys.Values)
            {
                WeaponDef w = WeaponCatalog.Get(id);
                Assert.IsNotNull(w, id);
                Assert.Greater(w.wallCost, 0, id);
            }
        }
    }

    public class NavGridTests
    {
        [Test]
        public void FlowField_GoesAroundWall()
        {
            bool[,] walk = new bool[7, 5];
            for (int x = 0; x < 7; x++)
                for (int y = 0; y < 5; y++)
                    walk[x, y] = true;
            for (int y = 0; y < 4; y++) walk[3, y] = false;   // parede com abertura embaixo

            NavGrid nav = new NavGrid(walk);
            nav.Update(new Vector2Int(6, 0));
            Vector2Int pos = new Vector2Int(0, 0);
            for (int i = 0; i < 30 && pos != new Vector2Int(6, 0); i++)
            {
                Assert.IsTrue(nav.NextStep(pos, out Vector2Int step));
                Assert.IsTrue(nav.IsWalkable(step));
                pos = step;
            }

            Assert.AreEqual(new Vector2Int(6, 0), pos);
        }

        [Test]
        public void ClosedDoor_BlocksUntilOpened()
        {
            bool[,] walk = new bool[5, 1];
            for (int x = 0; x < 5; x++) walk[x, 0] = x != 2;
            NavGrid nav = new NavGrid(walk);
            nav.Update(new Vector2Int(4, 0));
            Assert.AreEqual(NavGrid.Unreachable, nav.DistanceAt(new Vector2Int(0, 0)));
            nav.SetWalkable(new Vector2Int(2, 0), true);
            nav.Update(new Vector2Int(4, 0));
            Assert.AreEqual(4, nav.DistanceAt(new Vector2Int(0, 0)));
        }
    }

    public class BalanceTests
    {
        [Test]
        public void ZombieHealth_FollowsWaW()
        {
            Assert.AreEqual(150, Balance.ZombieHealth(1));
            Assert.AreEqual(950, Balance.ZombieHealth(9));
            Assert.AreEqual(1045, Balance.ZombieHealth(10));
            Assert.Greater(Balance.ZombieHealth(20), Balance.ZombieHealth(19));
        }

        [Test]
        public void ZombieCount_GrowsAndInsanoHasMore()
        {
            int prev = 0;
            for (int r = 1; r <= 30; r++)
            {
                int c = Balance.ZombieCount(r);
                Assert.GreaterOrEqual(c, prev, $"rodada {r}");
                prev = c;
                Assert.Greater(Balance.ZombieCount(r, Difficulty.Insano), c);
            }
        }

        [Test]
        public void Crit_TwentyPercentDoublesDamage()
        {
            Assert.AreEqual(2f, Balance.CritMultiplier);
            System.Random rng = new System.Random(42);
            int crits = 0, n = 20000;
            for (int i = 0; i < n; i++) if (Balance.IsCrit(rng.NextDouble())) crits++;
            Assert.AreEqual(0.2, (double)crits / n, 0.015);
        }

        [Test]
        public void MysteryBox_NeverGivesWallWeapons()
        {
            foreach (WeaponDef w in WeaponCatalog.All)
            {
                if (w.wallCost > 0) Assert.IsFalse(WeaponCatalog.InBox(w), w.id);
            }

            System.Random rng = new System.Random(1);
            for (int i = 0; i < 2000; i++)
            {
                WeaponDef got = WeaponCatalog.RollBox(_ => false, rng);
                Assert.AreEqual(0, got.wallCost, got.id);
            }
        }

        [Test]
        public void Flamethrower_FiftyPerTick_Overheats()
        {
            WeaponDef f = WeaponCatalog.Get("flamethrower");
            Assert.AreEqual(50, f.damage);
            Assert.IsTrue(f.overheat);
            Assert.IsTrue(WeaponCatalog.InBox(f));
        }

        [Test]
        public void DoubleTap_DoublesFireRate_AndPenetrationKeepsHalf()
        {
            Assert.AreEqual(0.5f, Balance.DoubleTapFireRate);
            Assert.AreEqual(0.5f, Balance.PenetrationKeep);
        }

        [Test]
        public void SpecialRounds_FollowTheSchedule()
        {
            Assert.AreEqual(6, Balance.DogFirstRoundMin);
            Assert.AreEqual(7, Balance.DogFirstRoundMax);
            Assert.AreEqual(7, Balance.DogIntervalMin);
            Assert.AreEqual(8, Balance.DogIntervalMax);
            Assert.AreEqual(20, Balance.PanzerFirstRound);
            Assert.AreEqual(5f, Balance.FlameOverheatLock);
            Assert.Greater(Balance.DogSpeed, Balance.PlayerSpeed, "cão é mais rápido que o jogador");
            Assert.LessOrEqual(Balance.DogCount(20), Balance.MaxAlive);
        }

        [Test]
        public void PackPunch_UpgradesWeapon()
        {
            WeaponState w = new WeaponState(WeaponCatalog.Get("mp40"));
            int dmg = w.Damage, mag = w.MagSize, res = w.ReserveSize;
            w.Upgrade();
            Assert.AreEqual(dmg * 2, w.Damage);
            Assert.Greater(w.MagSize, mag);
            Assert.AreEqual(res * 2, w.ReserveSize);
            Assert.AreEqual(w.MagSize, w.mag, "sai da máquina com o pente cheio");
            Assert.AreEqual(Balance.PackAmmoCost, w.AmmoCost);
            Assert.AreEqual("MP40+", w.Name);
        }

        [Test]
        public void Knife_KillsRoundOneZombie()
        {
            Assert.GreaterOrEqual(Balance.KnifeDamage, Balance.ZombieHealth(1));
        }

        [Test]
        public void TwoHitsDown_FiveWithFerroNog()
        {
            Assert.AreEqual(2, Mathf.CeilToInt((float)Balance.PlayerHealth / Balance.ZombieHitDamage));
            Assert.AreEqual(5, Mathf.CeilToInt((float)Balance.PlayerHealthJugg / Balance.ZombieHitDamage));
        }
    }
}
