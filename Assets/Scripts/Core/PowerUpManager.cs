using UnityEngine;

namespace Sanatorio
{
    public enum PowerUpType { InstaKill = 0, DoublePoints = 1, Nuke = 2, MaxAmmo = 3 }

    /// <summary>
    /// Power-ups que caem dos zumbis. O tipo segue um ciclo (ShuffleBag): os 4
    /// saem em ordem aleatória antes de qualquer um repetir, e um ciclo novo
    /// nunca começa pelo último que caiu. Máximo de 4 por rodada.
    /// </summary>
    public class PowerUpManager : MonoBehaviour
    {
        public float InstaKillTime { get; private set; }
        public float DoublePointsTime { get; private set; }
        public bool InstaKill => InstaKillTime > 0f;
        public bool DoublePoints => DoublePointsTime > 0f;

        private ShuffleBag<PowerUpType> bag;
        private PowerUpType last;
        private bool hasLast;
        private int droppedThisRound;
        private Game game;

        public static string Name(PowerUpType t)
        {
            switch (t)
            {
                case PowerUpType.InstaKill: return "MORTE INSTANTÂNEA";
                case PowerUpType.DoublePoints: return "PONTOS EM DOBRO";
                case PowerUpType.Nuke: return "BOMBA";
                case PowerUpType.MaxAmmo: return "MUNIÇÃO MÁXIMA";
            }

            return t.ToString();
        }

        private void Start()
        {
            game = Game.I;
            bag = new ShuffleBag<PowerUpType>(
                new[] { PowerUpType.InstaKill, PowerUpType.DoublePoints, PowerUpType.Nuke, PowerUpType.MaxAmmo },
                game.Rng.Next());
            bag.Begin();
            GameEvents.RoundStarted += _ => droppedThisRound = 0;
        }

        private void Update()
        {
            if (InstaKillTime > 0f) InstaKillTime -= Time.deltaTime;
            if (DoublePointsTime > 0f) DoublePointsTime -= Time.deltaTime;
        }

        /// <summary>Chamado em toda morte de zumbi (exceto pela bomba).</summary>
        public void TryDrop(Vector2 pos)
        {
            if (droppedThisRound >= Balance.PowerUpMaxPerRound) return;
            if (game.Rng.NextDouble() > Balance.PowerUpDropChance) return;
            if (!game.Map.IsWalkable(game.Map.WorldToTile(pos).x, game.Map.WorldToTile(pos).y)) return;

            PowerUpType type = bag.Next(last, hasLast);
            last = type;
            hasLast = true;
            droppedThisRound++;
            PowerUpPickup.Spawn(type, pos);
        }

        /// <summary>Drop garantido (Panzer): próximo tipo do ciclo, fora do limite por rodada.</summary>
        public void ForceDrop(Vector2 pos)
        {
            PowerUpType type = bag.Next(last, hasLast);
            last = type;
            hasLast = true;
            PowerUpPickup.Spawn(type, pos);
        }

        /// <summary>Último cão da rodada especial: sempre Munição Máxima.</summary>
        public void DropMaxAmmo(Vector2 pos)
        {
            PowerUpPickup.Spawn(PowerUpType.MaxAmmo, pos);
        }

        public void Collect(PowerUpType type, Vector2 pos)
        {
            AudioManager.Sting("PowerUp");
            GameEvents.RaiseToast(Name(type));
            Fx.Burst(pos, new Color(0.5f, 1f, 0.4f), 24, 4f, 0.5f, 2f / 16f, true);

            switch (type)
            {
                case PowerUpType.InstaKill:
                    InstaKillTime = Balance.PowerUpDuration;
                    break;
                case PowerUpType.DoublePoints:
                    DoublePointsTime = Balance.PowerUpDuration;
                    break;
                case PowerUpType.Nuke:
                    Fx.Flash(game.Player.transform.position, new Color(1f, 0.95f, 0.8f), 30f, 2.5f, 0.8f);
                    CameraRig.Shake(0.8f);
                    game.Rounds.KillAll(false);
                    game.AddPoints(Balance.NukePoints);
                    break;
                case PowerUpType.MaxAmmo:
                    game.Player.Inventory.MaxAmmo();
                    break;
            }
        }
    }

    /// <summary>Power-up no chão: gira, brilha, pisca antes de sumir.</summary>
    public class PowerUpPickup : MonoBehaviour
    {
        private PowerUpType type;
        private float life;
        private SpriteRenderer sr;
        private bool taken;

        public static void Spawn(PowerUpType type, Vector2 pos)
        {
            GameObject go = Res.Make("PowerUp_" + type, null, pos);
            go.layer = Layers.Pickup;
            PowerUpPickup p = go.AddComponent<PowerUpPickup>();
            p.type = type;
            p.life = Balance.PowerUpLifetime;
            p.sr = Res.AddSprite(go, "PowerUp" + type, Order.Item, false);
            CircleCollider2D c = go.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
            c.radius = 0.55f;
            LightKit.Point(go.transform, pos, 2.6f, 0.9f, new Color(0.5f, 1f, 0.45f));
            AudioManager.PlayAt("PowerUpSpawn", pos, 0.8f);
        }

        private void Update()
        {
            life -= Time.deltaTime;
            float bob = Mathf.Sin(Time.time * 4f) * 0.06f;
            sr.transform.localScale = Vector3.one * (1f + bob);
            if (life < 6f)
            {
                // pisca cada vez mais rápido antes de sumir
                float rate = life < 2.5f ? 14f : 6f;
                sr.enabled = Mathf.Sin(Time.time * rate) > -0.2f;
            }

            if (life <= 0f) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (taken || other.gameObject.layer != Layers.Player) return;
            taken = true;
            Game.I.PowerUps.Collect(type, transform.position);
            Destroy(gameObject);
        }
    }
}
