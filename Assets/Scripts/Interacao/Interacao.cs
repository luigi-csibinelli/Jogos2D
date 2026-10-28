using UnityEngine;

// No jogador: acha o interagivel mais proximo, mostra o aviso e usa com [E].
public class Interacao : MonoBehaviour
{
    public float raio = 0.9f;
    public LayerMask camadaInteragivel;

    Jogador jogador;
    Interagivel alvo;

    void Awake()
    {
        jogador = GetComponent<Jogador>();
    }

    void Update()
    {
        alvo = null;
        if (GameController.PodeAgir && !jogador.Morto) alvo = ProcurarAlvo();
        HUD.MostrarAviso(alvo);

        if (alvo != null && Input.GetKeyDown(KeyCode.E)) alvo.Interagir(jogador);
    }

    Interagivel ProcurarAlvo()
    {
        Vector2 centro = transform.position;
        Collider2D[] cols = Physics2D.OverlapCircleAll(centro, raio, camadaInteragivel);
        Interagivel melhor = null;
        float menor = float.MaxValue;
        foreach (Collider2D c in cols)
        {
            Interagivel i = c.GetComponentInParent<Interagivel>();
            if (i == null || !i.Disponivel) continue;
            float d = Vector2.Distance(centro, c.ClosestPoint(centro));
            if (d < menor)
            {
                menor = d;
                melhor = i;
            }
        }
        return melhor;
    }
}
