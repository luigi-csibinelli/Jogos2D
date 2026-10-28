using System.Collections;
using UnityEngine;

// Trigger no topo/pe da escada: escurece a tela e leva o jogador para outro andar.
// Cada andar e uma area da cena com seus proprios limites de camera.
public class Escada : MonoBehaviour
{
    public Transform destino;
    public Vector2 limiteMin;     // limites da camera no andar de destino
    public Vector2 limiteMax;
    public string mensagem;
    public AudioClip somPassos;

    static bool emTransicao;

    void OnTriggerEnter2D(Collider2D outro)
    {
        Jogador j = outro.GetComponentInParent<Jogador>();
        if (j == null || j.Morto || emTransicao || !GameController.PodeAgir) return;
        StartCoroutine(Transicao(j));
    }

    IEnumerator Transicao(Jogador j)
    {
        emTransicao = true;
        GameController.AbriuPainel();   // jogador fica parado durante o fade
        j.TocarSom(somPassos);
        yield return HUD.Fade(1f, 0.3f);
        j.Teleportar(destino.position);
        CameraSegue.MudarArea(limiteMin, limiteMax, destino.position);
        yield return HUD.Fade(0f, 0.3f);
        GameController.FechouPainel();
        emTransicao = false;
        if (!string.IsNullOrEmpty(mensagem)) HUD.Mensagem(mensagem);
    }

    void OnDestroy()
    {
        emTransicao = false;
    }
}
