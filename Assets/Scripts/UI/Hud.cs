using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sanatorio
{
    /// <summary>
    /// HUD estilo WaW em pixel art: rodada em marcas de giz vermelho, pontos com
    /// os ganhos subindo, munição, refrigerantes, power-ups com tempo, avisos de
    /// interação, dano na borda da tela e os painéis de pausa / ficha / fim.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        private Game game;
        private Canvas canvas;
        private RectTransform root;

        private PixelText points;
        private PixelText weaponName;
        private PixelText otherWeapon;
        private PixelText ammo;
        private Image gunIcon;
        private Image crosshair;
        private int lastHits;
        private float hitMark;
        private PixelText lethals;
        private PixelText prompt;
        private PixelText toast;
        private PixelText hint;
        private PixelText roundNumber;
        private RectTransform tallies;
        private readonly List<Image> tallyMarks = new List<Image>();
        private readonly List<Image> perkIcons = new List<Image>();
        private readonly List<Image> powerIcons = new List<Image>();
        private readonly List<PixelText> powerTimes = new List<PixelText>();
        private Image vignette;
        private Image bar;
        private Image barBack;
        private RectTransform deltas;
        private readonly List<PixelText> deltaPool = new List<PixelText>();
        private readonly List<float> deltaLife = new List<float>();

        private GameObject pausePanel;
        private GameObject optionsPanel;
        private GameObject notePanel;
        private GameObject gameOverPanel;
        private Selectable pauseFirst;
        private PixelText pauseStats;

        private float toastTime;
        private float hintTime;
        private float hurt;
        private float roundFlash;
        private int shownRound = -1;

        public static Hud Create(Game game)
        {
            UIKit.EnsureEventSystem();
            Canvas c = UIKit.Canvas("HUD", 100);
            Hud h = c.gameObject.AddComponent<Hud>();
            h.game = game;
            h.canvas = c;
            h.Build();
            Scanlines.Create();
            return h;
        }

        private void Build()
        {
            root = UIKit.Stretch("Root", canvas.transform);

            vignette = UIKit.Image(root, "Vignette", new Color(1, 1, 1, 0), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            RectTransform vr = vignette.rectTransform;
            vr.anchorMin = Vector2.zero;
            vr.anchorMax = Vector2.one;
            vr.sizeDelta = Vector2.zero;

            // canto inferior esquerdo: rodada
            tallies = UIKit.Rect("Tallies", root, Vector2.zero, Vector2.zero, new Vector2(8, 8), new Vector2(120, 24));
            for (int i = 0; i < 5; i++)
            {
                Image m = UIKit.Image(tallies, i < 4 ? "Tally" : "TallySlash", UIKit.Blood, Vector2.zero,
                    i < 4 ? new Vector2(4 + i * 7, 0) : new Vector2(2, 2), i < 4 ? new Vector2(5, 22) : new Vector2(28, 18), Vector2.zero);
                tallyMarks.Add(m);
            }

            roundNumber = UIKit.Label(root, "", Vector2.zero, new Vector2(8, 6), new Vector2(120, 24), UIKit.Blood, 3f, PixelText.Align.Left, Vector2.zero);

            // canto inferior direito: pontos, arma, munição
            points = UIKit.Label(root, "500", new Vector2(1, 0), new Vector2(-8, 50), new Vector2(140, 16), UIKit.Gold, 2f, PixelText.Align.Right, new Vector2(1, 0));
            deltas = UIKit.Rect("Deltas", root, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-8, 68), new Vector2(80, 60));
            weaponName = UIKit.Label(root, "", new Vector2(1, 0), new Vector2(-8, 34), new Vector2(160, 10), UIKit.Bone, 1f, PixelText.Align.Right, new Vector2(1, 0));
            otherWeapon = UIKit.Label(root, "", new Vector2(1, 0), new Vector2(-8, 34), new Vector2(300, 10), UIKit.Dim, 1f, PixelText.Align.Right, new Vector2(1, 0));
            ammo = UIKit.Label(root, "", new Vector2(1, 0), new Vector2(-8, 18), new Vector2(160, 16), UIKit.Bone, 2f, PixelText.Align.Right, new Vector2(1, 0));
            // silhueta da arma ao lado da munição: separa armas parecidas (Kar98k x Springfield)
            gunIcon = UIKit.Image(root, "Gun_m1911", Color.white, new Vector2(1, 0), new Vector2(-80, 17), new Vector2(60, 20), new Vector2(1, 0));
            gunIcon.preserveAspect = true;
            lethals = UIKit.Label(root, "", new Vector2(1, 0), new Vector2(-8, 6), new Vector2(160, 10), UIKit.Dim, 1f, PixelText.Align.Right, new Vector2(1, 0));

            // refrigerantes acima da rodada
            for (int i = 0; i < 4; i++)
            {
                Image icon = UIKit.Image(root, "PerkFerroNog", Color.white, Vector2.zero, new Vector2(8 + i * 18, 36), new Vector2(16, 16), Vector2.zero);
                icon.enabled = false;
                perkIcons.Add(icon);
            }

            // power-ups ativos no topo
            for (int i = 0; i < 2; i++)
            {
                Image icon = UIKit.Image(root, "PowerUpInstaKill", Color.white, new Vector2(0.5f, 1f), new Vector2(-12 + i * 24, -14), new Vector2(16, 16));
                icon.enabled = false;
                powerIcons.Add(icon);
                PixelText t = UIKit.Label(root, "", new Vector2(0.5f, 1f), new Vector2(-12 + i * 24, -28), new Vector2(24, 8), UIKit.Toxic);
                powerTimes.Add(t);
            }

            prompt = UIKit.Label(root, "", new Vector2(0.5f, 0f), new Vector2(0, 46), new Vector2(380, 10), UIKit.Bone);

            // mira: segue o mouse (ou o ponto mirado no controle); fica vermelha ao acertar
            crosshair = UIKit.Image(root, "Crosshair", Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(11, 11));
            toast = UIKit.Label(root, "", new Vector2(0.5f, 0.5f), new Vector2(0, 52), new Vector2(380, 16), UIKit.Bone, 2f);
            hint = UIKit.Label(root, "", new Vector2(0.5f, 1f), new Vector2(0, -44), new Vector2(380, 10), UIKit.Gold);

            barBack = UIKit.Image(root, null, new Color(0, 0, 0, 0.7f), new Vector2(0.5f, 0.5f), new Vector2(0, -16), new Vector2(26, 4));
            bar = UIKit.Image(root, null, UIKit.Bone, new Vector2(0.5f, 0.5f), new Vector2(-12, -16), new Vector2(24, 2), new Vector2(0, 0.5f));
            barBack.enabled = bar.enabled = false;

            game.Player.Inventory.Changed += RefreshInventory;
            GameEvents.PointsChanged += OnPoints;
            GameEvents.Toast += Toast;
            GameEvents.PlayerHurt += hp => { if (hp < game.Player.MaxHealth) hurt = 1f; };
            GameEvents.RoundStarted += r => roundFlash = 2.5f;
            GameEvents.RoundEnded += r => roundFlash = 2.5f;
            RefreshInventory();
        }

        private void OnDestroy()
        {
            if (game != null && game.Player != null) game.Player.Inventory.Changed -= RefreshInventory;
        }

        // ------------------------------------------------------------ eventos
        private void OnPoints(int total, int delta)
        {
            points.Text = total.ToString();
            if (delta == 0) return;

            PixelText t = null;
            int idx = -1;
            for (int i = 0; i < deltaPool.Count; i++)
            {
                if (deltaLife[i] <= 0f)
                {
                    t = deltaPool[i];
                    idx = i;
                    break;
                }
            }

            if (t == null)
            {
                t = UIKit.Label(deltas, "", new Vector2(1, 0), Vector2.zero, new Vector2(80, 10), UIKit.Gold, 1f, PixelText.Align.Right, new Vector2(1, 0));
                deltaPool.Add(t);
                deltaLife.Add(0f);
                idx = deltaPool.Count - 1;
            }

            t.Text = (delta > 0 ? "+" : "") + delta;
            t.color = delta > 0 ? UIKit.Gold : UIKit.Blood;
            t.rectTransform.anchoredPosition = new Vector2(Random.Range(-14f, 0f), 0f);
            deltaLife[idx] = 1f;
        }

        public void Toast(string msg)
        {
            toast.Text = msg;
            toastTime = 2.6f;
        }

        public void Hint(string msg)
        {
            hint.Text = msg;
            hintTime = 6f;
            AudioManager.Play("UiMove", 0.5f);
        }

        private void RefreshInventory()
        {
            Player p = game.Player;
            Inventory inv = p.Inventory;
            WeaponState w = inv.Current;
            if (w != null)
            {
                weaponName.Text = $"{inv.CurrentIndex + 1} {w.Name}";
                weaponName.color = w.upgraded ? new Color(0.85f, 0.55f, 1f) : UIKit.Bone;
                // a outra arma aparece apagada à esquerda, com a tecla que a seleciona
                string other = "";
                for (int s = 0; s < inv.Weapons.Count; s++)
                {
                    if (s != inv.CurrentIndex) other = $"{s + 1} {inv.Weapons[s].Name}";
                }

                otherWeapon.Text = other;
                otherWeapon.rectTransform.anchoredPosition = new Vector2(-8 - PixelFont.MeasureLine(weaponName.Text) - 10, 34);
                gunIcon.sprite = Res.Sprite(w.def.Icon);
                UpdateAmmo(w);
            }

            string lethalName = inv.LethalId == "molotov" ? "MOLOTOV" : "GRANADAS";
            lethals.Text = $"{lethalName} x{inv.Lethals}" + (inv.HasBetties ? $"   BETTY x{inv.Betties}" : "");

            int i = 0;
            foreach (PerkType perk in new[] { PerkType.FerroNog, PerkType.ReviveJa, PerkType.TurboCola, PerkType.GatilhoDuplo })
            {
                if (!inv.Perks.Contains(perk)) continue;
                perkIcons[i].sprite = Res.Sprite("Perk" + perk);
                perkIcons[i].enabled = true;
                i++;
            }

            for (; i < perkIcons.Count; i++) perkIcons[i].enabled = false;
        }

        // ------------------------------------------------------------ quadro a quadro
        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            Player p = game.Player;
            UpdateCrosshair(p, dt);

            // rodada: marcas de giz até 5, número depois disso
            int r = game.Rounds != null ? game.Rounds.Round : 0;
            if (r != shownRound)
            {
                shownRound = r;
                for (int i = 0; i < tallyMarks.Count; i++)
                {
                    tallyMarks[i].enabled = r <= 5 && (i < 4 ? i < Mathf.Min(r, 4) : r >= 5);
                }

                roundNumber.Text = r > 5 ? r.ToString() : "";
            }

            if (roundFlash > 0f)
            {
                roundFlash -= dt;
                Color c = Mathf.Repeat(roundFlash, 0.5f) < 0.25f ? UIKit.Bone : UIKit.Blood;
                foreach (Image m in tallyMarks) m.color = c;
                roundNumber.color = c;
            }
            else
            {
                foreach (Image m in tallyMarks) m.color = UIKit.Blood;
                roundNumber.color = UIKit.Blood;
            }

            // aviso de interação com preço (vermelho se não dá para pagar)
            if (p.Focus != null && game.IsPlaying)
            {
                string text = p.Focus.Prompt(p, out int cost);
                prompt.Text = text == null ? "" : cost >= 0 ? $"{text}  [{cost}]" : text;
                prompt.color = cost > game.Points ? UIKit.Blood : UIKit.Bone;
            }
            else
            {
                prompt.Text = "";
            }

            // barra de recarga / reconstrução / levantar
            float progress = p.Downed ? p.HoldProgress : p.Gun.Reloading ? p.Gun.ReloadProgress : 0f;
            bool show = progress > 0f && game.IsPlaying;
            barBack.enabled = bar.enabled = show;
            if (show)
            {
                Camera cam = Camera.main;
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, p.transform.position + Vector3.up * 0.9f);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out Vector2 local);
                barBack.rectTransform.anchoredPosition = local;
                bar.rectTransform.anchoredPosition = local + new Vector2(-12, 0);
                bar.rectTransform.sizeDelta = new Vector2(24 * progress, 2);
                bar.color = p.Downed ? new Color(0.5f, 0.7f, 1f) : UIKit.Bone;
            }

            // power-ups
            PowerUpManager pu = game.PowerUps;
            int slot = 0;
            if (pu.InstaKill) SetPower(slot++, "PowerUpInstaKill", pu.InstaKillTime);
            if (pu.DoublePoints) SetPower(slot++, "PowerUpDoublePoints", pu.DoublePointsTime);
            for (; slot < powerIcons.Count; slot++)
            {
                powerIcons[slot].enabled = false;
                powerTimes[slot].Text = "";
            }

            // pontos subindo e sumindo
            for (int i = 0; i < deltaPool.Count; i++)
            {
                if (deltaLife[i] <= 0f) continue;
                deltaLife[i] -= dt;
                PixelText t = deltaPool[i];
                t.rectTransform.anchoredPosition += new Vector2(0, 24f * dt);
                t.color = new Color(t.color.r, t.color.g, t.color.b, Mathf.Clamp01(deltaLife[i] * 2f));
                if (deltaLife[i] <= 0f) t.Text = "";
            }

            if (toastTime > 0f)
            {
                toastTime -= dt;
                toast.color = new Color(UIKit.Bone.r, UIKit.Bone.g, UIKit.Bone.b, Mathf.Clamp01(toastTime * 2f));
            }

            if (hintTime > 0f)
            {
                hintTime -= dt;
                hint.color = new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, Mathf.Clamp01(hintTime));
            }

            // borda vermelha: dano recente + vida baixa pulsando
            hurt = Mathf.Max(0f, hurt - dt * 1.6f);
            float low = 1f - (float)p.Health / p.MaxHealth;
            float pulse = low > 0.3f ? low * (0.55f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f)) : 0f;
            vignette.color = new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Max(hurt, pulse)));

            if (p.Inventory.Current != null) UpdateAmmo(p.Inventory.Current);

            if (game.State == GameState.Reading && notePanel != null && p.Input.Interact.WasPressedThisFrame())
            {
                CloseNote();
            }
        }

        private void UpdateCrosshair(Player p, float dt)
        {
            bool playing = game.State == GameState.Playing && !p.Dead;
            Cursor.visible = !playing;
            crosshair.enabled = playing && !p.Downed;
            if (!crosshair.enabled) return;

            if (game.ShotsHit != lastHits)
            {
                lastHits = game.ShotsHit;
                hitMark = 0.12f;
            }

            hitMark -= dt;
            Camera cam = Camera.main;
            Vector2 screen = p.Input.UsingGamepad && cam != null
                ? (Vector2)cam.WorldToScreenPoint(p.AimPoint)
                : p.Input.Point.ReadValue<Vector2>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out Vector2 local);
            crosshair.rectTransform.anchoredPosition = new Vector2(Mathf.Round(local.x), Mathf.Round(local.y));

            WeaponState w = p.Inventory.Current;
            bool cantShoot = p.Gun.Reloading || (w != null && (w.overheated || (!w.def.overheat && w.mag == 0)));
            crosshair.color = hitMark > 0f ? UIKit.Blood : cantShoot ? new Color(1f, 1f, 1f, 0.35f) : new Color(1f, 1f, 1f, 0.9f);
            crosshair.rectTransform.localScale = Vector3.one * (hitMark > 0f ? 1.25f : 1f);
        }

        /// <summary>Munição (ou calor, no lança-chamas) e posição da silhueta à esquerda do texto.</summary>
        private void UpdateAmmo(WeaponState w)
        {
            if (w.def.overheat)
            {
                if (w.overheated)
                {
                    ammo.Text = "SUPERAQUECIDO";
                    ammo.color = Mathf.Repeat(Time.unscaledTime, 0.4f) < 0.2f ? UIKit.Gold : UIKit.Blood;
                }
                else
                {
                    int bars = Mathf.CeilToInt(w.heat * 8f);
                    ammo.Text = "CALOR " + new string('•', bars) + new string('-', 8 - bars);
                    ammo.color = w.heat > 0.75f ? UIKit.Blood : w.heat > 0.4f ? UIKit.Gold : UIKit.Bone;
                }
            }
            else
            {
                ammo.Text = $"{w.mag} / {w.reserve}";
                ammo.color = w.mag == 0 ? UIKit.Blood : w.mag <= w.MagSize / 4 ? UIKit.Gold : UIKit.Bone;
            }

            float textW = PixelFont.MeasureLine(ammo.Text) * ammo.pixelSize;
            gunIcon.rectTransform.anchoredPosition = new Vector2(-8 - textW - 6, 17);
        }

        private void SetPower(int slot, string sprite, float time)
        {
            powerIcons[slot].sprite = Res.Sprite(sprite);
            powerIcons[slot].enabled = time > 5f || Mathf.Repeat(Time.unscaledTime, 0.3f) < 0.18f;
            powerTimes[slot].Text = Mathf.CeilToInt(time).ToString();
        }

        // ------------------------------------------------------------ pausa
        public void ShowPause(bool show)
        {
            if (show)
            {
                if (pausePanel == null) BuildPause();
                pauseStats.Text = $"RODADA {game.Rounds.Round}  -  {game.Kills} ABATES";
                toastTime = 0f;
                toast.Text = "";
                hintTime = 0f;
                hint.Text = "";
                pausePanel.SetActive(true);
                if (optionsPanel != null) optionsPanel.SetActive(false);
                UIKit.Select(pauseFirst);
            }
            else if (pausePanel != null)
            {
                pausePanel.SetActive(false);
                if (optionsPanel != null) optionsPanel.SetActive(false);
            }
        }

        private void BuildPause()
        {
            RectTransform layer = UIKit.Stretch("Pause", canvas.transform);
            Image dim = layer.gameObject.AddComponent<Image>();
            dim.color = new Color(0, 0, 0, 0.6f);
            pausePanel = layer.gameObject;

            Image panel = UIKit.Panel(layer, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(170, 130), UIKit.PanelColor);
            UIKit.Label(panel.transform, "PAUSA", new Vector2(0.5f, 1f), new Vector2(0, -14), new Vector2(160, 16), UIKit.Blood, 2f);
            pauseStats = UIKit.Label(panel.transform, "", new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(160, 10), UIKit.Dim);

            Button resume = UIKit.Button(panel.transform, "CONTINUAR", new Vector2(0.5f, 0.5f), new Vector2(0, 14), new Vector2(130, 14), () => game.SetPaused(false));
            Button opts = UIKit.Button(panel.transform, "OPÇÕES", new Vector2(0.5f, 0.5f), new Vector2(0, -4), new Vector2(130, 14), ShowOptions);
            Button restart = UIKit.Button(panel.transform, "RECOMEÇAR", new Vector2(0.5f, 0.5f), new Vector2(0, -22), new Vector2(130, 14), game.Restart);
            Button menu = UIKit.Button(panel.transform, "MENU PRINCIPAL", new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(130, 14), game.QuitToMenu);
            UIKit.Vertical(new[] { resume, opts, restart, menu });
            pauseFirst = resume;
        }

        private void ShowOptions()
        {
            if (optionsPanel == null)
            {
                optionsPanel = OptionsPanel.Build(pausePanel.transform, () =>
                {
                    optionsPanel.SetActive(false);
                    UIKit.Select(pauseFirst);
                }, out Selectable first);
                optionsPanel.AddComponent<FirstSelect>().first = first;
            }

            optionsPanel.SetActive(true);
            UIKit.Select(optionsPanel.GetComponent<FirstSelect>().first);
        }

        // ------------------------------------------------------------ fichas
        public void ShowNote(int index, bool first)
        {
            game.BeginReading();
            if (notePanel != null) Destroy(notePanel);
            RectTransform layer = UIKit.Stretch("Note", canvas.transform);
            Image dim = layer.gameObject.AddComponent<Image>();
            dim.color = new Color(0, 0, 0, 0.7f);
            notePanel = layer.gameObject;

            Image paper = UIKit.Image(layer, "Panel", new Color(0.86f, 0.8f, 0.66f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280, 110));
            UIKit.Label(paper.transform, Lore.NoteTitles[index], new Vector2(0.5f, 1f), new Vector2(0, -12), new Vector2(260, 10), new Color(0.5f, 0.1f, 0.1f));
            PixelText body = UIKit.Label(paper.transform, Lore.Notes[index], new Vector2(0.5f, 0.5f), new Vector2(0, -2), new Vector2(260, 60),
                new Color(0.15f, 0.12f, 0.1f));
            body.shadow = false;
            int found = SaveData.NotesFoundCount;
            string footer = first ? $"NOVA FICHA! {found}/{Lore.Notes.Length} ENCONTRADAS" : $"{found}/{Lore.Notes.Length} ENCONTRADAS";
            UIKit.Label(paper.transform, footer, new Vector2(0.5f, 0f), new Vector2(0, 18), new Vector2(260, 10), new Color(0.35f, 0.3f, 0.25f)).shadow = false;
            UIKit.Label(paper.transform, $"{game.Player.Input.InteractLabel} - FECHAR", new Vector2(0.5f, 0f), new Vector2(0, 8), new Vector2(260, 10),
                new Color(0.5f, 0.1f, 0.1f)).shadow = false;
            if (found == Lore.Notes.Length && first) Toast("TODAS AS FICHAS! VEJA O EPÍLOGO NO MENU");
        }

        private void CloseNote()
        {
            Destroy(notePanel);
            notePanel = null;
            game.EndReading();
        }

        // ------------------------------------------------------------ fim de jogo
        public void ShowGameOver(bool record)
        {
            RectTransform layer = UIKit.Stretch("GameOver", canvas.transform);
            Image dim = layer.gameObject.AddComponent<Image>();
            dim.color = new Color(0.12f, 0, 0, 0.75f);
            gameOverPanel = layer.gameObject;

            int round = game.Rounds.Round;
            UIKit.Label(layer, "FIM DE JOGO", new Vector2(0.5f, 0.5f), new Vector2(0, 64), new Vector2(380, 24), UIKit.Blood, 3f);
            UIKit.Label(layer, $"VOCÊ SOBREVIVEU {round} RODADA{(round == 1 ? "" : "S")}", new Vector2(0.5f, 0.5f), new Vector2(0, 38),
                new Vector2(380, 16), UIKit.Bone, 2f);

            int secs = Mathf.FloorToInt(game.Elapsed);
            string stats =
                $"ABATES {game.Kills}     PONTOS {game.TotalPointsEarned}\n" +
                $"PRECISÃO {Mathf.RoundToInt(game.Accuracy * 100f)}%     TEMPO {secs / 60}:{secs % 60:00}\n" +
                $"FICHAS {SaveData.NotesFoundCount}/{Lore.Notes.Length}     RECORDE {SaveData.BestRound(game.Difficulty)} RODADAS";
            UIKit.Label(layer, stats, new Vector2(0.5f, 0.5f), new Vector2(0, 8), new Vector2(380, 40), UIKit.Dim);
            if (record)
            {
                UIKit.Label(layer, "NOVO RECORDE!", new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(380, 16), UIKit.Gold, 2f);
            }

            Button again = UIKit.Button(layer, "JOGAR DE NOVO", new Vector2(0.5f, 0.5f), new Vector2(-70, -44), new Vector2(130, 16), game.Restart);
            Button menu = UIKit.Button(layer, "MENU", new Vector2(0.5f, 0.5f), new Vector2(70, -44), new Vector2(130, 16), game.QuitToMenu);
            Navigation na = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = menu, selectOnLeft = menu };
            again.navigation = na;
            Navigation nm = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = again, selectOnLeft = again };
            menu.navigation = nm;
            UIKit.Select(again);
        }
    }

    public class FirstSelect : MonoBehaviour
    {
        public Selectable first;
    }
}
