using UnityEngine;

// Sangue em pixels: cada gota voa em arco e fica manchando o chao.
public class PixelsSangue : MonoBehaviour
{
    static PixelsSangue instancia;

    public Sprite pixel;
    public Material material;
    public int maximo = 500;
    public float gravidade = 9f;

    static readonly Color[] cores =
    {
        new Color32(150, 18, 22, 255),
        new Color32(190, 24, 30, 255),
        new Color32(110, 10, 16, 255),
    };

    class Gota
    {
        public Transform t;
        public SpriteRenderer sr;
        public Vector2 chao;
        public Vector2 velocidade;
        public float altura;
        public float vAltura;
        public bool voando;
    }

    Gota[] gotas;
    int proxima;

    void Awake()
    {
        instancia = this;
        gotas = new Gota[maximo];
    }

    public static void Explodir(Vector2 chao, float altura, Vector2 direcao, int quantidade)
    {
        if (instancia == null) return;
        for (int i = 0; i < quantidade; i++)
        {
            Vector2 espalha = Random.insideUnitCircle * 1.4f;
            Vector2 v = direcao * Random.Range(1f, 3.5f) + espalha;
            instancia.Soltar(chao, altura + Random.Range(-0.1f, 0.1f), v, Random.Range(0.5f, 2.5f), Random.value < 0.3f ? 2 : 1);
        }
    }

    public static void Pingar(Vector2 chao)
    {
        if (instancia == null) return;
        instancia.Soltar(chao, 0.35f, Random.insideUnitCircle * 0.2f, 0f, 1);
    }

    void Soltar(Vector2 chao, float altura, Vector2 velocidade, float subida, int tamanho)
    {
        Gota g = gotas[proxima];
        if (g == null)
        {
            GameObject go = new GameObject("gota");
            go.transform.SetParent(transform, false);
            g = new Gota { t = go.transform, sr = go.AddComponent<SpriteRenderer>() };
            g.sr.sprite = pixel;
            g.sr.sharedMaterial = material;
            gotas[proxima] = g;
        }
        proxima = (proxima + 1) % maximo;

        g.chao = chao;
        g.velocidade = velocidade;
        g.altura = Mathf.Max(0.02f, altura);
        g.vAltura = subida;
        g.voando = true;
        g.sr.color = cores[Random.Range(0, cores.Length)];
        g.sr.sortingOrder = 50;
        g.t.localScale = Vector3.one * tamanho;
        g.t.gameObject.SetActive(true);
        g.t.position = chao + Vector2.up * g.altura;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        foreach (Gota g in gotas)
        {
            if (g == null || !g.voando) continue;
            g.vAltura -= gravidade * dt;
            g.altura += g.vAltura * dt;
            g.chao += g.velocidade * dt;
            g.velocidade *= 1f - 2f * dt;
            if (g.altura <= 0)
            {
                // pousou: vira mancha no chao, abaixo dos personagens
                g.altura = 0;
                g.voando = false;
                g.sr.sortingOrder = -80;
                g.sr.color *= new Color(0.75f, 0.75f, 0.75f, 1f);
            }
            // arredonda para o pixel (16 por unidade) para manter o visual 8-bit
            Vector2 p = g.chao + Vector2.up * g.altura;
            g.t.position = new Vector3(Mathf.Round(p.x * 16f) / 16f, Mathf.Round(p.y * 16f) / 16f, 0);
        }
    }
}
