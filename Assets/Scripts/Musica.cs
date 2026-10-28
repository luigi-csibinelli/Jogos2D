using UnityEngine;

// Trilha dinamica: ambiente normalmente, tensa enquanto um zumbi esta de pe perseguindo.
public class Musica : MonoBehaviour
{
    public static Musica instancia;

    public AudioSource ambiente;
    public AudioSource tensa;
    public float volume = 0.5f;
    public float velocidadeFade = 1.5f;

    public int reinicios { get; private set; }   // quantas vezes a trilha tensa comecou

    float alvoAmbiente;
    float alvoTensa;

    void Awake()
    {
        instancia = this;
    }

    void OnEnable()
    {
        Zumbi.AoLevantar += ZumbiLevantou;
        Zumbi.AoCair += ZumbiSaiu;
        Zumbi.AoMorrer += ZumbiSaiu;
    }

    void OnDisable()
    {
        Zumbi.AoLevantar -= ZumbiLevantou;
        Zumbi.AoCair -= ZumbiSaiu;
        Zumbi.AoMorrer -= ZumbiSaiu;
    }

    void Start()
    {
        ambiente.volume = volume;
        tensa.volume = 0;
        alvoAmbiente = volume;
        alvoTensa = 0;
        ambiente.Play();
    }

    void ZumbiLevantou(Zumbi z)
    {
        // a trilha tensa sempre recomeca do inicio quando um zumbi levanta
        // (Stop + Play volta ao comeco; mexer em AudioSource.time nao e confiavel no WebGL)
        tensa.Stop();
        tensa.volume = volume;
        tensa.Play();
        reinicios++;
        alvoTensa = volume;
        alvoAmbiente = 0;
    }

    void ZumbiSaiu(Zumbi z)
    {
        foreach (Zumbi outro in Zumbi.todos)
            if (outro != z && outro.Ameacando) return;
        alvoTensa = 0;
        alvoAmbiente = volume;
        if (!ambiente.isPlaying) ambiente.Play();
    }

    public void Parar()
    {
        alvoAmbiente = 0;
        alvoTensa = 0;
    }

    void Update()
    {
        float passo = velocidadeFade * Time.unscaledDeltaTime;
        ambiente.volume = Mathf.MoveTowards(ambiente.volume, alvoAmbiente, passo);
        tensa.volume = Mathf.MoveTowards(tensa.volume, alvoTensa, passo);
        if (tensa.isPlaying && tensa.volume <= 0 && alvoTensa <= 0) tensa.Stop();
    }
}
