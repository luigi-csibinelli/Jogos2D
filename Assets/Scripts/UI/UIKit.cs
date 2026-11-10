using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Sanatorio
{
    /// <summary>
    /// Fábrica de UI em pixel art: canvas em escala inteira (pixels nítidos em
    /// qualquer resolução), painéis, rótulos, botões, sliders e alternadores
    /// navegáveis por mouse, teclado e controle.
    /// </summary>
    public static class UIKit
    {
        public static readonly Color Ink = new Color32(12, 10, 16, 255);
        public static readonly Color PanelColor = new Color32(24, 20, 30, 235);
        public static readonly Color Blood = new Color32(196, 32, 40, 255);
        public static readonly Color Bone = new Color32(232, 226, 210, 255);
        public static readonly Color Gold = new Color32(250, 206, 72, 255);
        public static readonly Color Dim = new Color32(140, 132, 128, 255);
        public static readonly Color Toxic = new Color32(120, 230, 90, 255);

        public static Canvas Canvas(string name, int order)
        {
            GameObject go = new GameObject(name);
            Canvas c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            c.pixelPerfect = true;
            CanvasScaler s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            go.AddComponent<IntegerScaler>();
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Image(Transform parent, string sprite, Color color, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            RectTransform rt = Rect("Img_" + sprite, parent, anchor, pivot ?? new Vector2(0.5f, 0.5f), pos, size);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite != null ? Res.Sprite(sprite) : null;
            img.color = color;
            img.raycastTarget = false;
            if (sprite == "Panel")
            {
                img.type = UnityEngine.UI.Image.Type.Sliced;
            }

            return img;
        }

        public static Image Panel(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color? color = null)
        {
            Image img = Image(parent, "Panel", color ?? Color.white, anchor, pos, size);
            img.raycastTarget = true;
            return img;
        }

        public static PixelText Label(Transform parent, string text, Vector2 anchor, Vector2 pos, Vector2 size, Color color,
                                      float scale = 1f, PixelText.Align align = PixelText.Align.Center, Vector2? pivot = null)
        {
            RectTransform rt = Rect("Text", parent, anchor, pivot ?? new Vector2(0.5f, 0.5f), pos, size);
            PixelText t = rt.gameObject.AddComponent<PixelText>();
            t.pixelSize = scale;
            t.align = align;
            t.color = color;
            t.Text = text;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(Transform parent, string text, Vector2 anchor, Vector2 pos, Vector2 size, Action onClick, float scale = 1f)
        {
            Image bg = Image(parent, "Panel", new Color(0.35f, 0.3f, 0.32f), anchor, pos, size);
            bg.raycastTarget = true;
            bg.gameObject.name = "Btn_" + text;
            Button b = bg.gameObject.AddComponent<Button>();
            ColorBlock cb = b.colors;
            cb.normalColor = new Color(0.55f, 0.5f, 0.52f);
            cb.highlightedColor = new Color(0.95f, 0.35f, 0.35f);
            cb.selectedColor = new Color(0.95f, 0.35f, 0.35f);
            cb.pressedColor = new Color(1f, 0.85f, 0.4f);
            cb.fadeDuration = 0.02f;
            b.colors = cb;
            b.targetGraphic = bg;
            PixelText label = Label(bg.transform, text, new Vector2(0.5f, 0.5f), Vector2.zero, size, Bone, scale);
            label.gameObject.name = "Label";
            b.onClick.AddListener(() =>
            {
                AudioManager.Play("UiClick", 0.8f);
                onClick?.Invoke();
            });
            bg.gameObject.AddComponent<UiSelectSound>();
            return b;
        }

        /// <summary>Linha "rótulo  < valor >" que muda com clique, setas ou analógico.</summary>
        public static OptionRow Option(Transform parent, string label, Vector2 anchor, Vector2 pos, float width, Func<string> value, Action<int> change)
        {
            Button b = Button(parent, label, anchor, pos, new Vector2(width, 14), () => change(1));
            PixelText txt = b.GetComponentInChildren<PixelText>();
            txt.align = PixelText.Align.Left;
            txt.rectTransform.offsetMin = new Vector2(6, 0);
            OptionRow row = b.gameObject.AddComponent<OptionRow>();
            row.Init(txt, label, value, change);
            return row;
        }

        public static void Vertical(Button[] buttons)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                Navigation n = new Navigation { mode = Navigation.Mode.Explicit };
                n.selectOnUp = buttons[(i - 1 + buttons.Length) % buttons.Length];
                n.selectOnDown = buttons[(i + 1) % buttons.Length];
                buttons[i].navigation = n;
            }
        }

        public static void Select(Selectable s)
        {
            if (s == null || EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(s.gameObject);
        }
    }

    /// <summary>Escala inteira da UI: 1 pixel da arte = N pixels da tela, sem borrar.</summary>
    public class IntegerScaler : MonoBehaviour
    {
        private CanvasScaler scaler;

        private void Awake() => scaler = GetComponent<CanvasScaler>();

        private void Update()
        {
            int s = Mathf.Max(1, Mathf.Min(Screen.width / 384, Screen.height / 216));
            if (Screen.height / 216f < 1f) s = 1;
            if (!Mathf.Approximately(scaler.scaleFactor, s)) scaler.scaleFactor = s;
        }
    }

    public class UiSelectSound : MonoBehaviour, ISelectHandler
    {
        public void OnSelect(BaseEventData e) => AudioManager.Play("UiMove", 0.4f);
    }

    /// <summary>Opção com valor: esquerda/direita alteram, clique avança.</summary>
    public class OptionRow : MonoBehaviour, IMoveHandler
    {
        private PixelText text;
        private string label;
        private Func<string> value;
        private Action<int> change;

        public void Init(PixelText t, string l, Func<string> v, Action<int> c)
        {
            text = t;
            label = l;
            value = v;
            change = c;
            Refresh();
            GetComponent<Button>().onClick.AddListener(Refresh);
        }

        public void Refresh() => text.Text = $"{label}  < {value()} >";

        public void OnMove(AxisEventData e)
        {
            if (e.moveDir == MoveDirection.Left || e.moveDir == MoveDirection.Right)
            {
                change(e.moveDir == MoveDirection.Right ? 1 : -1);
                AudioManager.Play("UiMove", 0.6f);
                Refresh();
            }
        }
    }
}
