using UnityEngine;
using UnityEngine.UI;

// Tela do inventario (Tab). Nao pausa o jogo: os zumbis continuam vindo.
public class InventarioUI : MonoBehaviour
{
    public GameObject painel;
    public Image[] fundos;
    public Image[] icones;
    public Text[] quantidades;
    public Sprite slotNormal;
    public Sprite slotSelecionado;
    public Text textoNome;
    public Text textoDescricao;
    public Button botaoUsar;
    public Text textoUsar;
    public Button botaoLargar;

    public Inventario inventario;
    public Jogador jogador;
    public Arma arma;
    public AudioClip somAbrir;

    int selecionado;

    public bool Aberto => painel.activeSelf;

    void Start()
    {
        painel.SetActive(false);
        inventario.AoMudar += Atualizar;
    }

    void OnDestroy()
    {
        if (inventario != null) inventario.AoMudar -= Atualizar;
    }

    void Update()
    {
        if (LeituraUI.Aberta || TecladoUI.Aberto) return;
        if (GameController.estado != GameController.Estado.Jogando || jogador.Morto)
        {
            if (Aberto) Fechar();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I))
        {
            if (Aberto) Fechar();
            else if (GameController.paineisAbertos == 0) Abrir();
        }
        else if (Aberto && Input.GetKeyDown(KeyCode.Escape))
        {
            Fechar();
        }

        if (!Aberto) return;
        for (int i = 0; i < fundos.Length; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) Selecionar(i);
    }

    void Abrir()
    {
        painel.SetActive(true);
        GameController.AbriuPainel();
        jogador.TocarSom(somAbrir);
        Atualizar();
    }

    public void Fechar()
    {
        painel.SetActive(false);
        GameController.FechouPainel();
    }

    public void Selecionar(int i)
    {
        selecionado = i;
        Atualizar();
    }

    ItemInstancia ItemSelecionado()
    {
        return selecionado < inventario.itens.Count ? inventario.itens[selecionado] : null;
    }

    void Atualizar()
    {
        if (!Aberto) return;
        for (int i = 0; i < fundos.Length; i++)
        {
            bool temItem = i < inventario.itens.Count;
            fundos[i].sprite = i == selecionado ? slotSelecionado : slotNormal;
            icones[i].enabled = temItem;
            quantidades[i].text = "";
            if (!temItem) continue;
            ItemInstancia it = inventario.itens[i];
            icones[i].sprite = CatalogoItens.instancia.Icone(it);
            if (it.tipo == TipoItem.Municao) quantidades[i].text = it.quantidade.ToString();
            if (it.tipo == TipoItem.Pistola) quantidades[i].text = it.balasNoPente.ToString();
            if (it.tipo == TipoItem.Faca) quantidades[i].text = it.durabilidade.ToString();
        }

        ItemInstancia sel = ItemSelecionado();
        if (sel == null)
        {
            textoNome.text = selecionado < inventario.espacos ? "Espaço vazio" : "";
            textoDescricao.text = "Espaços livres: " + (inventario.espacos - inventario.itens.Count) + " de " + inventario.espacos + ".";
            botaoUsar.interactable = false;
            botaoLargar.interactable = false;
            textoUsar.text = "USAR";
            return;
        }

        textoNome.text = CatalogoItens.Nome(sel);
        textoDescricao.text = CatalogoItens.Descricao(sel);
        botaoLargar.interactable = true;
        switch (sel.tipo)
        {
            case TipoItem.Spray: textoUsar.text = "USAR"; botaoUsar.interactable = true; break;
            case TipoItem.Documento: textoUsar.text = "LER"; botaoUsar.interactable = true; break;
            case TipoItem.Pistola: textoUsar.text = "RECARREGAR"; botaoUsar.interactable = true; break;
            default: textoUsar.text = "USAR"; botaoUsar.interactable = false; break;
        }
    }

    public void Usar()
    {
        ItemInstancia sel = ItemSelecionado();
        if (sel == null) return;
        switch (sel.tipo)
        {
            case TipoItem.Spray:
                if (jogador.Curar(60))
                {
                    inventario.Remover(sel);
                    HUD.Mensagem("Você usou o spray. Os ferimentos doem menos.");
                }
                else
                {
                    HUD.Mensagem("Você está bem. Melhor guardar o spray.");
                }
                break;
            case TipoItem.Documento:
                if (sel.documento != null) LeituraUI.Abrir(sel.documento.titulo, sel.documento.texto, null);
                break;
            case TipoItem.Pistola:
                arma.RecarregarPeloInventario();
                break;
        }
        Atualizar();
    }

    public void Largar()
    {
        ItemInstancia sel = ItemSelecionado();
        if (sel == null) return;
        inventario.Largar(sel);
        HUD.Mensagem("Largou: " + CatalogoItens.Nome(sel) + ". Ele fica aqui no chão.");
        Atualizar();
    }
}
