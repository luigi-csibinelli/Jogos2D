using UnityEngine;

namespace Sanatorio
{
    public enum Difficulty { Normal = 0, Insano = 1 }

    /// <summary>
    /// Todos os números ajustáveis do jogo num lugar só. Os valores-base seguem
    /// as regras de World at War (vida dos zumbis, pontos, preços), ajustadas
    /// para uma câmera de cima e um jogador só.
    /// </summary>
    public static class Balance
    {
        // ------------------------------------------------------------ jogador
        public const float PlayerSpeed = 4.4f;
        public const float PlayerRadius = 0.32f;
        public const int PlayerHealth = 100;
        public const int PlayerHealthJugg = 250;
        public const float RegenDelay = 2.6f;
        public const float RegenPerSecond = 120f;
        public const float HurtInvulnerability = 0.35f;
        public const float InteractRadius = 1.25f;

        // ------------------------------------------------------------ pontos
        public const int StartingPoints = 500;
        public const int PointsPerHit = 10;
        public const int PointsPerKill = 60;
        public const int PointsPerKnifeKill = 130;
        public const int PointsPerPlank = 10;
        public const int PlankPointsCapPerRound = 400;
        public const int NukePoints = 400;

        // ------------------------------------------------------------ faca
        public const int KnifeDamage = 150;
        public const float KnifeRange = 1.25f;
        public const float KnifeArc = 110f;
        public const float KnifeCooldown = 0.55f;

        // ------------------------------------------------------------ crítico
        /// <summary>Chance de um disparo de arma de fogo causar dano dobrado.</summary>
        public const float CritChance = 0.2f;
        public const float CritMultiplier = 2f;

        /// <summary>roll em [0,1): abaixo da chance é crítico.</summary>
        public static bool IsCrit(double roll) => roll < CritChance;

        /// <summary>Bala que atravessa um zumbi segue com esta fração do dano para o próximo.</summary>
        public const float PenetrationKeep = 0.5f;

        // ------------------------------------------------------------ lança-chamas (superaquece)
        public const float FlameHeatTime = 5f;           // segundos de fogo contínuo até superaquecer
        public const float FlameCoolTime = 2.5f;         // de cheio a frio, soltando o gatilho
        public const float FlameOverheatLock = 5f;       // travado esfriando depois de superaquecer
        public const float FlameCoolDelay = 0.15f;       // só esfria depois de soltar o gatilho

        // ------------------------------------------------------------ zumbis
        public const int MaxAlive = 24;
        public const float ZombieWalkSpeed = 1.55f;
        public const float ZombieRunSpeed = 3.35f;
        public const float CrawlerSpeed = 1.0f;
        public const int ZombieHitDamage = 50;           // 2 golpes derrubam (100 de vida)
        public const float ZombieAttackRange = 0.85f;
        public const float ZombieWindup = 0.38f;         // telegrafado: dá para esquivar
        public const float ZombieAttackCooldown = 1.05f;
        public const float PlankTearInterval = 1.1f;
        public const float StuckTimeout = 15f;
        public const float BetweenRounds = 10f;

        public static int ZombieHealth(int round)
        {
            if (round <= 9)
            {
                return 150 + 100 * (round - 1);
            }

            float hp = 950f;
            for (int r = 10; r <= round; r++)
            {
                hp *= 1.1f;
            }

            return Mathf.RoundToInt(Mathf.Min(hp, 50000f));
        }

        private static readonly int[] EarlyCounts = { 6, 8, 13, 18, 24, 27, 28, 28, 29 };

        /// <summary>Quantos zumbis a rodada tem no total (curva solo de WaW/BO).</summary>
        public static int ZombieCount(int round, Difficulty difficulty = Difficulty.Normal)
        {
            int count = round <= EarlyCounts.Length
                ? EarlyCounts[Mathf.Max(0, round - 1)]
                : Mathf.RoundToInt(0.09f * round * round - 0.0029f * round + 23.958f);

            if (difficulty == Difficulty.Insano)
            {
                count = Mathf.RoundToInt(count * 1.25f);
            }

            return count;
        }

        /// <summary>Fração dos zumbis que correm. Verrückt é rápido: corrida cedo.</summary>
        public static float RunnerChance(int round, Difficulty difficulty = Difficulty.Normal)
        {
            float c = round <= 1 ? 0f : Mathf.Clamp01((round - 1) * 0.16f);
            if (difficulty == Difficulty.Insano)
            {
                c = Mathf.Clamp01(c + 0.35f);
            }

            return c;
        }

        public static float SpawnDelay(int round)
        {
            return Mathf.Max(0.3f, 2.0f * Mathf.Pow(0.95f, round - 1));
        }

