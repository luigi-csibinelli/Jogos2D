using UnityEngine;

// Nomes, descricoes e icones de cada tipo de item.
public class CatalogoItens : MonoBehaviour
{
    public static CatalogoItens instancia;

    public Sprite iconePistola;
    public Sprite iconeMunicao;
    public Sprite iconeFaca;
    public Sprite iconeSpray;
    public Sprite iconeDocumento;
    public Sprite iconeChave;

    void Awake()
    {
        instancia = this;
    }

    public Sprite Icone(ItemInstancia i)
    {
        switch (i.tipo)
        {
            case TipoItem.Pistola: return iconePistola;
            case TipoItem.Municao: return iconeMunicao;
            case TipoItem.Faca: return iconeFaca;
            case TipoItem.Spray: return iconeSpray;
            case TipoItem.Documento: return iconeDocumento;
            default: return iconeChave;
        }
    }

    public static string Nome(ItemInstancia i)
    {
        switch (i.tipo)
        {
            case TipoItem.Pistola: return "Pistola 9mm";
            case TipoItem.Municao: return "Munição 9mm";
            case TipoItem.Faca: return "Faca de combate";
            case TipoItem.Spray: return "Spray de primeiros socorros";
            case TipoItem.Documento: return i.documento != null ? i.documento.titulo : "Documento";
            default: return "Chave do Brasão";
        }
    }

    public static string Descricao(ItemInstancia i)
    {
        switch (i.tipo)
        {
            case TipoItem.Pistola:
                return "Pistola padrão do DHS. Pente de 7 balas.\nNo pente: " + i.balasNoPente + ".";
            case TipoItem.Municao:
                return "Balas 9mm. Use [R] para recarregar.\nQuantidade: " + i.quantidade + ".";
            case TipoItem.Faca:
                return "Afiada, mas não vai durar para sempre. Finalize zumbis caídos com ela.\nUsos restantes: " + i.durabilidade + ".";
            case TipoItem.Spray:
                return "Fecha ferimentos e alivia a dor. Recupera boa parte da saúde.";
            case TipoItem.Documento:
                return "Um documento. Ocupa um espaço, mas pode ser relido.";
            default:
                return "Chave ornamentada com uma serpente mordendo a própria cauda.";
        }
    }

    // texto curto do aviso "[E] Pegar ..."
    public static string NomeCurto(ItemInstancia i)
    {
        if (i.tipo == TipoItem.Municao) return "munição (" + i.quantidade + ")";
        if (i.tipo == TipoItem.Documento) return "documento";
        return Nome(i).ToLower();
    }
}
