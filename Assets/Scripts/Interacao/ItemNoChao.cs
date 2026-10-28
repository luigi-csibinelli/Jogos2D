using UnityEngine;

// Item que pode ser pego. Tambem e o que aparece quando o jogador larga algo.
public class ItemNoChao : Interagivel
{
    public ItemInstancia item;
    public string idUnico;                  // itens colocados na cena: para nao voltarem depois de pegos
    public Zumbi[] acordarAoPegar;          // ex.: pegar a pistola levanta o zumbi
    public SpriteRenderer sr;
    public SpriteRenderer brilho;
    public AudioClip somPegar;

    [HideInInspector] public EstadoSalas.ItemLargado registro;

    float semente;

    public override string TextoAcao => "Pegar " + CatalogoItens.NomeCurto(item);

    public static ItemNoChao Criar(GameObject prefab, ItemInstancia item, Vector2 posicao, bool registrar)
    {
        GameObject go = Instantiate(prefab, posicao, Quaternion.identity);
        ItemNoChao novo = go.GetComponent<ItemNoChao>();
        novo.item = item;
        if (registrar) novo.registro = EstadoSalas.Registrar(item, posicao);
        return novo;
    }

    void Start()
    {
        if (EstadoSalas.FoiColetado(idUnico))
        {
            Destroy(gameObject);
            return;
        }
        sr.sprite = CatalogoItens.instancia.Icone(item);
        semente = Random.value * 10f;
    }

    void Update()
    {
        // brilho piscando para mostrar que da para pegar
        float ciclo = (Time.time + semente) % 1.6f;
        brilho.enabled = ciclo < 0.35f;
        brilho.transform.localPosition = new Vector3(0.25f, 0.25f + ciclo * 0.15f, 0);
    }

    public override void Interagir(Jogador jogador)
    {
        Inventario inv = jogador.GetComponent<Inventario>();
        if (!inv.Adicionar(item))
        {
            HUD.Mensagem("Inventário cheio. Largue algo antes [Tab].");
            return;
        }

        jogador.TocarSom(somPegar);
        string extra = item.tipo == TipoItem.Pistola ? " (" + item.balasNoPente + " balas)" : "";
        HUD.Mensagem("Pegou: " + CatalogoItens.Nome(item) + extra);

        EstadoSalas.Remover(registro);
        EstadoSalas.MarcarColetado(idUnico);

        foreach (Zumbi z in acordarAoPegar)
            if (z != null) z.Acordar();

        if (item.tipo == TipoItem.Documento && item.documento != null)
            LeituraUI.Abrir(item.documento.titulo, item.documento.texto, null);

        Destroy(gameObject);
    }
}
