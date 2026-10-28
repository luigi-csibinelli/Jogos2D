using System;
using System.Collections.Generic;
using UnityEngine;

public enum TipoItem { Pistola, Municao, Faca, Spray, Documento, Chave }

[Serializable]
public class ItemInstancia
{
    public TipoItem tipo;
    public int quantidade = 1;      // municao
    public int balasNoPente;        // pistola
    public int durabilidade;        // faca
    public DocumentoDados documento;
    public string idChave;

    public ItemInstancia Clonar()
    {
        return (ItemInstancia)MemberwiseClone();
    }
}

// Inventario no estilo RE: poucos espacos, documentos tambem ocupam espaco.
public class Inventario : MonoBehaviour
{
    public int espacos = 6;
    public List<ItemInstancia> itens = new List<ItemInstancia>();
    public GameObject prefabItemNoChao;

    public event Action AoMudar;

    public bool Cheio => itens.Count >= espacos;

    public void AvisarMudanca()
    {
        AoMudar?.Invoke();
    }

    public bool PodeReceber(ItemInstancia item)
    {
        if (item.tipo == TipoItem.Municao && Encontrar(TipoItem.Municao) != null) return true;
        return !Cheio;
    }

    public bool Adicionar(ItemInstancia item)
    {
        if (item.tipo == TipoItem.Municao)
        {
            // municao junta na mesma pilha
            ItemInstancia pilha = Encontrar(TipoItem.Municao);
            if (pilha != null)
            {
                pilha.quantidade += item.quantidade;
                AvisarMudanca();
                return true;
            }
        }
        if (Cheio) return false;
        itens.Add(item);
        AvisarMudanca();
        return true;
    }

    public ItemInstancia Encontrar(TipoItem tipo)
    {
        foreach (ItemInstancia i in itens)
            if (i.tipo == tipo) return i;
        return null;
    }

    public bool TemChave(string id)
    {
        foreach (ItemInstancia i in itens)
            if (i.tipo == TipoItem.Chave && i.idChave == id) return true;
        return false;
    }

    public ItemInstancia EncontrarChave(string id)
    {
        foreach (ItemInstancia i in itens)
            if (i.tipo == TipoItem.Chave && i.idChave == id) return i;
        return null;
    }

    public int Contar(TipoItem tipo)
    {
        int total = 0;
        foreach (ItemInstancia i in itens)
            if (i.tipo == tipo) total += i.quantidade;
        return total;
    }

    // tira ate "quanto" unidades; devolve quantas conseguiu tirar
    public int Consumir(TipoItem tipo, int quanto)
    {
        int tirou = 0;
        for (int k = itens.Count - 1; k >= 0 && tirou < quanto; k--)
        {
            ItemInstancia i = itens[k];
            if (i.tipo != tipo) continue;
            int t = Mathf.Min(i.quantidade, quanto - tirou);
            i.quantidade -= t;
            tirou += t;
            if (i.quantidade <= 0) itens.RemoveAt(k);
        }
        AvisarMudanca();
        return tirou;
    }

    public void Remover(ItemInstancia item)
    {
        itens.Remove(item);
        AvisarMudanca();
    }

    // larga o item no chao, onde o jogador esta; ele fica la ate ser pego de novo
    public void Largar(ItemInstancia item)
    {
        if (!itens.Contains(item)) return;
        itens.Remove(item);
        Vector2 pos = (Vector2)transform.position + new Vector2(UnityEngine.Random.Range(-0.15f, 0.15f), 0.05f);
        ItemNoChao.Criar(prefabItemNoChao, item, pos, true);
        AvisarMudanca();
    }
}
