using UnityEngine;

// Movel que pode ser vasculhado (relogio, estante, vaso, sofa...).
// Pode guardar um item escondido ou estar vazio.
public class Esconderijo : Interagivel
{
    public string nomeObjeto = "relógio";
    public string artigo = "no";            // "no relógio", "na estante"
    public bool temItem;
    public ItemInstancia item;
    [TextArea] public string textoVazio = "Não há nada aqui.";
    public AudioClip somPegar;
    public AudioClip somVasculhar;

    bool vasculhado;

    public override string TextoAcao => "Vasculhar " + nomeObjeto;

    public override void Interagir(Jogador jogador)
    {
        jogador.TocarSom(somVasculhar);
        if (!temItem)
        {
            HUD.Mensagem(vasculhado ? "Você já vasculhou aqui. " + textoVazio : textoVazio);
            vasculhado = true;
            return;
        }

        Inventario inv = jogador.GetComponent<Inventario>();
        if (!inv.Adicionar(item.Clonar()))
        {
            HUD.Mensagem("Tem algo escondido " + artigo + " " + nomeObjeto + ", mas seu inventário está cheio.");
            return;
        }
        temItem = false;
        vasculhado = true;
        jogador.TocarSom(somPegar);
        string extra = item.tipo == TipoItem.Municao ? " (" + item.quantidade + ")" : "";
        HUD.Mensagem("Encontrou " + artigo + " " + nomeObjeto + ": " + CatalogoItens.Nome(item) + extra);
    }
}
