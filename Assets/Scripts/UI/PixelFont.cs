using System.Collections.Generic;
using UnityEngine;

namespace Sanatorio
{
    /// <summary>
    /// Fonte bitmap 5x7 (célula 6x8) gerada por Tools/gerar_sprites.py.
    /// Só maiúsculas, como nos jogos de 8 bits; minúsculas viram maiúsculas.
    /// </summary>
    public static class PixelFont
    {
        public const string Charset =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_ÁÀÂÃÉÊÍÓÔÕÚÇÜ×•";

        public const int CellW = 6;
        public const int CellH = 8;
        public const int Columns = 16;

        private static Texture2D texture;
        private static Dictionary<char, int> index;

        public static Texture2D Texture
        {
            get
            {
                if (texture == null)
                {
                    texture = Resources.Load<Texture2D>("Art/Font");
                    if (texture != null)
                    {
                        texture.filterMode = FilterMode.Point;
                    }
                }

                return texture;
            }
        }

        public static int Rows => (Charset.Length + Columns - 1) / Columns;

        public static bool TryGlyph(char c, out Rect uv)
        {
            if (index == null)
            {
                index = new Dictionary<char, int>();
                for (int i = 0; i < Charset.Length; i++)
                {
                    index[Charset[i]] = i;
                }
            }

            char u = char.ToUpperInvariant(c);
            if (!index.TryGetValue(u, out int i2))
            {
                uv = default;
                return false;
            }

            float tw = Columns * CellW;
            float th = Rows * CellH;
            int col = i2 % Columns;
            int row = i2 / Columns;
            uv = new Rect(col * CellW / tw, 1f - (row + 1) * CellH / th, CellW / tw, CellH / th);
            return true;
        }

        public static int MeasureLine(string line) => line.Length == 0 ? 0 : line.Length * CellW - 1;
    }
}
