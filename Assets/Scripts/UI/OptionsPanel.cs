using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sanatorio
{
    /// <summary>Painel de opções, usado no menu principal e na pausa.</summary>
    public static class OptionsPanel
    {
        public static GameObject Build(Transform parent, Action onBack, out Selectable first)
        {
            Image panel = UIKit.Panel(parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(230, 150), UIKit.PanelColor);
            Transform p = panel.transform;
            UIKit.Label(p, "OPÇÕES", new Vector2(0.5f, 1f), new Vector2(0, -12), new Vector2(200, 16), UIKit.Blood, 2f);

            float x = 0f, y = 38f, w = 200f;
            OptionRow music = UIKit.Option(p, "MÚSICA", new Vector2(0.5f, 0.5f), new Vector2(x, y), w,
                () => Bar(SaveData.MusicVolume),
                d => { SaveData.MusicVolume = Mathf.Clamp01(Mathf.Round((SaveData.MusicVolume + d * 0.1f) * 10f) / 10f); SaveData.Save(); });
            OptionRow sfx = UIKit.Option(p, "EFEITOS", new Vector2(0.5f, 0.5f), new Vector2(x, y - 17), w,
                () => Bar(SaveData.SfxVolume),
                d => { SaveData.SfxVolume = Mathf.Clamp01(Mathf.Round((SaveData.SfxVolume + d * 0.1f) * 10f) / 10f); SaveData.Save(); AudioManager.Play("Hit", 0.8f); });
            OptionRow shake = UIKit.Option(p, "TREMOR DE TELA", new Vector2(0.5f, 0.5f), new Vector2(x, y - 34), w,
                () => SaveData.ScreenShake ? "SIM" : "NÃO",
                d => { SaveData.ScreenShake = !SaveData.ScreenShake; SaveData.Save(); });
            OptionRow scan = UIKit.Option(p, "LINHAS DE TV", new Vector2(0.5f, 0.5f), new Vector2(x, y - 51), w,
                () => SaveData.Scanlines ? "SIM" : "NÃO",
                d => { SaveData.Scanlines = !SaveData.Scanlines; SaveData.Save(); Scanlines.Refresh(); });
            OptionRow assist = UIKit.Option(p, "MIRA ASSISTIDA (CONTROLE)", new Vector2(0.5f, 0.5f), new Vector2(x, y - 68), w,
                () => SaveData.AimAssist ? "SIM" : "NÃO",
                d => { SaveData.AimAssist = !SaveData.AimAssist; SaveData.Save(); });
            Button full = UIKit.Button(p, "TELA CHEIA", new Vector2(0.5f, 0.5f), new Vector2(x, y - 85), new Vector2(w, 14),
                () => Screen.fullScreen = !Screen.fullScreen);
            Button back = UIKit.Button(p, "VOLTAR", new Vector2(0.5f, 0.5f), new Vector2(x, y - 104), new Vector2(w, 14), onBack);

            UIKit.Vertical(new[]
            {
                music.GetComponent<Button>(), sfx.GetComponent<Button>(), shake.GetComponent<Button>(),
                scan.GetComponent<Button>(), assist.GetComponent<Button>(), full, back,
            });
            first = music.GetComponent<Button>();
            return panel.gameObject;
        }

        private static string Bar(float v)
        {
            int n = Mathf.RoundToInt(v * 10f);
            return new string('•', n) + new string('-', 10 - n);
        }
    }

    /// <summary>Linhas de TV de tubo por cima de tudo (opcional nas opções).</summary>
    public class Scanlines : MonoBehaviour
    {
        private static Scanlines instance;
        private RawImage img;

        public static void Create()
        {
            Canvas c = UIKit.Canvas("Scanlines", 500);
            RectTransform rt = UIKit.Stretch("Lines", c.transform);
            instance = c.gameObject.AddComponent<Scanlines>();
            instance.img = rt.gameObject.AddComponent<RawImage>();
            instance.img.texture = Res.Sprite("Scanline").texture;
            instance.img.texture.wrapMode = TextureWrapMode.Repeat;
            instance.img.raycastTarget = false;
            instance.img.color = new Color(1, 1, 1, 0.22f);
            Refresh();
        }

        public static void Refresh()
        {
            if (instance != null) instance.img.enabled = SaveData.Scanlines;
        }

        private void Update()
        {
            // 1 linha a cada 2 pixels da tela virtual, rolando devagar
            float rows = 216f / 2f;
            img.uvRect = new Rect(0, Time.unscaledTime * 0.02f, 1, rows);
        }
    }
}
