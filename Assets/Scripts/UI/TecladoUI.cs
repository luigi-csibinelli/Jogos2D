using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Teclado numerico do cofre (4 digitos). Aceita mouse e teclado.
public class TecladoUI : MonoBehaviour
{
    static TecladoUI instancia;

    public GameObject painel;
    public Text visor;
    public Image fundoVisor;
    public AudioSource audioSource;
    public AudioClip somBip;
    public AudioClip somErro;

    static readonly Color corNormal = new Color32(20, 40, 24, 255);
    static readonly Color corErro = new Color32(110, 16, 16, 255);
    static readonly Color corCerto = new Color32(30, 110, 40, 255);

    Cofre cofre;
    string digitado = "";
    bool travado;
    int frameAbriu;

    public static bool Aberto => instancia != null && instancia.painel.activeSelf;

    void Awake()
    {
        instancia = this;
        painel.SetActive(false);
    }

    public static void Abrir(Cofre c)
    {
        if (instancia == null) return;
        TecladoUI t = instancia;
        t.cofre = c;
        t.digitado = "";
        t.travado = false;
        t.frameAbriu = Time.frameCount;
        t.fundoVisor.color = corNormal;
        t.AtualizarVisor();
        t.painel.SetActive(true);
        GameController.AbriuPainel();
    }

    void Update()
    {
        if (!painel.activeSelf || Time.frameCount == frameAbriu) return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
        {
            Fechar();
            return;
        }
        for (int i = 0; i <= 9; i++)
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i)) Digitar(i);
        if (Input.GetKeyDown(KeyCode.Backspace)) Apagar();
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Confirmar();
    }

    public void Digitar(int n)
    {
        if (travado || digitado.Length >= 4) return;
        digitado += n.ToString();
        audioSource.PlayOneShot(somBip);
        AtualizarVisor();
        if (digitado.Length == 4) Confirmar();
    }

    public void Apagar()
    {
        if (travado || digitado.Length == 0) return;
        digitado = digitado.Substring(0, digitado.Length - 1);
        AtualizarVisor();
    }

    public void Confirmar()
    {
        if (travado || digitado.Length < 4) return;
        StartCoroutine(cofre.TentarCodigo(digitado) ? Acertou() : Errou());
    }

    IEnumerator Errou()
    {
        travado = true;
        audioSource.PlayOneShot(somErro);
        fundoVisor.color = corErro;
        visor.text = "ERRO";
        yield return new WaitForSeconds(0.8f);
        fundoVisor.color = corNormal;
        digitado = "";
        AtualizarVisor();
        travado = false;
    }

    IEnumerator Acertou()
    {
        travado = true;
        fundoVisor.color = corCerto;
        visor.text = "OK";
        yield return new WaitForSeconds(0.5f);
        Fechar();
        cofre.Abrir();
    }

    void AtualizarVisor()
    {
        visor.text = digitado.PadRight(4, '_');
    }

    public void Fechar()
    {
        if (!painel.activeSelf) return;
        painel.SetActive(false);
        GameController.FechouPainel();
    }
}
