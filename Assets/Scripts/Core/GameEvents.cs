using System;
using UnityEngine;

namespace Sanatorio
{
    /// <summary>Eventos globais. HUD, áudio e efeitos ouvem; a lógica dispara.</summary>
    public static class GameEvents
    {
        public static event Action<int, int> PointsChanged;       // total, delta
        public static event Action<int> RoundStarted;
        public static event Action<int> RoundEnded;
        public static event Action PowerOn;
        public static event Action<string> Toast;                  // mensagem central curta
        public static event Action<int> PlayerHurt;                // vida restante
        public static event Action<Vector2, int> ZombieKilled;     // posição, pontos

        public static void RaisePoints(int total, int delta) => PointsChanged?.Invoke(total, delta);
        public static void RaiseRoundStarted(int r) => RoundStarted?.Invoke(r);
        public static void RaiseRoundEnded(int r) => RoundEnded?.Invoke(r);
        public static void RaisePowerOn() => PowerOn?.Invoke();
        public static void RaiseToast(string msg) => Toast?.Invoke(msg);
        public static void RaisePlayerHurt(int hp) => PlayerHurt?.Invoke(hp);
        public static void RaiseZombieKilled(Vector2 p, int pts) => ZombieKilled?.Invoke(p, pts);

        /// <summary>Limpa os inscritos ao recarregar a cena (evita chamadas em objetos destruídos).</summary>
        public static void Clear()
        {
            PointsChanged = null;
            RoundStarted = null;
            RoundEnded = null;
            PowerOn = null;
            Toast = null;
            PlayerHurt = null;
            ZombieKilled = null;
        }
    }
}
