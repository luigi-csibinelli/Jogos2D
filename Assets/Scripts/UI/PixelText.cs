using UnityEngine;
using UnityEngine.UI;

namespace Sanatorio
{
    /// <summary>
    /// Texto em pixel art para uGUI: monta um quad por letra a partir do atlas
    /// da PixelFont. Funciona em canvas de tela e de mundo. Sombra opcional
    /// (1 px para baixo/direita) para legibilidade em cima do jogo.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class PixelText : MaskableGraphic
    {
        public enum Align { Left, Center, Right }

        [SerializeField] private string text = "";
        public float pixelSize = 1f;
        public Align align = Align.Left;
        public bool shadow = true;
        public Color shadowColor = new Color(0f, 0f, 0f, 0.85f);
        public float lineSpacing = 2f;

        public string Text
        {
            get => text;
            set
            {
                if (text != value)
                {
                    text = value ?? "";
                    SetVerticesDirty();
                }
            }
        }

        public override Texture mainTexture => PixelFont.Texture != null ? PixelFont.Texture : base.mainTexture;

        public Vector2 PreferredSize
        {
            get
            {
                string[] lines = text.Split('\n');
                int w = 0;
                foreach (string l in lines) w = Mathf.Max(w, PixelFont.MeasureLine(l));
                float h = lines.Length * PixelFont.CellH + (lines.Length - 1) * lineSpacing;
                return new Vector2(w * pixelSize, h * pixelSize);
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            Rect r = rectTransform.rect;
            string[] lines = text.Split('\n');
            float lineH = (PixelFont.CellH + lineSpacing) * pixelSize;
            float totalH = lines.Length * PixelFont.CellH * pixelSize + (lines.Length - 1) * lineSpacing * pixelSize;
            float top = r.center.y + totalH * 0.5f;

            if (shadow)
            {
                Emit(vh, lines, r, top, lineH, new Vector2(pixelSize, -pixelSize), shadowColor * new Color(1, 1, 1, color.a));
            }

            Emit(vh, lines, r, top, lineH, Vector2.zero, color);
        }

        private void Emit(VertexHelper vh, string[] lines, Rect r, float top, float lineH, Vector2 offset, Color col)
        {
            for (int li = 0; li < lines.Length; li++)
            {
                string line = lines[li];
                float w = PixelFont.MeasureLine(line) * pixelSize;
                float x = align == Align.Left ? r.xMin : align == Align.Right ? r.xMax - w : r.center.x - w * 0.5f;
                x = Mathf.Round(x / pixelSize) * pixelSize;
                float y = Mathf.Round((top - li * lineH) / pixelSize) * pixelSize;

                for (int i = 0; i < line.Length; i++)
                {
                    if (line[i] != ' ' && PixelFont.TryGlyph(line[i], out Rect uv))
                    {
                        float gx = x + i * PixelFont.CellW * pixelSize + offset.x;
                        float gy = y + offset.y;
                        float gw = PixelFont.CellW * pixelSize;
                        float gh = PixelFont.CellH * pixelSize;
                        int start = vh.currentVertCount;
                        vh.AddVert(new Vector3(gx, gy - gh), col, new Vector2(uv.xMin, uv.yMin));
                        vh.AddVert(new Vector3(gx, gy), col, new Vector2(uv.xMin, uv.yMax));
                        vh.AddVert(new Vector3(gx + gw, gy), col, new Vector2(uv.xMax, uv.yMax));
                        vh.AddVert(new Vector3(gx + gw, gy - gh), col, new Vector2(uv.xMax, uv.yMin));
                        vh.AddTriangle(start, start + 1, start + 2);
                        vh.AddTriangle(start + 2, start + 3, start);
                    }
                }
            }
        }
    }
}
