using UnityEngine;

// Segue o jogador, puxa um pouco na direcao da mira e respeita os limites da sala.
public class CameraSegue : MonoBehaviour
{
    static CameraSegue instancia;

    public Transform alvo;
    public Vector2 limiteMin;
    public Vector2 limiteMax;
    public float suavidade = 8f;
    public float puxaMira = 0.2f;

    Camera cam;
    float tremor;
    float tempoTremor;

    void Awake()
    {
        instancia = this;
        cam = GetComponent<Camera>();
    }

    public static void Tremer(float intensidade, float duracao)
    {
        if (instancia == null) return;
        instancia.tremor = Mathf.Max(instancia.tremor, intensidade);
        instancia.tempoTremor = Mathf.Max(instancia.tempoTremor, duracao);
    }

    // troca de andar: novos limites e a camera ja aparece no lugar certo
    public static void MudarArea(Vector2 min, Vector2 max, Vector2 posicao)
    {
        if (instancia == null) return;
        instancia.limiteMin = min;
        instancia.limiteMax = max;
        instancia.transform.position = instancia.Limitar(posicao);
    }

    Vector3 Limitar(Vector2 p)
    {
        float meiaAltura = cam.orthographicSize;
        float meiaLargura = meiaAltura * cam.aspect;
        p.x = Limita(p.x, limiteMin.x + meiaLargura, limiteMax.x - meiaLargura);
        p.y = Limita(p.y, limiteMin.y + meiaAltura, limiteMax.y - meiaAltura);
        return new Vector3(p.x, p.y, -10);
    }

    void LateUpdate()
    {
        if (alvo == null) return;

        Vector2 destino = alvo.position;
        Jogador j = Jogador.instancia;
        if (j != null && !j.Morto && GameController.PodeAgir)
            destino += Vector2.ClampMagnitude(j.PosicaoMira - j.Peito, 5f) * puxaMira;

        Vector2 atual = Vector2.Lerp(transform.position, destino, 1f - Mathf.Exp(-suavidade * Time.unscaledDeltaTime));

        atual = Limitar(atual);

        Vector2 extra = Vector2.zero;
        if (tempoTremor > 0)
        {
            tempoTremor -= Time.unscaledDeltaTime;
            extra = Random.insideUnitCircle * tremor;
            if (tempoTremor <= 0) tremor = 0;
        }
        transform.position = new Vector3(atual.x + extra.x, atual.y + extra.y, -10);
    }

    static float Limita(float v, float min, float max)
    {
        // sala menor que a tela nesse eixo: centraliza
        if (min > max) return (min + max) / 2f;
        return Mathf.Clamp(v, min, max);
    }
}
