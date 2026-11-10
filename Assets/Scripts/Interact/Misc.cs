using System.Collections.Generic;
using UnityEngine;

namespace Sanatorio
{
    /// <summary>
    /// Janela com 6 tábuas. Zumbis arrancam uma por vez; o jogador segura o
    /// botão para pregar de volta (+10 por tábua, com limite por rodada).
    /// </summary>
    public class Barricade : Interactable
    {
        public const int MaxPlanks = 6;
        public MapData.Window window;
        public int Planks { get; private set; } = MaxPlanks;
        private readonly List<SpriteRenderer> planks = new List<SpriteRenderer>();
        private float holdTimer;

        public override bool Hold => true;

        public void Setup(MapData.Window w, List<SpriteRenderer> plankSprites)
        {
            window = w;
            planks.AddRange(plankSprites);
            Refresh();
        }

        public override string Prompt(Player p, out int cost)
        {
            cost = -1;
            return Planks >= MaxPlanks ? null : $"SEGURE {Key(p)} - RECONSTRUIR JANELA";
        }

        public override void HoldTick(Player p, float dt)
        {
            if (Planks >= MaxPlanks) return;
            float interval = 0.45f * (p.HasPerk(PerkType.TurboCola) ? 0.5f : 1f);
            holdTimer += dt;
            if (holdTimer < interval) return;
            holdTimer = 0f;

            Planks++;
            Refresh();
            AudioManager.PlayAt("Plank", transform.position, 0.9f, 0.12f);
            Fx.Dust(transform.position, 5, new Color(0.55f, 0.4f, 0.25f));
            if (game.PlankPointsThisRound < Balance.PlankPointsCapPerRound)
            {
                game.PlankPointsThisRound += Balance.PointsPerPlank;
                game.AddPoints(Balance.PointsPerPlank);
            }
        }

        public void Tear()
        {
            if (Planks <= 0) return;
            Planks--;
            Refresh();
            AudioManager.PlayAt("PlankBreak", transform.position, 0.9f, 0.12f);
            Fx.Dust(transform.position, 8, new Color(0.55f, 0.4f, 0.25f));
            if (game.World != null) game.World.AnyBarricadeBroken = true;
        }

        private void Refresh()
        {
            for (int i = 0; i < planks.Count; i++)
            {
                planks[i].enabled = i < Planks;
            }
        }
    }

    /// <summary>Privada do andar de cima. Dar descarga 3x na da esquerda toca a música secreta.</summary>
    public class Toilet : Interactable
    {
        public bool secret;
        private int flushes;
        private float cooldown;
        private static bool songPlayed;

        public override string Prompt(Player p, out int cost)
        {
            cost = -1;
            return Time.time < cooldown ? null : $"{Key(p)} - DAR DESCARGA";
        }

        public override void Use(Player p)
        {
            if (Time.time < cooldown) return;
            cooldown = Time.time + 1.2f;
            AudioManager.PlayAt("Flush", transform.position, 1f, 0.05f);
            Fx.Burst(transform.position, new Color(0.5f, 0.7f, 1f), 8, 1.5f, 0.5f, 1.5f / 16f);
            if (!secret || songPlayed) return;
            flushes++;
            if (flushes >= 3)
            {
                songPlayed = true;
                GameEvents.RaiseToast("UMA CANÇÃO TOCA NOS CANOS...");
                AudioManager.Music("MusicSecret", 0.9f);
            }
        }

        public static void ResetSecret() => songPlayed = false;
    }

    /// <summary>Ficha de paciente: lore do sanatório, guardada entre partidas.</summary>
    public class LoreNote : Interactable
    {
        public int index;

        public override string Prompt(Player p, out int cost)
        {
            cost = -1;
            return $"{Key(p)} - LER FICHA {index + 1}/{Lore.Notes.Length}";
        }

        public override void Use(Player p)
        {
            bool first = !SaveData.NoteFound(index);
            SaveData.MarkNote(index);
            AudioManager.Play("Paper", 1f);
            game.Hud.ShowNote(index, first);
            Destroy(gameObject);
        }
    }
}
