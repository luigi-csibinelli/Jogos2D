using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Guarda, por sala, os itens que o jogador largou e os que ja pegou.
// Serve para quando houver varias salas: ao voltar, tudo continua onde ficou.
public static class EstadoSalas
{
    public class ItemLargado
    {
        public ItemInstancia item;
        public Vector2 posicao;
    }

    static readonly Dictionary<string, List<ItemLargado>> largados = new Dictionary<string, List<ItemLargado>>();
    static readonly HashSet<string> coletados = new HashSet<string>();

    static string Sala => SceneManager.GetActiveScene().name;

    public static ItemLargado Registrar(ItemInstancia item, Vector2 posicao)
    {
        if (!largados.TryGetValue(Sala, out List<ItemLargado> lista))
        {
            lista = new List<ItemLargado>();
            largados[Sala] = lista;
        }
        ItemLargado registro = new ItemLargado { item = item, posicao = posicao };
        lista.Add(registro);
        return registro;
    }

    public static void Remover(ItemLargado registro)
    {
        if (registro != null && largados.TryGetValue(Sala, out List<ItemLargado> lista)) lista.Remove(registro);
    }

    public static List<ItemLargado> LargadosNaSala()
    {
        return largados.TryGetValue(Sala, out List<ItemLargado> lista) ? lista : new List<ItemLargado>();
    }

    public static void MarcarColetado(string id)
    {
        if (!string.IsNullOrEmpty(id)) coletados.Add(Sala + "/" + id);
    }

    public static bool FoiColetado(string id)
    {
        return !string.IsNullOrEmpty(id) && coletados.Contains(Sala + "/" + id);
    }

    public static void Limpar()
    {
        largados.Clear();
        coletados.Clear();
    }
}
