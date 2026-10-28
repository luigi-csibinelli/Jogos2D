using UnityEngine;
using UnityEngine.Rendering.Universal;

// Chama de vela: a luz oscila e o sprite alterna entre dois quadros.
public class LuzTremula : MonoBehaviour
{
    public Light2D luz;
    public float variacao = 0.25f;
    public float velocidade = 6f;
    public SpriteRenderer sr;
    public Sprite[] quadros;

    float baseIntensidade;
    float baseRaio;
    float semente;

    void Start()
    {
        baseIntensidade = luz.intensity;
        baseRaio = luz.pointLightOuterRadius;
        semente = Random.value * 100f;
    }

    void Update()
    {
        float n = Mathf.PerlinNoise(semente, Time.time * velocidade);
        luz.intensity = baseIntensidade * (1f - variacao + n * variacao * 2f);
        luz.pointLightOuterRadius = baseRaio * (0.95f + n * 0.1f);
        if (sr != null && quadros != null && quadros.Length > 1)
            sr.sprite = quadros[(int)(Time.time * 5f + semente) % quadros.Length];
    }
}
