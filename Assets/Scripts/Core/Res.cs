using System.Collections.Generic;
using UnityEngine;

namespace Sanatorio
{
    /// <summary>
    /// Acesso a sprites e materiais em Resources/ (os sons ficam em StreamingAssets, ver AudioManager). O mundo inteiro é montado
    /// em código, então isso substitui a fiação manual de prefabs no Inspector.
    /// Recurso faltando gera um aviso e um substituto, nunca uma exceção.
    /// </summary>
    public static class Res
    {
        private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        private static Material lit;
        private static Material unlit;
        private static Material additive;
        private static Sprite pixel;

        public static Sprite Sprite(string name)
        {
            if (sprites.TryGetValue(name, out Sprite s))
            {
                return s;
            }

            s = Resources.Load<Sprite>("Art/" + name);
            if (s == null)
            {
                Debug.LogWarning("[Res] sprite faltando: " + name);
                s = Pixel;
            }

            sprites[name] = s;
            return s;
        }

        public static bool HasSprite(string name)
        {
            return Resources.Load<Sprite>("Art/" + name) != null;
        }

        public static Sprite Pixel
        {
            get
            {
                if (pixel == null)
                {
                    Texture2D t = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                    t.SetPixel(0, 0, Color.white);
                    t.Apply();
                    pixel = UnityEngine.Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                }

                return pixel;
            }
        }

        /// <summary>Material que recebe as luzes 2D.</summary>
        public static Material Lit => lit != null ? lit : (lit = Resources.Load<Material>("Materials/SpriteLit"));

        /// <summary>Material que ignora as luzes (brilhos, textos no mundo).</summary>
        public static Material Unlit => unlit != null ? unlit : (unlit = Resources.Load<Material>("Materials/SpriteUnlit"));

        public static Material Additive => additive != null ? additive : (additive = Resources.Load<Material>("Materials/SpriteAdditive"));

        public static SpriteRenderer AddSprite(GameObject go, string sprite, int order, bool lit = true)
        {
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite(sprite);
            sr.sortingOrder = order;
            Material m = lit ? Lit : Unlit;
            if (m != null)
            {
                sr.sharedMaterial = m;
            }

            return sr;
        }

        public static GameObject Make(string name, Transform parent, Vector2 pos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            return go;
        }
    }
}
