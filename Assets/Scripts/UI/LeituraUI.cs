using UnityEngine;
using UnityEngine.UI;

// Painel para ler documentos e examinar objetos. Tambem nao pausa o jogo.
public class LeituraUI : MonoBehaviour
{
    static LeituraUI instancia;

    public GameObject painel;
    public Text textoTitulo;
    public Text textoCorpo;
    public Image imagem;
    public RectTransform areaTexto;
    public AudioSource audioSource;
    public AudioClip somPapel;

    int frameAbriu;

    public static bool Aberta => instancia != null && instancia.painel.activeSelf;

    void Awake()
    {
        instancia = this;
        painel.SetActive(false);
    }

    public static void Abrir(string titulo, string texto, Sprite imagem)
    {
        if (instancia == null) return;
        LeituraUI l = instancia;
        if (!l.painel.activeSelf) GameController.AbriuPainel();
        l.painel.SetActive(true);
        l.frameAbriu = Time.frameCount;
        l.textoTitulo.text = titulo;
        l.textoCorpo.text = texto;
        l.imagem.gameObject.SetActive(imagem != null);
        l.imagem.sprite = imagem;
        // com imagem, o texto vai para a direita
        l.areaTexto.offsetMin = new Vector2(imagem != null ? 220 : 30, l.areaTexto.offsetMin.y);
        l.audioSource.PlayOneShot(l.somPapel);
    }

    void Update()
    {
        if (!painel.activeSelf || Time.frameCount == frameAbriu) return;
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return))
            Fechar();
    }

    public void Fechar()
    {
        if (!painel.activeSelf) return;
        painel.SetActive(false);
        GameController.FechouPainel();
    }
}
