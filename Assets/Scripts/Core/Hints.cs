using UnityEngine;

namespace Sanatorio
{
    /// <summary>
    /// Dicas de primeira vez, no momento em que a mecânica aparece: ensina
    /// jogando em vez de um tutorial na frente. Cada dica aparece uma vez por
    /// partida e some depois da primeira partida concluída.
    /// </summary>
    public class Hints : MonoBehaviour
    {
        private Game game;
        private bool moveHint, barricadeHint, buyHint, doorHint, powerHint, boxHint, lowAmmoHint;
        private float t;

        private void Start()
        {
            game = Game.I;
            if (SaveData.GamesPlayed > 1)
            {
                enabled = false;
            }
        }

        private void Update()
        {
            if (!game.IsPlaying || game.Player == null) return;
            t += Time.deltaTime;
            Player p = game.Player;

            if (!moveHint && t > 1.5f)
            {
                moveHint = true;
                Show(p.Input.UsingGamepad
                    ? "ANALÓGICO ESQUERDO ANDA, DIREITO MIRA, RT ATIRA"
                    : "WASD ANDA, MOUSE MIRA, CLIQUE ATIRA, V FACA");
            }

            if (!barricadeHint && game.World.AnyBarricadeBroken)
            {
                barricadeHint = true;
                Show($"SEGURE {p.Input.InteractLabel} NA JANELA PARA RECONSTRUIR (+10)");
            }

            if (!buyHint && game.Points >= 800 && t > 20f)
            {
                buyHint = true;
                Show("GIZ NA PAREDE = ARMA À VENDA. CHEGUE PERTO PARA VER O PREÇO");
            }

            if (!doorHint && game.Points >= 1000 && game.Rounds.Round >= 2)
            {
                doorHint = true;
                Show("PORTAS CUSTAM PONTOS. O DISJUNTOR FICA NA SALA DO GERADOR");
            }

            if (!powerHint && game.Zones.IsUnlocked(3) && !game.Power)
            {
                powerHint = true;
                Show("LIGUE O DISJUNTOR: REFRIGERANTES, ARMADILHAS E A PORTA ELÉTRICA");
            }

            if (!boxHint && game.World.Box != null && game.World.Box.NearPlayer)
            {
                boxHint = true;
                Show($"CAIXA MISTERIOSA: {Balance.BoxCost} PONTOS POR UMA ARMA ALEATÓRIA");
            }

            if (!lowAmmoHint && p.Inventory.Current != null && p.Inventory.Current.TotalAmmo == 0)
            {
                lowAmmoHint = true;
                Show("SEM BALA: USE A FACA OU COMPRE MUNIÇÃO NA PAREDE");
            }
        }

        private void Show(string msg)
        {
            if (game.Hud != null) game.Hud.Hint(msg);
        }
    }
}
