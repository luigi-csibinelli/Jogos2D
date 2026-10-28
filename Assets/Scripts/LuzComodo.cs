using UnityEngine;
using UnityEngine.Rendering.Universal;

// Abajur, candelabro etc.: troca o sprite e liga a luz 2D.
public class LuzComodo : MonoBehaviour
{
    public SpriteRenderer sr;
    public Sprite apagado;
    public Sprite aceso;
    public Light2D luz;

    public void Definir(bool ligado)
    {
        if (sr != null) sr.sprite = ligado ? aceso : apagado;
        if (luz != null) luz.enabled = ligado;
    }
}
