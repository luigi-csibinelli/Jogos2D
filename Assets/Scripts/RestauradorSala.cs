using UnityEngine;

// Recoloca na sala os itens que o jogador largou aqui antes.
public class RestauradorSala : MonoBehaviour
{
    public GameObject prefabItemNoChao;

    void Start()
    {
        foreach (EstadoSalas.ItemLargado r in EstadoSalas.LargadosNaSala().ToArray())
        {
            ItemNoChao novo = ItemNoChao.Criar(prefabItemNoChao, r.item, r.posicao, false);
            novo.registro = r;
        }
    }
}
