using UnityEngine;

// Objeto que so mostra um texto (e uma imagem opcional) ao ser examinado. Ex.: o retrato.
public class Examinavel : Interagivel
{
    public string acao = "Examinar";
    public string titulo;
    [TextArea(4, 12)] public string texto;
    public Sprite imagem;
    public bool usarPainel = true;   // falso: mostra so a mensagem curta no rodape

    public override string TextoAcao => acao;

    public override void Interagir(Jogador jogador)
    {
        if (usarPainel) LeituraUI.Abrir(titulo, texto, imagem);
        else HUD.Mensagem(texto);
    }
}
