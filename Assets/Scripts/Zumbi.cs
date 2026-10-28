using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Zumbi : MonoBehaviour
{
    public enum EstadoZumbi { Dormindo, Vagando, Perseguindo, Caido, Levantando, Morto }

    // a trilha sonora escuta estes eventos
    public static event Action<Zumbi> AoLevantar;
    public static event Action<Zumbi> AoCair;
    public static event Action<Zumbi> AoMorrer;
    public static readonly List<Zumbi> todos = new List<Zumbi>();

    public EstadoZumbi estado = EstadoZumbi.Dormindo;

    public float vidaMax = 100f;
    public float vida;
    [Range(0, 1)] public float limiarQueda = 0.4f;   // cai ao chegar a 40% da vida
    public float tempoCaido = 4f;
    public float tempoLevantar = 0.9f;

    public float velocidade = 1.05f;
    public float alcanceVisao = 8f;
    public float alcanceAtaque = 0.8f;
    public float intervaloAtaque = 1.3f;
    public int danoAtaque = 20;
    public float distanciaDesiste = 20f;
    public LayerMask camadaParede;

    public Sprite[] spritesAndar;   // [0] parado, [1] e [2] andando (olhando para a direita)
    public Sprite spriteCaido;
    public Sprite spriteLevantando;
    public GameObject pocaDeSangue;

    public SpriteRenderer visual;
    public Collider2D corpoFisico;
    public Collider2D hurtbox;

    public AudioClip somGrunhido;
    public AudioClip somGemido;
    public AudioClip somImpacto;

    public bool JaCaiu { get; private set; }
    public bool Atingivel => estado != EstadoZumbi.Dormindo && estado != EstadoZumbi.Morto;
    public bool Ameacando => estado == EstadoZumbi.Perseguindo || estado == EstadoZumbi.Levantando;

    Rigidbody2D rb;
    AudioSource audioSource;
    Vector2 direcaoAndar;
    Vector2 empurrao;
    float tempoEmpurrao;
    float esperaAtaque;
    float pausaAtaque;
    float tempoAnim;
    float proximoGemido;
    float trocaDirecao;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        vida = vidaMax;
    }

    void OnEnable() { todos.Add(this); }
    void OnDisable() { todos.Remove(this); }

    void Start()
    {
        if (estado == EstadoZumbi.Dormindo)
        {
            visual.sprite = spriteCaido;
            visual.sortingOrder = -60;
            corpoFisico.enabled = false;
            hurtbox.enabled = false;
        }
        pocaDeSangue.SetActive(false);
    }

    // ------------------------------------------------------------ eventos externos

    public void Acordar()
    {
        if (estado != EstadoZumbi.Dormindo) return;
        StartCoroutine(Levantar());
    }

    public static void OuvirBarulho(Vector2 posicao, float raio)
    {
        foreach (Zumbi z in todos)
        {
            if (z.estado == EstadoZumbi.Vagando && Vector2.Distance(z.transform.position, posicao) < raio)
                z.ComecarPerseguicao();
        }
    }

    public static Zumbi CaidoMaisProximo(Vector2 posicao, float raio)
    {
        Zumbi melhor = null;
        float menor = raio;
        foreach (Zumbi z in todos)
        {
            if (z.estado != EstadoZumbi.Caido) continue;
            float d = Vector2.Distance(z.transform.position, posicao);
            if (d <= menor)
            {
                menor = d;
                melhor = z;
            }
        }
        return melhor;
    }

    public void LevarDano(float dano, Vector2 dir, Vector2 ponto, float forcaEmpurrao = 3f)
    {
        if (!Atingivel) return;
        vida -= dano;
        audioSource.PlayOneShot(somImpacto);

        // pixels de sangue saem na direcao do tiro
        PixelsSangue.Explodir(ponto, 0.35f, dir, UnityEngine.Random.Range(8, 13));

        if (estado == EstadoZumbi.Vagando) ComecarPerseguicao();

        if (vida <= 0)
        {
            Morrer();
            return;
        }

        if (estado != EstadoZumbi.Caido)
        {
            empurrao = dir * forcaEmpurrao;
            tempoEmpurrao = 0.12f;
        }

        if (!JaCaiu && vida <= vidaMax * limiarQueda && estado == EstadoZumbi.Perseguindo)
            StartCoroutine(Cair());
    }

    public void Finalizar()
    {
        if (estado != EstadoZumbi.Caido) return;
        audioSource.PlayOneShot(somImpacto);
        PixelsSangue.Explodir(transform.position, 0.2f, Vector2.zero, 22);
        Morrer();
    }

    // ------------------------------------------------------------ estados

    IEnumerator Levantar()
    {
        estado = EstadoZumbi.Levantando;
        visual.sprite = spriteLevantando;
        visual.sortingOrder = 10;
        audioSource.PlayOneShot(somGrunhido);
        corpoFisico.enabled = true;
        hurtbox.enabled = true;
        AoLevantar?.Invoke(this);
        yield return new WaitForSeconds(tempoLevantar);
        estado = EstadoZumbi.Perseguindo;
        proximoGemido = Time.time + UnityEngine.Random.Range(3f, 6f);
    }

    IEnumerator Cair()
    {
        JaCaiu = true;
        estado = EstadoZumbi.Caido;
        visual.sprite = spriteCaido;
        visual.sortingOrder = -60;
        corpoFisico.enabled = false;
        rb.linearVelocity = Vector2.zero;
        AoCair?.Invoke(this);

        yield return new WaitForSeconds(tempoCaido);

        // nao foi finalizado com a faca: levanta e volta a perseguir
        if (estado == EstadoZumbi.Caido) StartCoroutine(Levantar());
    }

    void ComecarPerseguicao()
    {
        estado = EstadoZumbi.Perseguindo;
        audioSource.PlayOneShot(somGemido);
        proximoGemido = Time.time + UnityEngine.Random.Range(5f, 9f);
    }

    void Morrer()
    {
        StopAllCoroutines();
        estado = EstadoZumbi.Morto;
        visual.sprite = spriteCaido;
        visual.sortingOrder = -60;
        visual.color = new Color(0.8f, 0.8f, 0.8f);
        corpoFisico.enabled = false;
        hurtbox.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        pocaDeSangue.SetActive(true);
        AoMorrer?.Invoke(this);
    }

    // ------------------------------------------------------------ comportamento

    void Update()
    {
        Jogador j = Jogador.instancia;
        if (esperaAtaque > 0) esperaAtaque -= Time.deltaTime;
        if (pausaAtaque > 0) pausaAtaque -= Time.deltaTime;

        switch (estado)
        {
            case EstadoZumbi.Vagando:
                Vagar();
                if (j != null && !j.Morto && Enxerga(j)) ComecarPerseguicao();
                break;

            case EstadoZumbi.Perseguindo:
                if (j == null || j.Morto)
                {
                    direcaoAndar = Vector2.zero;
                    break;
                }
                Vector2 alvo = (Vector2)j.transform.position - (Vector2)transform.position;
                // jogador foi para outro andar: o zumbi fica parado esperando
                bool longe = alvo.magnitude > distanciaDesiste;
                direcaoAndar = pausaAtaque > 0 || longe ? Vector2.zero : alvo.normalized;
                if (alvo.magnitude < alcanceAtaque && esperaAtaque <= 0)
                {
                    j.TomarDano(danoAtaque, transform.position);
                    esperaAtaque = intervaloAtaque;
                    pausaAtaque = 0.5f;
                }
                if (Time.time > proximoGemido)
                {
                    audioSource.PlayOneShot(somGemido);
                    proximoGemido = Time.time + UnityEngine.Random.Range(5f, 9f);
                }
                break;

            default:
                direcaoAndar = Vector2.zero;
                break;
        }

        Animar();
    }

    void FixedUpdate()
    {
        if (estado == EstadoZumbi.Morto || estado == EstadoZumbi.Dormindo) return;

        Vector2 passo = Vector2.zero;
        if (tempoEmpurrao > 0)
        {
            tempoEmpurrao -= Time.fixedDeltaTime;
            passo = empurrao;
        }
        else if (estado == EstadoZumbi.Perseguindo || estado == EstadoZumbi.Vagando)
        {
            float v = estado == EstadoZumbi.Vagando ? velocidade * 0.4f : velocidade;
            passo = direcaoAndar * v;
        }
        rb.linearVelocity = Vector2.zero;
        rb.MovePosition(rb.position + passo * Time.fixedDeltaTime);
    }

    bool Enxerga(Jogador j)
    {
        Vector2 olhos = transform.position;
        if (Vector2.Distance(olhos, j.Peito) > alcanceVisao) return false;
        return Physics2D.Linecast(olhos, j.Peito, camadaParede).collider == null;
    }

    void Vagar()
    {
        trocaDirecao -= Time.deltaTime;
        if (trocaDirecao <= 0)
        {
            trocaDirecao = UnityEngine.Random.Range(1.5f, 3f);
            direcaoAndar = UnityEngine.Random.value < 0.4f ? Vector2.zero : UnityEngine.Random.insideUnitCircle.normalized;
        }
    }

    void Animar()
    {
        if (estado != EstadoZumbi.Perseguindo && estado != EstadoZumbi.Vagando) return;

        Vector2 dir = direcaoAndar;
        if (dir == Vector2.zero && Jogador.instancia != null)
            dir = (Jogador.instancia.transform.position - transform.position).normalized;
        if (dir != Vector2.zero)
        {
            float alvo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float atual = visual.transform.eulerAngles.z;
            visual.transform.rotation = Quaternion.Euler(0, 0, Mathf.MoveTowardsAngle(atual, alvo, 240f * Time.deltaTime));
        }

        if (direcaoAndar == Vector2.zero)
        {
            visual.sprite = spritesAndar[0];
        }
        else
        {
            tempoAnim += Time.deltaTime * 4f;
            visual.sprite = spritesAndar[1 + (int)tempoAnim % 2];
        }
    }
}
