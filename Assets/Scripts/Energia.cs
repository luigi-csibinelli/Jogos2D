using System;
using System.Collections.Generic;
using UnityEngine;

// Quais secoes da mansao tem energia. No futuro um PainelEnergia vai ligar
// secoes novas e liberar interruptores, portas eletricas, elevadores etc.
public static class RedeEnergia
{
    static readonly HashSet<string> ligadas = new HashSet<string>();

    public static event Action<string> AoMudar;

    public static bool TemEnergia(string secao)
    {
        return ligadas.Contains(secao);
    }

    public static void Ligar(string secao)
    {
        if (ligadas.Add(secao)) AoMudar?.Invoke(secao);
    }

    public static void Desligar(string secao)
    {
        if (ligadas.Remove(secao)) AoMudar?.Invoke(secao);
    }
}
