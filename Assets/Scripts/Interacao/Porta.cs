using UnityEngine;

public class Porta : Interagivel
{
    public enum Tipo { Trancada, Emperrada }

    public Tipo tipo = Tipo.Trancada;
    public string idChave = "brasao";
    public bool aberta;
    public string cenaDestino = "FimDemo";
    [TextArea] public string textoTrancada = "Trancada. A fechadura tem um brasão de serpente.";
    [TextArea] public string textoEmperrada = "A porta da frente emperrou atrás de mim. Não dá para voltar.";

    public SpriteRenderer sr;
    public Sprite spriteFechada;
    public Sprite spriteAberta;
    public AudioClip somTrancada;
    public AudioClip somAbrir;

    public override string TextoAcao => aberta ? "Entrar" : "Abrir porta";

    void Start()
    {
        if (sr != null && spriteFechada != null) sr.sprite = aberta ? spriteAberta : spriteFechada;
    }

    public override void Interagir(Jogador jogador)
    {
        if (tipo == Tipo.Emperrada)
        {
            jogador.TocarSom(somTrancada);
            HUD.Mensagem(textoEmperrada);
            return;
        }

        if (aberta)
        {
            GameController.instancia.IrPara(cenaDestino);
            return;
        }

        Inventario inv = jogador.GetComponent<Inventario>();
        ItemInstancia chave = inv.EncontrarChave(idChave);
        if (chave == null)
        {
            jogador.TocarSom(somTrancada);
            HUD.Mensagem(textoTrancada);
            return;
        }

        // a chave fica no inventario: cabe ao jogador perceber que ja pode larga-la
        aberta = true;
        sr.sprite = spriteAberta;
        jogador.TocarSom(somAbrir);
        HUD.Mensagem("Você usou a " + CatalogoItens.Nome(chave) + ". A porta está destrancada.");
    }
}
