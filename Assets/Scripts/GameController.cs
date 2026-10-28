using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public enum Estado { Jogando, Pausado, Morto }

    public static GameController instancia;
    public static Estado estado = Estado.Jogando;

    // paineis abertos (inventario, leitura, cofre). Eles NAO pausam o jogo:
    // o jogador fica parado mas os zumbis continuam andando.
    public static int paineisAbertos = 0;
    public static int frameFechouPainel = -1;

    // o clique/tecla que fechou um painel nao conta como tiro ou interacao
    public static bool PodeAgir => estado == Estado.Jogando && paineisAbertos == 0 && frameFechouPainel != Time.frameCount;

    public GameObject painelPausa;
    public GameObject painelMorte;
    public string cenaMenu = "MainMenu";
    public AudioClip somMorte;

    AudioSource audioSource;

    void Awake()
    {
        instancia = this;
        estado = Estado.Jogando;
        paineisAbertos = 0;
        Time.timeScale = 1;
        AudioListener.pause = false;
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        painelPausa.SetActive(false);
        painelMorte.SetActive(false);
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape) || estado == Estado.Morto) return;

        // o Esc que fechou um painel neste frame nao abre a pausa
        if (paineisAbertos > 0 || frameFechouPainel == Time.frameCount) return;

        if (estado == Estado.Jogando) Pausar();
        else Continuar();
    }

    public static void AbriuPainel()
    {
        paineisAbertos++;
    }

    public static void FechouPainel()
    {
        paineisAbertos = Mathf.Max(0, paineisAbertos - 1);
        frameFechouPainel = Time.frameCount;
    }

    public void Pausar()
    {
        estado = Estado.Pausado;
        Time.timeScale = 0;
        AudioListener.pause = true;
        painelPausa.SetActive(true);
    }

    public void Continuar()
    {
        estado = Estado.Jogando;
        Time.timeScale = 1;
        AudioListener.pause = false;
        painelPausa.SetActive(false);
        frameFechouPainel = Time.frameCount;
    }

    public void Morrer()
    {
        if (estado == Estado.Morto) return;
        estado = Estado.Morto;
        if (Musica.instancia != null) Musica.instancia.Parar();
        if (somMorte != null) audioSource.PlayOneShot(somMorte);
        StartCoroutine(MostraTelaMorte());
    }

    IEnumerator MostraTelaMorte()
    {
        yield return new WaitForSecondsRealtime(1.8f);
        painelMorte.SetActive(true);
    }

    public void ReiniciarSala()
    {
        // morrer ou reiniciar volta a sala ao estado inicial
        EstadoSalas.Limpar();
        Time.timeScale = 1;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void IrMenu()
    {
        EstadoSalas.Limpar();
        IrPara(cenaMenu);
    }

    public void IrPara(string cena)
    {
        Time.timeScale = 1;
        AudioListener.pause = false;
        SceneManager.LoadScene(cena);
    }
}