        // ------------------------------------------------------------ Soco em Lata (melhoria de arma)
        public const int PackCost = 5000;
        public const float PackTime = 3.5f;
        public const float PackOfferTime = 12f;
        public const int PackAmmoCost = 4500;          // munição de arma melhorada na parede
        public const float PackDamage = 2f;
        public const float PackMag = 1.5f;
        public const float PackReserve = 2f;
        public const float FlameHeatTimePacked = 8f;   // melhorado: aguenta mais tempo
        public const float FlameLockPacked = 3.5f;     // e esfria mais rápido

        // ------------------------------------------------------------ rodadas dos cães
        public const int DogFirstRoundMin = 6, DogFirstRoundMax = 7;   // primeira: rodada 6 ou 7
        public const int DogIntervalMin = 7, DogIntervalMax = 8;       // depois: a cada 7 ou 8 rodadas
        public const float DogSpeed = 5.1f;                            // mais rápido que o jogador
        public const float DogWindup = 0.22f;

        public static int DogCount(int dogRoundIndex) => Mathf.Min(MaxAlive, 6 + 2 * dogRoundIndex);

        public static int DogHealth(int round) => Mathf.RoundToInt(ZombieHealth(round) * 0.45f);

        public static float DogSpawnDelay(int dogRoundIndex) => Mathf.Max(0.6f, 2.2f - 0.25f * dogRoundIndex);

        // ------------------------------------------------------------ Panzer
        public const int PanzerFirstRound = 20;
        public const int PanzerEvery = 4;                // volta a cada 4 rodadas (se não cair em rodada de cães)
        public const float PanzerSpeed = 1.9f;
        public const float PanzerArmor = 0.5f;           // balas e fogo causam metade; explosivos, cheio
        public const float PanzerWindup = 0.55f;
        public const float PanzerMeleeRange = 1.2f;
        public const int PanzerMeleeDamage = 130;        // derruba sem Ferro-Nog; com ele, 2 socos
        public const float PanzerFlameRange = 3.2f;
        public const float PanzerFlameTime = 2.2f;
        public const float PanzerFlameCooldown = 5f;
        public const int PanzerFlameDamage = 22;         // a cada 0,25 s
        public const float PanzerClawRange = 9f;
        public const float PanzerClawCooldown = 7f;
        public const float PanzerClawPull = 3.2f;
        public const int PanzerClawBreakDamage = 1500;
        public const int PanzerKillPoints = 500;

        public static int PanzerHealth(int round) => 9000 + 700 * Mathf.Max(0, round - PanzerFirstRound);

        // ------------------------------------------------------------ power-ups
        public const float PowerUpDropChance = 0.025f;
        public const int PowerUpMaxPerRound = 4;
        public const float PowerUpLifetime = 26f;
        public const float PowerUpDuration = 30f;

        // ------------------------------------------------------------ caixa
        public const int BoxCost = 950;
        public const float BoxRollTime = 3.6f;
        public const float BoxOfferTime = 11f;
        public const int BoxSafeRolls = 3;               // antes disso nunca sai o ursinho
        public const float BoxTeddyBaseChance = 0.16f;
        public const float BoxTeddyStep = 0.06f;
        public const float BoxMoveTime = 7f;

        // ------------------------------------------------------------ armadilhas
        public const int TrapCost = 1000;
        public const float TrapDuration = 25f;
        public const float TrapCooldown = 50f;
        public const int TrapPlayerDamage = 60;

        // ------------------------------------------------------------ explosivos
        public const int GrenadeMax = 4;
        public const float GrenadeFuse = 1.9f;
        public const float GrenadeRadius = 2.6f;
        public const int GrenadeDamage = 700;
        public const float MolotovDuration = 4f;
        public const int MolotovTickDamage = 70;
        public const int BettyPerRound = 2;
        public const int BettyMax = 4;
        public const float BettyRadius = 2.4f;
        public const int BettyDamage = 1200;

        // ------------------------------------------------------------ refrigerantes
        public const float SpeedColaReload = 0.5f;
        public const float DoubleTapFireRate = 0.5f;     // intervalo x0.5 = +100% de cadência
        public const int SoloReviveMaxBuys = 3;
        public const float SelfReviveTime = 3f;
        public const float ReviveInvulnerability = 3f;

        public static int PerkCost(PerkType perk)
        {
            switch (perk)
            {
                case PerkType.FerroNog: return 2500;
                case PerkType.ReviveJa: return 500;      // preço solo (estilo Black Ops)
                case PerkType.TurboCola: return 3000;
                case PerkType.GatilhoDuplo: return 2000;
            }

            return 0;
        }

        // ------------------------------------------------------------ dificuldade
        public static float ZombieSpeedMultiplier(Difficulty d) => d == Difficulty.Insano ? 1.15f : 1f;
    }
}
