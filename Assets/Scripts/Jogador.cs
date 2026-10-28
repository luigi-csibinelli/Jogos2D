using UnityEngine;

// Visao de cima: o corpo inteiro gira para a mira; os pes alternam ao andar.
public class Jogador : MonoBehaviour
{
    public enum Condicao { Bem, Cuidado, Perigo }

    public static Jogador instancia;

    public float velocidade = 3.2f;
    public int vidaMax = 100;
    public int vida;

    // [0] parado, [1] e [2] andando
    public Sprite[] spritesPistola;
    public Sprite[] spritesFaca;
    public Sprite[] spritesMaos;
    public Sprite spriteMorto;

    public Transform corpo;          // gira para a mira (leva a lanterna e o cano junto)
    public SpriteRenderer visual;
    public GameObject lanterna;

    public AudioClip somDano;
    public AudioClip somCura;

    Rigidbody2D rb;
    AudioSource audioSource;
    Inventario inventario;
    Vector2 movimento;
    Vector2 posicaoMira;
    float tempoAnim;
    float invencivel;
    float tempoPingo;
    bool morto;

    public Condicao condicao => vida > 66 ? Condicao.Bem : (vida > 33 ? Condicao.Cuidado : Condicao.Perigo);
    public bool Morto => morto;
    public Vector2 Peito => transform.position;
    public Vector2 PosicaoMira => posicaoMira;
    public Vector2 DirecaoMira
    {
        get
        {
            Vector2 d = posicaoMira - Peito;
            return d.sqrMagnitude < 0.0001f ? Vector2.right : d.normalized;
        }
    }

    void Awake()
    {
        instancia = this;
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        inventario = GetComponent<Inventario>();
        vida = vidaMax;
        posicaoMira = (Vector2)transform.position + Vector2.left;
    }

    void Update()
    {
        if (morto) return;

        if (GameController.PodeAgir)
        {
            movimento = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            posicaoMira = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
        else
        {
            movimento = Vector2.zero;
        }

        if (invencivel > 0) invencivel -= Time.deltaTime;

        Animar();

        // em Perigo vai pingando sangue no chao
        if (condicao == Condicao.Perigo)
        {
            tempoPingo -= Time.deltaTime;
            if (tempoPingo <= 0)
            {
                PixelsSangue.Pingar((Vector2)transform.position + Random.insideUnitCircle * 0.2f);
                tempoPingo = movimento != Vector2.zero ? Random.Range(0.25f, 0.5f) : Random.Range(0.9f, 1.5f);
            }
        }
    }

    void FixedUpdate()
    {
        if (morto) return;
        rb.MovePosition(rb.position + movimento.normalized * VelocidadeAtual() * Time.fixedDeltaTime);
    }

    float VelocidadeAtual()
    {
        if (condicao == Condicao.Perigo) return velocidade * 0.55f;
        if (condicao == Condicao.Cuidado) return velocidade * 0.75f;
        return velocidade;
    }

    Sprite[] SpritesAtuais()
    {
        if (Arma.golpeando) return spritesFaca;   // mostra a faca durante o golpe
        if (inventario.Encontrar(TipoItem.Pistola) != null) return spritesPistola;
        if (inventario.Encontrar(TipoItem.Faca) != null) return spritesFaca;
        return spritesMaos;
    }

    void Animar()
    {
        Sprite[] lista = SpritesAtuais();
        float balanco = 0;

        if (movimento == Vector2.zero)
        {
            visual.sprite = lista[0];
            tempoAnim = 0;
        }
        else if (condicao == Condicao.Bem)
        {
            tempoAnim += Time.deltaTime * 8f;
            visual.sprite = lista[1 + (int)tempoAnim % 2];
        }
        else
        {
            // mancando: passo com a perna boa, arrasta a outra e o corpo balanca
            float ritmo = condicao == Condicao.Cuidado ? 5.5f : 4f;
            tempoAnim += Time.deltaTime * ritmo;
            int[] sequencia = { 1, 0, 2, 0 };
            visual.sprite = lista[sequencia[(int)tempoAnim % 4]];
            balanco = Mathf.Sin(tempoAnim * Mathf.PI * 0.5f) * (condicao == Condicao.Cuidado ? 7f : 13f);
        }

        Vector2 dir = DirecaoMira;
        float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        corpo.localRotation = Quaternion.Euler(0, 0, angulo + Arma.anguloGolpe + balanco);
    }

    public void TomarDano(int dano, Vector2 origem)
    {
        if (morto || invencivel > 0) return;
        vida -= dano;
        invencivel = 0.9f;
        audioSource.PlayOneShot(somDano);
        Vector2 dir = ((Vector2)transform.position - origem).normalized;
        PixelsSangue.Explodir(transform.position, 0.35f, dir, 12);
        CameraSegue.Tremer(0.18f, 0.15f);
        HUD.FlashDano();

        if (vida <= 0)
        {
            vida = 0;
            Morrer();
        }
    }

    public void Teleportar(Vector2 posicao)
    {
        rb.position = posicao;
        transform.position = posicao;
        rb.linearVelocity = Vector2.zero;
        movimento = Vector2.zero;
    }

    public bool Curar(int quanto)
    {
        if (vida >= vidaMax) return false;
        vida = Mathf.Min(vidaMax, vida + quanto);
        audioSource.PlayOneShot(somCura);
        return true;
    }

    void Morrer()
    {
        morto = true;
        movimento = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        visual.sprite = spriteMorto;
        lanterna.SetActive(false);
        PixelsSangue.Explodir(transform.position, 0.3f, Vector2.zero, 20);
        GameController.instancia.Morrer();
    }

    public void TocarSom(AudioClip clip)
    {
        if (clip != null) audioSource.PlayOneShot(clip);
    }
}
