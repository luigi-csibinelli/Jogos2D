using System.Collections.Generic;
using UnityEngine;

namespace Sanatorio
{
    /// <summary>
    /// Grade de navegação + campo de fluxo. Um BFS a partir do tile do jogador
    /// dá a distância de cada tile até ele; cada zumbi só olha os vizinhos e
    /// anda para o menor. Custa o mesmo para 1 ou 24 zumbis.
    /// Portas fechadas são parede; abrir uma porta libera os tiles dela.
    /// </summary>
    public class NavGrid
    {
        public const int Unreachable = ushort.MaxValue;

        public readonly int Width;
        public readonly int Height;
        private readonly bool[,] walkable;
        private readonly ushort[,] dist;
        private readonly Queue<Vector2Int> queue = new Queue<Vector2Int>();
        private Vector2Int target = new Vector2Int(-999, -999);
        private bool dirty = true;

        private static readonly Vector2Int[] Dir8 =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
        };

        public Vector2Int Target => target;

        public NavGrid(MapData map)
        {
            Width = map.Width;
            Height = map.Height;
            walkable = new bool[Width, Height];
            dist = new ushort[Width, Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    walkable[x, y] = map.IsWalkable(x, y);
                }
            }
        }

        /// <summary>Construtor para testes: grade livre de um tamanho qualquer.</summary>
        public NavGrid(bool[,] walk)
        {
            Width = walk.GetLength(0);
            Height = walk.GetLength(1);
            walkable = (bool[,])walk.Clone();
            dist = new ushort[Width, Height];
        }

        public bool IsWalkable(Vector2Int t) => t.x >= 0 && t.y >= 0 && t.x < Width && t.y < Height && walkable[t.x, t.y];

        public void SetWalkable(Vector2Int t, bool value)
        {
            if (t.x >= 0 && t.y >= 0 && t.x < Width && t.y < Height && walkable[t.x, t.y] != value)
            {
                walkable[t.x, t.y] = value;
                dirty = true;
            }
        }

        public int DistanceAt(Vector2Int t)
        {
            if (t.x < 0 || t.y < 0 || t.x >= Width || t.y >= Height) return Unreachable;
            return dist[t.x, t.y];
        }

        /// <summary>Recalcula se o alvo mudou de tile ou se alguma porta abriu.</summary>
        public void Update(Vector2Int newTarget)
        {
            if (!dirty && newTarget == target)
            {
                return;
            }

            target = newTarget;
            dirty = false;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    dist[x, y] = Unreachable;
                }
            }

            if (!IsWalkable(target))
            {
                // Jogador em cima de algo estranho (borda de porta): parte do vizinho livre.
                foreach (Vector2Int d in Dir8)
                {
                    if (IsWalkable(target + d))
                    {
                        target += d;
                        break;
                    }
                }

                if (!IsWalkable(target)) return;
            }

            queue.Clear();
            dist[target.x, target.y] = 0;
            queue.Enqueue(target);
            while (queue.Count > 0)
            {
                Vector2Int t = queue.Dequeue();
                ushort nd = (ushort)(dist[t.x, t.y] + 1);
                for (int i = 0; i < 8; i++)
                {
                    Vector2Int d = Dir8[i];
                    Vector2Int n = t + d;
                    if (!IsWalkable(n) || dist[n.x, n.y] <= nd)
                    {
                        continue;
                    }

                    // diagonal só se não cortar quina de parede
                    if (i >= 4 && (!IsWalkable(new Vector2Int(t.x + d.x, t.y)) || !IsWalkable(new Vector2Int(t.x, t.y + d.y))))
                    {
                        continue;
                    }

                    dist[n.x, n.y] = nd;
                    queue.Enqueue(n);
                }
            }
        }

        /// <summary>
        /// Próximo tile no caminho até o alvo, ou o próprio tile se já chegou /
        /// não há caminho. Devolve false se o tile não alcança o alvo.
        /// </summary>
        public bool NextStep(Vector2Int from, out Vector2Int step)
        {
            step = from;
            int here = DistanceAt(from);
            if (here == Unreachable)
            {
                // fora da grade (ex.: entrando pela janela): procura o vizinho alcançável
                int best = Unreachable;
                foreach (Vector2Int d in Dir8)
                {
                    int v = DistanceAt(from + d);
                    if (v < best)
                    {
                        best = v;
                        step = from + d;
                    }
                }

                return best != Unreachable;
            }

            int bestDist = here;
            for (int i = 0; i < 8; i++)
            {
                Vector2Int d = Dir8[i];
                Vector2Int n = from + d;
                int v = DistanceAt(n);
                if (v >= bestDist)
                {
                    continue;
                }

                if (i >= 4 && (!IsWalkable(new Vector2Int(from.x + d.x, from.y)) || !IsWalkable(new Vector2Int(from.x, from.y + d.y))))
                {
                    continue;
                }

                bestDist = v;
                step = n;
            }

            return true;
        }
    }
}
