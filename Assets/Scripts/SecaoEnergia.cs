using UnityEngine;
using UnityEngine.Rendering.Universal;

// Luzes de uma secao da mansao (ex.: o saguao). O interruptor acende e apaga.
public class SecaoEnergia : MonoBehaviour
{
    public string id = "Saguao";
    public bool comecaComEnergia = true;
    public bool luzesAcesas;

    public Light2D luzGlobal;
    public float intensidadeApagada = 0.1f;
    public float intensidadeAcesa = 0.55f;
    public Color corApagada = new Color(0.55f, 0.6f, 0.9f);
    public Color corAcesa = new Color(1f, 0.9f, 0.75f);

    public LuzComodo[] luzes;

    float fade = 0;

    void Awake()
    {
        if (comecaComEnergia) RedeEnergia.Ligar(id);
        else RedeEnergia.Desligar(id);
    }

    void Start()
    {
        fade = luzesAcesas ? 1 : 0;
        Aplicar(true);
    }

    void OnEnable() { RedeEnergia.AoMudar += EnergiaMudou; }
    void OnDisable() { RedeEnergia.AoMudar -= EnergiaMudou; }

    void EnergiaMudou(string secao)
    {
        // sem energia as luzes apagam sozinhas
        if (secao == id && !RedeEnergia.TemEnergia(id) && luzesAcesas) Definir(false);
    }

    public bool Alternar()
    {
        if (!RedeEnergia.TemEnergia(id)) return false;
        Definir(!luzesAcesas);
        return true;
    }

    public void Definir(bool acesas)
    {
        luzesAcesas = acesas;
        Aplicar(false);
    }

    void Aplicar(bool imediato)
    {
        foreach (LuzComodo l in luzes)
            if (l != null) l.Definir(luzesAcesas);
        if (imediato) fade = luzesAcesas ? 1 : 0;
    }

    void Update()
    {
        fade = Mathf.MoveTowards(fade, luzesAcesas ? 1 : 0, Time.deltaTime * 3f);
        if (luzGlobal != null)
        {
            luzGlobal.intensity = Mathf.Lerp(intensidadeApagada, intensidadeAcesa, fade);
            luzGlobal.color = Color.Lerp(corApagada, corAcesa, fade);
        }
    }
}
