using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sanatorio
{
    /// <summary>
    /// Menu principal: jogar (Normal / Insano), como jogar, opções, recordes,
    /// fichas encontradas e créditos. Ao fundo, zumbis arrastando os pés.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        private Canvas canvas;
        private RectTransform root;
        private GameObject mainPanel;
        private GameObject subPanel;
        private Selectable mainFirst;
        private readonly List<RectTransform> walkers = new List<RectTransform>();
        private readonly List<float> walkerSpeed = new List<float>();
        private Image flicker;

        private void Start()
        {
            Time.timeScale = 1f;
            Cursor.visible = true;
            GameEvents.Clear();
            AudioManager.Ensure();
            AudioManager.StopAmbience();
            AudioManager.Music("MusicMenu", 0.8f);
            UIKit.EnsureEventSystem();

            Camera cam = Camera.main;
            if (cam != null) cam.backgroundColor = UIKit.Ink;

            canvas = UIKit.Canvas("Menu", 10);
            root = UIKit.Stretch("Root", canvas.transform);
            Image bg = root.gameObject.AddComponent<Image>();
            bg.color = UIKit.Ink;

            BuildBackdrop();
            BuildMain();
            Scanlines.Create();
        }

        private void BuildBackdrop()
        {
            // janelas com luar e zumbis passando: o clima antes de clicar em jogar
            for (int i = 0; i < 5; i++)
            {
                UIKit.Image(root, "MenuWindow", new Color(1, 1, 1, 0.5f), new Vector2(0.1f + i * 0.2f, 0.5f), new Vector2(0, 50), new Vector2(32, 40));
            }

            for (int i = 0; i < 6; i++)
            {
                Image z = UIKit.Image(root, $"Zombie{i % 3}_0", new Color(0.55f, 0.55f, 0.6f), new Vector2(0f, 0f),
                    new Vector2(Random.Range(0f, 400f), 14 + (i % 3) * 6), new Vector2(16, 16));
                z.rectTransform.localRotation = Quaternion.Euler(0, 0, 0);
                walkers.Add(z.rectTransform);
                walkerSpeed.Add(Random.Range(6f, 14f));
            }

            flicker = UIKit.Image(root, null, new Color(0, 0, 0, 0), Vector2.zero, Vector2.zero, Vector2.zero);
            flicker.rectTransform.anchorMax = Vector2.one;
        }

        private void Update()
        {
            float w = root.rect.width;
            for (int i = 0; i < walkers.Count; i++)
            {
                RectTransform z = walkers[i];
                Vector2 p = z.anchoredPosition;
                p.x += walkerSpeed[i] * Time.unscaledDeltaTime;
                if (p.x > w + 20) p.x = -20;
                z.anchoredPosition = p;
                bool frame = Mathf.Repeat(Time.unscaledTime * 2f + i, 1f) < 0.5f;
                z.GetComponent<Image>().sprite = Res.Sprite($"Zombie{i % 3}_{(frame ? 0 : 1)}");
            }

            // lâmpada falhando
            float a = Random.value < 0.015f ? 0.6f : Mathf.MoveTowards(flicker.color.a, 0f, Time.unscaledDeltaTime * 3f);
            flicker.color = new Color(0, 0, 0, a);
        }

        private void BuildMain()
        {
            RectTransform panel = UIKit.Stretch("Main", root);
            mainPanel = panel.gameObject;

            UIKit.Label(panel, Lore.Title, new Vector2(0.5f, 1f), new Vector2(0, -32), new Vector2(380, 32), UIKit.Blood, 4f);
            UIKit.Label(panel, Lore.Subtitle, new Vector2(0.5f, 1f), new Vector2(0, -56), new Vector2(380, 10), UIKit.Bone);

            float y = 20f;
            Button normal = UIKit.Button(panel, "JOGAR - NORMAL", new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(150, 14), () => Play(Difficulty.Normal));
            Button insano = UIKit.Button(panel, "JOGAR - INSANO", new Vector2(0.5f, 0.5f), new Vector2(0, y - 17), new Vector2(150, 14), () => Play(Difficulty.Insano));
            Button how = UIKit.Button(panel, "COMO JOGAR", new Vector2(0.5f, 0.5f), new Vector2(0, y - 34), new Vector2(150, 14), () => Sub(BuildHowTo));
            Button opts = UIKit.Button(panel, "OPÇÕES", new Vector2(0.5f, 0.5f), new Vector2(0, y - 51), new Vector2(150, 14), ShowOptions);
            Button recs = UIKit.Button(panel, "RECORDES", new Vector2(0.5f, 0.5f), new Vector2(0, y - 68), new Vector2(150, 14), () => Sub(BuildRecords));
            Button files = UIKit.Button(panel, "ARQUIVOS", new Vector2(0.5f, 0.5f), new Vector2(0, y - 85), new Vector2(150, 14), () => Sub(BuildFiles));
            Button credits = UIKit.Button(panel, "CRÉDITOS", new Vector2(0.5f, 0.5f), new Vector2(0, y - 102), new Vector2(150, 14), () => Sub(BuildCredits));
            UIKit.Vertical(new[] { normal, insano, how, opts, recs, files, credits });
            mainFirst = normal;
            UIKit.Select(normal);

            UIKit.Label(panel, "INSANO: MAIS ZUMBIS, MAIS RÁPIDOS, CORRENDO DESDE A RODADA 1", new Vector2(0.5f, 0f), new Vector2(0, 7),
                new Vector2(380, 10), UIKit.Dim);
        }

        private void Play(Difficulty d)
        {
            Session.Difficulty = d;
            Session.Seed = 0;
            AudioManager.Sting("RoundFirst");
            SceneManager.LoadScene("Game");
        }

        private void Sub(System.Action<Transform> build)
        {
            mainPanel.SetActive(false);
            if (subPanel != null) Destroy(subPanel);
            Image panel = UIKit.Panel(root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320, 180), UIKit.PanelColor);
            subPanel = panel.gameObject;
            build(panel.transform);
            Button back = UIKit.Button(panel.transform, "VOLTAR", new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(100, 14), Back);
            UIKit.Select(back);
        }

        private void Back()
        {
            if (subPanel != null) Destroy(subPanel);
            mainPanel.SetActive(true);
            UIKit.Select(mainFirst);
        }

        private void ShowOptions()
        {
            mainPanel.SetActive(false);
            if (subPanel != null) Destroy(subPanel);
            subPanel = OptionsPanel.Build(root, Back, out Selectable first);
            UIKit.Select(first);
        }

        private static void BuildHowTo(Transform p)
        {
            UIKit.Label(p, "COMO JOGAR", new Vector2(0.5f, 1f), new Vector2(0, -12), new Vector2(300, 16), UIKit.Blood, 2f);
            string text =
                "SOBREVIVA ÀS RODADAS: ACERTOS E ABATES DÃO PONTOS.\n" +
                "PONTOS ABREM PORTAS E COMPRAM ARMAS DE PAREDE\n" +
                "E A CAIXA MISTERIOSA. O DISJUNTOR DO GERADOR LIGA\n" +
                "REFRIGERANTES, ARMADILHAS E A PORTA ENTRE AS ALAS.\n" +
                "SEGURE F NAS JANELAS PARA PREGAR TÁBUAS (+10).\n\n" +
                "MOVER   WASD / ANALÓGICO ESQUERDO\n" +
                "MIRAR   MOUSE / ANALÓGICO DIREITO\n" +
                "ATIRAR  CLIQUE / RT    FACA  V / RB\n" +
                "RECARREGAR  R / X    ARMAS  1 E 2 / Y\n" +
                "GRANADA  G / LB    BETTY  B / LT\n" +
                "USAR  F / A    PAUSA  ESC / START";
            UIKit.Label(p, text, new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(310, 130), UIKit.Bone);
        }

        private static void BuildRecords(Transform p)
        {
            UIKit.Label(p, "RECORDES", new Vector2(0.5f, 1f), new Vector2(0, -12), new Vector2(300, 16), UIKit.Blood, 2f);
            string text =
                $"NORMAL\nRODADA {SaveData.BestRound(Difficulty.Normal)}   ABATES {SaveData.BestKills(Difficulty.Normal)}   PONTOS {SaveData.BestPoints(Difficulty.Normal)}\n\n" +
                $"INSANO\nRODADA {SaveData.BestRound(Difficulty.Insano)}   ABATES {SaveData.BestKills(Difficulty.Insano)}   PONTOS {SaveData.BestPoints(Difficulty.Insano)}\n\n" +
                $"PARTIDAS JOGADAS {SaveData.GamesPlayed}";
            UIKit.Label(p, text, new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(300, 100), UIKit.Bone);
        }

        private static void BuildFiles(Transform p)
        {
            int found = SaveData.NotesFoundCount;
            UIKit.Label(p, $"ARQUIVOS {found}/{Lore.Notes.Length}", new Vector2(0.5f, 1f), new Vector2(0, -12), new Vector2(300, 16), UIKit.Blood, 2f);
            string list = "";
            for (int i = 0; i < Lore.Notes.Length; i++)
            {
                list += (SaveData.NoteFound(i) ? Lore.NoteTitles[i] : $"FICHA {i + 1:00} - ???") + "\n";
            }

            UIKit.Label(p, list, new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(300, 80), UIKit.Bone);
            string bottom = found == Lore.Notes.Length ? Lore.Epilogue : Lore.Intro;
            UIKit.Label(p, bottom, new Vector2(0.5f, 0.5f), new Vector2(0, -38), new Vector2(310, 50), found == Lore.Notes.Length ? UIKit.Gold : UIKit.Dim);
        }

        private static void BuildCredits(Transform p)
        {
            UIKit.Label(p, "CRÉDITOS", new Vector2(0.5f, 1f), new Vector2(0, -12), new Vector2(300, 16), UIKit.Blood, 2f);
            UIKit.Label(p, Lore.Credits, new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(300, 50), UIKit.Bone);
            UIKit.Label(p, Lore.Intro, new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(310, 60), UIKit.Dim);
        }
    }
}
