using UnityEngine;

public class Cofre : Interagivel
{
    public string codigo = "1987";
    public bool aberto;
    public ItemInstancia conteudo;
    public bool temConteudo = true;

    public SpriteRenderer sr;
    public Sprite spriteFechado;
    public Sprite spriteAberto;
    public AudioClip somAbrir;
    public AudioClip somPegar;

    public override string TextoAcao => aberto ? "Pegar chave" : "Usar cofre";
    public override bool Disponivel => !aberto || temConteudo;

    void Start()
    {
        sr.sprite = aberto ? spriteAberto : spriteFechado;
    }

    public override void Interagir(Jogador jogador)
    {
        if (!aberto) TecladoUI.Abrir(this);
        else PegarConteudo(jogador);
    }

    public bool TentarCodigo(string tentativa)
    {
        return tentativa == codigo;
    }

    public void Abrir()
    {
        aberto = true;
        sr.sprite = spriteAberto;
        Jogador.instancia.TocarSom(somAbrir);
        PegarConteudo(Jogador.instancia);
    }

    void PegarConteudo(Jogador jogador)
    {
        if (!temConteudo) return;
        Inventario inv = jogador.GetComponent<Inventario>();
        if (!inv.Adicionar(conteudo.Clonar()))
        {
            HUD.Mensagem("O cofre abriu! Mas seu inventário está cheio. Largue algo e volte.");
            return;
        }
        temConteudo = false;
        jogador.TocarSom(somPegar);
        HUD.Mensagem("Pegou: " + CatalogoItens.Nome(conteudo));
    }
}
