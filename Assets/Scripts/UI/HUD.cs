using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// HUD enxuto: municao, faca, mensagens e o aviso [E]. A vida aparece so no personagem.
public class HUD : MonoBehaviour
{
    static HUD instancia;

    public Inventario inventario;
    public Arma arma;

    public GameObject grupoPistola;
    public Text textoMunicao;
    public Text textoFaca;
    public Text textoMensagem;
    public RectTransform aviso;
    public Text textoAviso;
    public RectTransform mira;
    public Image flash;
    public Text textoIntro;
    public Image fade;
    public float duracaoIntro = 6f;

    float tempoMensagem;
    float alphaFlash;
    float tempoIntro;

    void Awake()
    {
        instancia = this;
    }

    void Start()
    {
        Cursor.visible = false;
        textoMensagem.text = "";
        aviso.gameObject.SetActive(false);
        tempoIntro = duracaoIntro;
    }

    void OnDestroy()
    {
        Cursor.visible = true;
    }

    public static void Mensagem(string texto, float duracao = 3.5f)
    {
        if (instancia == null) return;
        instancia.textoMensagem.text = texto;
        instancia.tempoMensagem = duracao;
    }

    // escurece (alvo 1) ou clareia (alvo 0) a tela inteira
    public static IEnumerator Fade(float alvo, float duracao)
    {
        if (instancia == null) yield break;
        Image f = instancia.fade;
        float inicio = f.color.a;
        for (float t = 0; t < duracao; t += Time.unscaledDeltaTime)
        {
            f.color = new Color(0, 0, 0, Mathf.Lerp(inicio, alvo, t / duracao));
            yield return null;
        }
        f.color = new Color(0, 0, 0, alvo);
    }

    public static void FlashDano()
    {
        if (instancia != null) instancia.alphaFlash = 0.45f;
    }

    public static void MostrarAviso(Interagivel alvo)
    {
        if (instancia == null) return;
        if (alvo == null)
        {
            instancia.aviso.gameObject.SetActive(false);
            return;
        }
        instancia.aviso.gameObject.SetActive(true);
        instancia.textoAviso.text = "[E] " + alvo.TextoAcao;
        instancia.aviso.position = Camera.main.WorldToScreenPoint(alvo.PontoAviso);
    }

    void Update()
    {
        mira.position = Input.mousePosition;
        Cursor.visible = false;

        ItemInstancia pistola = inventario.Encontrar(TipoItem.Pistola);
        grupoPistola.SetActive(pistola != null);
        if (pistola != null)
        {
            textoMunicao.text = arma.Recarregando
                ? "RECARREGANDO..."
                : pistola.balasNoPente + " / " + inventario.Contar(TipoItem.Municao);
            textoMunicao.color = pistola.balasNoPente == 0 && !arma.Recarregando ? new Color(1f, 0.35f, 0.3f) : new Color(0.95f, 0.9f, 0.78f);
        }

        ItemInstancia faca = inventario.Encontrar(TipoItem.Faca);
        if (faca == null)
        {
            textoFaca.text = "FACA: -";
        }
        else
        {
            string barras = "";
            for (int i = 0; i < 5; i++) barras += i < faca.durabilidade ? "■" : "□";
            textoFaca.text = "FACA " + barras;
        }

        if (tempoMensagem > 0)
        {
            tempoMensagem -= Time.unscaledDeltaTime;
            if (tempoMensagem <= 0) textoMensagem.text = "";
        }

        alphaFlash = Mathf.MoveTowards(alphaFlash, 0, Time.deltaTime * 1.5f);
        flash.color = new Color(0.6f, 0, 0, alphaFlash);

        if (tempoIntro > 0)
        {
            tempoIntro -= Time.deltaTime;
            Color c = textoIntro.color;
            c.a = Mathf.Clamp01(tempoIntro / 1.5f);
            textoIntro.color = c;
            if (tempoIntro <= 0) textoIntro.gameObject.SetActive(false);
        }
    }
}
