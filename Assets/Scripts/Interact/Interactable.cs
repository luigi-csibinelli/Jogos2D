using System.Collections.Generic;
using UnityEngine;

namespace Sanatorio
{
    /// <summary>
    /// Qualquer coisa com "Aperte F": armas na parede, portas, refrigerantes,
    /// caixa, disjuntor, alavancas, janelas, privadas e fichas. O jogador
    /// mostra o aviso do mais próximo e chama Use (ou HoldTick, se for segurar).
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        /// <summary>De onde o jogador interage (o tile de piso na frente).</summary>
        public Vector2 Point;
        public float Radius = Balance.InteractRadius;
        public virtual bool Hold => false;

        /// <summary>Texto do aviso, ou null se não há nada a fazer agora. cost = -1 sem preço.</summary>
        public abstract string Prompt(Player p, out int cost);

        public virtual void Use(Player p) { }

        public virtual void HoldTick(Player p, float dt) { }

        protected Game game;

        protected virtual void Awake()
        {
            game = Game.I;
            All.Add(this);
        }

        protected virtual void OnDestroy()
        {
            All.Remove(this);
        }

        protected static string Key(Player p) => p.Input.InteractLabel;
    }
}
