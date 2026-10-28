using UnityEngine;

// Liga e desliga as luzes de uma secao, mas so se ela tiver energia.
public class Interruptor : Interagivel
{
    public SecaoEnergia secao;
    public SpriteRenderer sr;
    public Sprite spriteDesligado;
    public Sprite spriteLigado;
    public AudioClip somClique;

    public override string TextoAcao => secao.luzesAcesas ? "Apagar luzes" : "Acender luzes";

    void Start()
    {
        sr.sprite = secao.luzesAcesas ? spriteLigado : spriteDesligado;
    }

    public override void Interagir(Jogador jogador)
    {
        jogador.TocarSom(somClique);
        if (!secao.Alternar())
        {
            HUD.Mensagem("Nada acontece. Esta parte da mansão está sem energia.");
            return;
        }
        sr.sprite = secao.luzesAcesas ? spriteLigado : spriteDesligado;
    }
}
