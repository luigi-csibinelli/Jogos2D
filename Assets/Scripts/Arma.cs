using System.Collections;
using UnityEngine;

// Pistola (clique esquerdo) e faca (clique direito). Fica no jogador.
public class Arma : MonoBehaviour
{
    // giro extra do corpo durante o golpe de faca (lido pelo Jogador)
    public static float anguloGolpe;
    public static bool golpeando;

    [Header("Pistola")]
    public float danoBase = 17f;
    public float multiplicadorMin = 0.75f;
    public float multiplicadorMax = 1.25f;
    public int capacidadePente = 7;
    public float intervaloTiro = 0.32f;
    public float tempoRecarga = 1.1f;
    public float alcance = 14f;
    public LayerMask camadasTiro;

    [Header("Faca")]
    public float danoFaca = 10f;
    public float alcanceFaca = 0.65f;
    public float intervaloFaca = 0.45f;
    public float alcanceFinalizacao = 1.3f;
    public LayerMask camadaZumbi;

    [Header("Visual")]
    public Transform bocaDoCano;
    public LineRenderer rastro;
    public GameObject clarao;

    [Header("Sons")]
    public AudioClip somTiro;
    public AudioClip somVazio;
    public AudioClip somRecarga;
    public AudioClip somFaca;
    public AudioClip somFacaQuebra;

    public bool Recarregando { get; private set; }

    Jogador jogador;
    Inventario inventario;
    AudioSource audioSource;
    float esperaTiro;
    float esperaFaca;
    float tempoGolpe;

    void Awake()
    {
        jogador = GetComponent<Jogador>();
        inventario = GetComponent<Inventario>();
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        rastro.enabled = false;
        clarao.SetActive(false);
    }

    void Update()
    {
        if (esperaTiro > 0) esperaTiro -= Time.deltaTime;
        if (esperaFaca > 0) esperaFaca -= Time.deltaTime;
        AtualizarGolpe();

        if (!GameController.PodeAgir || jogador.Morto) return;

        ItemInstancia pistola = inventario.Encontrar(TipoItem.Pistola);

        if (Input.GetMouseButtonDown(0))
        {
            if (pistola != null) Atirar(pistola);
            else Facada();
        }
        if (Input.GetMouseButtonDown(1)) Facada();
        if (Input.GetKeyDown(KeyCode.R) && pistola != null) TentarRecarregar(pistola);
    }

    void AtualizarGolpe()
    {
        golpeando = tempoGolpe > 0;
        if (golpeando)
        {
            tempoGolpe -= Time.deltaTime;
            // o corpo gira num arco de +50 a -50 graus
            anguloGolpe = Mathf.Lerp(50f, -50f, 1f - tempoGolpe / 0.2f);
        }
        else
        {
            anguloGolpe = 0;
        }
    }

    void Atirar(ItemInstancia pistola)
    {
        if (Recarregando || esperaTiro > 0) return;
        esperaTiro = intervaloTiro;

        if (pistola.balasNoPente <= 0)
        {
            audioSource.PlayOneShot(somVazio);
            if (inventario.Contar(TipoItem.Municao) > 0) HUD.Mensagem("Pente vazio. [R] para recarregar.");
            else HUD.Mensagem("Sem munição.");
            return;
        }

        pistola.balasNoPente--;
        inventario.AvisarMudanca();
        audioSource.PlayOneShot(somTiro);
        CameraSegue.Tremer(0.08f, 0.08f);
        Zumbi.OuvirBarulho(transform.position, 12f);

        Vector2 origem = jogador.Peito;
        Vector2 dir = jogador.DirecaoMira;
        Vector2 fim = origem + dir * alcance;

        RaycastHit2D[] acertos = Physics2D.RaycastAll(origem, dir, alcance, camadasTiro);
        System.Array.Sort(acertos, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit2D h in acertos)
        {
            Zumbi z = h.collider.GetComponentInParent<Zumbi>();
            if (z != null)
            {
                if (!z.Atingivel) continue;
                float dano = danoBase * Random.Range(multiplicadorMin, multiplicadorMax);
                z.LevarDano(dano, dir, h.point);
                fim = h.point;
                break;
            }
            // parede ou movel
            fim = h.point;
            break;
        }
        StartCoroutine(MostraTiro(fim));
    }

    IEnumerator MostraTiro(Vector2 fim)
    {
        rastro.SetPosition(0, bocaDoCano.position);
        rastro.SetPosition(1, fim);
        rastro.enabled = true;
        clarao.SetActive(true);
        yield return new WaitForSeconds(0.05f);
        rastro.enabled = false;
        clarao.SetActive(false);
    }

    void TentarRecarregar(ItemInstancia pistola)
    {
        if (Recarregando) return;
        if (pistola.balasNoPente >= capacidadePente)
        {
            HUD.Mensagem("O pente já está cheio.");
            return;
        }
        if (inventario.Contar(TipoItem.Municao) <= 0)
        {
            HUD.Mensagem("Sem munição para recarregar.");
            return;
        }
        StartCoroutine(Recarregar(pistola));
    }

    IEnumerator Recarregar(ItemInstancia pistola)
    {
        Recarregando = true;
        audioSource.PlayOneShot(somRecarga);
        yield return new WaitForSeconds(tempoRecarga);
        int falta = capacidadePente - pistola.balasNoPente;
        int pegou = inventario.Consumir(TipoItem.Municao, falta);
        pistola.balasNoPente += pegou;
        Recarregando = false;
        inventario.AvisarMudanca();
    }

    public void RecarregarPeloInventario()
    {
        ItemInstancia pistola = inventario.Encontrar(TipoItem.Pistola);
        if (pistola != null) TentarRecarregar(pistola);
    }

    public void Facada()
    {
        if (esperaFaca > 0 || Recarregando) return;
        ItemInstancia faca = inventario.Encontrar(TipoItem.Faca);
        if (faca == null)
        {
            HUD.Mensagem("Você não tem uma faca.");
            return;
        }
        esperaFaca = intervaloFaca;
        tempoGolpe = 0.2f;
        audioSource.PlayOneShot(somFaca);

        bool acertou = false;

        // finalizacao: zumbi caido por perto morre na hora gastando so 1 uso
        Zumbi caido = Zumbi.CaidoMaisProximo(transform.position, alcanceFinalizacao);
        if (caido != null)
        {
            caido.Finalizar();
            acertou = true;
        }
        else
        {
            Vector2 dir = jogador.DirecaoMira;
            Vector2 ponto = jogador.Peito + dir * 0.55f;
            Collider2D[] cols = Physics2D.OverlapCircleAll(ponto, alcanceFaca, camadaZumbi);
            foreach (Collider2D c in cols)
            {
                Zumbi z = c.GetComponentInParent<Zumbi>();
                if (z == null || !z.Atingivel) continue;
                z.LevarDano(danoFaca, dir, ponto, 5f);
                acertou = true;
                break;
            }
        }

        if (acertou)
        {
            faca.durabilidade--;
            if (faca.durabilidade <= 0)
            {
                inventario.Remover(faca);
                audioSource.PlayOneShot(somFacaQuebra);
                HUD.Mensagem("A faca quebrou!");
            }
            inventario.AvisarMudanca();
        }
    }
}
