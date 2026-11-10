using System;
using System.Collections.Generic;

namespace Sanatorio
{
    /// <summary>
    /// Sorteio em ciclos: cada item sai uma vez, em ordem aleatória, antes de
    /// qualquer um repetir. Usado pela caixa misteriosa (pontos onde ela pode
    /// aparecer) e pelos power-ups (tipo do próximo drop).
    ///
    /// Regras:
    /// - o item "atual" conta como já visitado no ciclo em que está;
    /// - um ciclo novo nunca começa pelo item atual / último sorteado;
    /// - itens indisponíveis agora (ex.: zona trancada) são pulados, mas
    ///   continuam no ciclo para depois.
    /// </summary>
    public class ShuffleBag<T>
    {
        private readonly List<T> items;
        private readonly List<T> remaining = new List<T>();
        private readonly Random rng;
        private readonly IEqualityComparer<T> eq = EqualityComparer<T>.Default;

        public int CycleCount { get; private set; }
        public IReadOnlyList<T> Remaining => remaining;

        private readonly bool currentCountsAsVisited;

        /// <param name="currentCountsAsVisited">
        /// true (caixa): o ciclo novo não inclui o item atual, porque a caixa já está lá.
        /// false (power-ups): o ciclo novo tem todos os itens, só não começa pelo último.
        /// </param>
        public ShuffleBag(IEnumerable<T> items, int seed, bool currentCountsAsVisited = false)
        {
            this.items = new List<T>(items);
            rng = new Random(seed);
            this.currentCountsAsVisited = currentCountsAsVisited;
        }

        /// <summary>Começa o primeiro ciclo já "em" <paramref name="current"/>.</summary>
        public void Begin(T current)
        {
            Refill(current, true);
            if (currentCountsAsVisited)
            {
                remaining.RemoveAll(x => eq.Equals(x, current));
            }
        }

        public void Begin()
        {
            Refill(default, false);
        }

        /// <summary>
        /// Próximo item. <paramref name="available"/> pode ser null.
        /// Devolve false se nenhum item disponível existir (nem num ciclo novo).
        /// </summary>
        public bool TryNext(T current, bool hasCurrent, Func<T, bool> available, out T result)
        {
            for (int i = 0; i < remaining.Count; i++)
            {
                T candidate = remaining[i];
                if (hasCurrent && currentCountsAsVisited && eq.Equals(candidate, current))
                {
                    continue;
                }

                if (available == null || available(candidate))
                {
                    remaining.RemoveAt(i);
                    result = candidate;
                    return true;
                }
            }

            // Ciclo acabou (ou só sobraram itens indisponíveis): ciclo novo.
            Refill(current, hasCurrent);
            for (int i = 0; i < remaining.Count; i++)
            {
                T candidate = remaining[i];
                if (available == null || available(candidate))
                {
                    remaining.RemoveAt(i);
                    result = candidate;
                    return true;
                }
            }

            result = default;
            return false;
        }

        public T Next(T last, bool hasLast)
        {
            TryNext(last, hasLast, null, out T r);
            return r;
        }

        private void Refill(T exclude, bool hasExclude)
        {
            remaining.Clear();
            foreach (T item in items)
            {
                if (!hasExclude || !currentCountsAsVisited || !eq.Equals(item, exclude))
                {
                    remaining.Add(item);
                }
            }

            for (int i = remaining.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (remaining[i], remaining[j]) = (remaining[j], remaining[i]);
            }

            // nunca começa o ciclo repetindo o último item
            if (hasExclude && remaining.Count > 1 && eq.Equals(remaining[0], exclude))
            {
                int j = 1 + rng.Next(remaining.Count - 1);
                (remaining[0], remaining[j]) = (remaining[j], remaining[0]);
            }

            CycleCount++;
        }
    }
}
