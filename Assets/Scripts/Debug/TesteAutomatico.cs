using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Roteiro de teste da Sala 01. So roda quando a URL do WebGL tem "?teste=1"
// ou quando o teste do editor (Assets/Editor/TesteSala.cs) liga a chave "teste_forcar".
// Escreve "TESTE ok/FALHA" no console e guarda o resultado em PlayerPrefs.
public class TesteAutomatico : MonoBehaviour
{
    static bool ativo;
    int ok;
    int falhas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        ativo = Application.absoluteURL.Contains("teste=1") || PlayerPrefs.GetInt("teste_forcar", 0) == 1;
        if (!ativo) return;
        SceneManager.sceneLoaded += (cena, modo) => Verificar(cena);
        Verificar(SceneManager.GetActiveScene());
    }

    static void Verificar(Scene cena)
    {
        if (cena.name == "MainMenu") SceneManager.LoadScene("Sala01_Saguao");
        else if (cena.name == "Sala01_Saguao" && FindAnyObjectByType<TesteAutomatico>() == null)
            new GameObject("TesteAutomatico").AddComponent<TesteAutomatico>();
    }

    void Checa(bool condicao, string descricao)
    {
        if (condicao) ok++;
        else falhas++;
        Debug.Log((condicao ? "TESTE ok    " : "TESTE FALHA ") + descricao);
    }

    static T Achar<T>(string nome) where T : Component
    {
        foreach (T c in FindObjectsByType<T>(FindObjectsSortMode.None))
            if (c.gameObject.name == nome) return c;
        return null;
    }

    static bool TemDocumentoNoChao(string titulo)
    {
        foreach (ItemNoChao i in FindObjectsByType<ItemNoChao>(FindObjectsSortMode.None))
            if (i.item.documento != null && i.item.documento.titulo == titulo) return true;
        return false;
    }

    // empurra o jogador (com a fisica) contra uma parede e confere onde ele parou
    IEnumerator EmpurrarContraParede(Jogador j, Vector2 inicio, Vector2 direcao, System.Func<Vector2, bool> ficouDoLadoCerto, string descricao)
    {
        j.enabled = false;
        Rigidbody2D rb = j.GetComponent<Rigidbody2D>();
        j.Teleportar(inicio);
        for (int i = 0; i < 60; i++)
        {
            rb.MovePosition(rb.position + direcao * 0.12f);
            yield return new WaitForFixedUpdate();
        }
        Checa(ficouDoLadoCerto(rb.position), descricao + " (parou em " + rb.position.ToString("0.0") + ")");
        j.enabled = true;
    }

    IEnumerator Start()
    {
        ativo = false;   // o teste roda uma vez so
        yield return new WaitForSeconds(0.5f);

        Jogador j = Jogador.instancia;
        Inventario inv = j.GetComponent<Inventario>();
        Arma arma = j.GetComponent<Arma>();
        Zumbi z = FindAnyObjectByType<Zumbi>();
        Musica musica = Musica.instancia;

        // ---------- luzes
        Interruptor inte = FindAnyObjectByType<Interruptor>();
        inte.Interagir(j);
        Checa(inte.secao.luzesAcesas, "interruptor acende as luzes");

        // ---------- pistola acorda o zumbi e liga a trilha tensa
        ItemNoChao pistola = null;
        foreach (ItemNoChao i in FindObjectsByType<ItemNoChao>(FindObjectsSortMode.None))
            if (i.item.tipo == TipoItem.Pistola) pistola = i;
        Checa(z.estado == Zumbi.EstadoZumbi.Dormindo, "zumbi comeca deitado");
        pistola.Interagir(j);
        yield return null;
        ItemInstancia p = inv.Encontrar(TipoItem.Pistola);
        Checa(p != null && p.balasNoPente == 7, "pegou a pistola com 7 balas");
        Checa(z.estado == Zumbi.EstadoZumbi.Levantando, "zumbi levanta ao pegar a pistola");
        Checa(musica.tensa.isPlaying, "trilha tensa toca quando o zumbi levanta");
        yield return new WaitForSeconds(1.2f);
        Checa(z.estado == Zumbi.EstadoZumbi.Perseguindo, "zumbi passa a perseguir");

        // ---------- tiros com dano aleatorio ate cair a 40%
        int tiros = 0;
        while (z.estado == Zumbi.EstadoZumbi.Perseguindo && tiros < 10)
        {
            z.LevarDano(arma.danoBase * Random.Range(arma.multiplicadorMin, arma.multiplicadorMax), Vector2.right, z.transform.position + Vector3.up * 0.5f);
            tiros++;
        }
        Debug.Log("TESTE info  tiros para derrubar: " + tiros + ", vida restante: " + z.vida.ToString("0"));
        Checa(z.estado == Zumbi.EstadoZumbi.Caido, "zumbi cai ao chegar a 40% da vida");
        Checa(tiros >= 3 && tiros <= 5, "derrubar leva cerca de 3 a 4 tiros");
        yield return new WaitForSeconds(1.6f);
        Checa(musica.tensa.volume < 0.05f, "trilha volta a ambiente quando o zumbi cai");

        // ---------- sem finalizar: levanta de novo e a trilha tensa recomeca
        int reiniciosAntes = musica.reinicios;
        yield return new WaitForSeconds(3.2f);
        Checa(z.estado == Zumbi.EstadoZumbi.Levantando || z.estado == Zumbi.EstadoZumbi.Perseguindo, "zumbi levanta se nao for finalizado");
        Checa(musica.tensa.isPlaying && musica.reinicios == reiniciosAntes + 1, "trilha tensa recomeca quando ele levanta de novo");

        // ---------- segundo zumbi: finalizacao com a faca
        Zumbi z2 = Instantiate(z.gameObject, new Vector3(20, 3, 0), Quaternion.identity).GetComponent<Zumbi>();
        z2.estado = Zumbi.EstadoZumbi.Dormindo;
        z2.vida = z2.vidaMax;
        yield return null;
        z2.Acordar();
        yield return new WaitForSeconds(1.1f);
        while (z2.estado == Zumbi.EstadoZumbi.Perseguindo) z2.LevarDano(17f, Vector2.left, z2.transform.position);
        Checa(z2.estado == Zumbi.EstadoZumbi.Caido, "segundo zumbi caido");
        j.Teleportar(z2.transform.position + Vector3.right * 0.6f);
        int facaAntes = inv.Encontrar(TipoItem.Faca).durabilidade;
        arma.Facada();
        Checa(z2.estado == Zumbi.EstadoZumbi.Morto, "faca finaliza o zumbi caido");
        Checa(inv.Encontrar(TipoItem.Faca).durabilidade == facaAntes - 1, "finalizacao gasta so 1 uso da faca");

        // ---------- primeiro zumbi: termina no tiro
        while (z.estado != Zumbi.EstadoZumbi.Morto && z.vida > -50) z.LevarDano(17f, Vector2.right, z.transform.position);
        Checa(z.estado == Zumbi.EstadoZumbi.Morto, "primeiro zumbi morre no tiro depois de levantar");

        // ---------- municao escondida no relogio; vaso vazio
        Esconderijo relogio = Achar<Esconderijo>("Relogio");
        relogio.Interagir(j);
        Checa(inv.Contar(TipoItem.Municao) == relogio.item.quantidade, "relogio do saguao esconde municao");
        relogio.Interagir(j);
        Checa(inv.Contar(TipoItem.Municao) == relogio.item.quantidade, "relogio vasculhado nao da municao de novo");
        int antesVaso = inv.itens.Count;
        Achar<Esconderijo>("Vaso").Interagir(j);
        Checa(inv.itens.Count == antesVaso, "vaso vazio nao da nada");
        Checa(FindAnyObjectByType<ItemNoChao>() != null && TemDocumentoNoChao("Bilhete do mordomo"), "bilhete do mordomo esta no saguao");

        // ---------- documento no corpo do Okafor
        Corpo corpo = FindAnyObjectByType<Corpo>();
        corpo.Interagir(j);
        Checa(inv.Encontrar(TipoItem.Documento) != null, "documento do Okafor vai para o inventario");
        Checa(LeituraUI.Aberta, "documento abre para leitura");
        FindAnyObjectByType<LeituraUI>().Fechar();
        Checa(GameController.paineisAbertos == 0, "fechar a leitura libera o jogador");

        // ---------- portas trancadas sem chave
        Porta norte = Achar<Porta>("Porta Norte");
        Porta sul = Achar<Porta>("Porta Sul");
        Porta frente = Achar<Porta>("Porta da Frente (leste)");
        norte.Interagir(j);
        frente.Interagir(j);
        Checa(!norte.aberta && !frente.aberta, "portas do terreo trancadas sem a chave");

        // ---------- escada leva para a galeria
        Escada sobe = Achar<Escada>("Escada Sobe");
        j.Teleportar(sobe.transform.position + Vector3.right * 1.2f);
        yield return new WaitForSeconds(0.2f);
        j.Teleportar(sobe.transform.position);
        yield return new WaitForSeconds(1f);
        Checa(j.transform.position.y > 30, "escada leva o jogador ao 1o andar");
        yield return new WaitForSeconds(4f);   // tempo para olhar a galeria

        // ---------- paredes, vao e corrimao do 1o andar tem colisao
        int parede = LayerMask.GetMask("Parede");
        Checa(Physics2D.OverlapPoint(new Vector2(0.5f, 9.5f), parede) != null, "parede oeste do saguao e solida");
        Checa(Physics2D.OverlapPoint(new Vector2(10.5f, 13.5f), parede) != null, "parede norte do saguao e solida");
        Checa(Physics2D.OverlapPoint(new Vector2(0.5f, 45.5f), parede) != null, "parede oeste da galeria e solida");
        Checa(Physics2D.OverlapPoint(new Vector2(20.5f, 51.5f), parede) != null, "parede norte da galeria e solida");
        Checa(Physics2D.OverlapPoint(new Vector2(20.5f, 40.5f), parede) != null, "vao do saguao e solido");
        Checa(Physics2D.OverlapPoint(new Vector2(6.5f, 44.5f), parede) != null, "balaustrada e solida");
        yield return EmpurrarContraParede(j, new Vector2(3f, 44f), Vector2.left, v => v.x > 1.1f, "jogador nao atravessa a parede oeste da galeria");
        yield return EmpurrarContraParede(j, new Vector2(3f, 44f), Vector2.right, v => v.x < 6.1f, "jogador nao cai no vao pela balaustrada");
        yield return EmpurrarContraParede(j, new Vector2(15f, 49f), Vector2.up, v => v.y < 51.1f, "jogador nao atravessa a parede norte da galeria");
        yield return EmpurrarContraParede(j, new Vector2(15f, 33f), Vector2.down, v => v.y > 30.9f, "jogador nao atravessa a parede sul da galeria");
        j.Teleportar(new Vector2(4.3f, 40.5f));

        // ---------- municao escondida na estante da galeria
        int antesEstante = inv.Contar(TipoItem.Municao);
        foreach (Esconderijo e in FindObjectsByType<Esconderijo>(FindObjectsSortMode.None))
            if (e.temItem && e.nomeObjeto == "estante") e.Interagir(j);
        Checa(inv.Contar(TipoItem.Municao) > antesEstante, "estante da galeria esconde municao");

        // ---------- cofre
        Cofre cofre = FindAnyObjectByType<Cofre>();
        Checa(!cofre.TentarCodigo("1941"), "codigo errado (ano de nascimento) nao abre");
        Checa(cofre.TentarCodigo("1987"), "codigo 1987 abre o cofre");
        cofre.Abrir();
        Checa(inv.TemChave("brasao"), "chave do brasao vai para o inventario");

        // ---------- porta da galeria com outra fechadura
        Porta lua = Achar<Porta>("Porta Galeria Norte");
        lua.Interagir(j);
        Checa(!lua.aberta, "porta da galeria (lua) continua trancada");

        // ---------- largar item e inventario cheio
        ItemInstancia doc = inv.Encontrar(TipoItem.Documento);
        int itensAntes = FindObjectsByType<ItemNoChao>(FindObjectsSortMode.None).Length;
        inv.Largar(doc);
        yield return null;
        Checa(inv.Encontrar(TipoItem.Documento) == null, "documento saiu do inventario ao largar");
        Checa(FindObjectsByType<ItemNoChao>(FindObjectsSortMode.None).Length == itensAntes + 1, "documento largado aparece no chao");
        while (!inv.Cheio) inv.Adicionar(new ItemInstancia { tipo = TipoItem.Spray });
        ItemNoChao municao = null;
        foreach (ItemNoChao i in FindObjectsByType<ItemNoChao>(FindObjectsSortMode.None))
            if (i.item.tipo == TipoItem.Documento) municao = i;
        municao.Interagir(j);
        yield return null;
        Checa(municao != null, "com inventario cheio o item fica no chao");
        while (inv.Encontrar(TipoItem.Spray) != null) inv.Remover(inv.Encontrar(TipoItem.Spray));

        // ---------- volta ao terreo e abre as duas portas; a chave e descartada no fim
        Escada desce = Achar<Escada>("Escada Desce");
        j.Teleportar(desce.transform.position);
        yield return new WaitForSeconds(1f);
        Checa(j.transform.position.y < 16, "escada leva de volta ao terreo");
        norte.Interagir(j);
        Checa(norte.aberta && inv.TemChave("brasao"), "porta norte abre e a chave continua (falta a sul)");
        sul.Interagir(j);
        Checa(sul.aberta && inv.TemChave("brasao"), "porta sul abre e a chave NAO e descartada sozinha");
        inv.Largar(inv.EncontrarChave("brasao"));
        Checa(!inv.TemChave("brasao"), "jogador pode largar a chave quando quiser");
        frente.Interagir(j);
        Checa(!frente.aberta, "porta da frente continua emperrada");

        // ---------- dano: manca e morre
        j.TomarDano(40, j.transform.position + Vector3.left);
        Checa(j.condicao == Jogador.Condicao.Cuidado, "com 60 de vida o jogador fica em Cuidado");
        yield return new WaitForSeconds(1f);
        j.TomarDano(30, j.transform.position + Vector3.left);
        Checa(j.condicao == Jogador.Condicao.Perigo, "com 30 de vida o jogador fica em Perigo");
        yield return new WaitForSeconds(1f);
        j.TomarDano(40, j.transform.position + Vector3.left);
        Checa(GameController.estado == GameController.Estado.Morto, "vida zerada leva a tela de morte");

        Debug.Log("TESTE_FIM ok=" + ok + " falhas=" + falhas);
        PlayerPrefs.SetInt("teste_ok", ok);
        PlayerPrefs.SetInt("teste_falhas", falhas);
        PlayerPrefs.SetInt("teste_fim", 1);
        PlayerPrefs.Save();
    }
}
