using UnityEngine;

// Corpo que pode ser revistado uma vez (guarda um item, ex.: o documento do agente).
public class Corpo : Interagivel
{
    public ItemInstancia itemGuardado;
    public bool temItem = true;
    [TextArea] public string textoVazio = "É o Agente Okafor, do DHS. Não há mais nada com ele.";
    public AudioClip somPegar;

    public override string TextoAcao => temItem ? "Revistar corpo" : "Examinar corpo";

    public override void Interagir(Jogador jogador)
    {
        if (!temItem)
        {
            HUD.Mensagem(textoVazio);
            return;
        }

        Inventario inv = jogador.GetComponent<Inventario>();
        if (!inv.Adicionar(itemGuardado.Clonar()))
        {
            HUD.Mensagem("Tem algo com ele, mas seu inventário está cheio.");
            return;
        }

        temItem = false;
        jogador.TocarSom(somPegar);
        HUD.Mensagem("Encontrou: " + CatalogoItens.Nome(itemGuardado));
        if (itemGuardado.documento != null)
            LeituraUI.Abrir(itemGuardado.documento.titulo, itemGuardado.documento.texto, null);
    }
}
