using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Sanatorio
{
    /// <summary>
    /// Lê a planta ASCII (VerrucktMap) e descobre zonas, portas, janelas e
    /// marcadores. Também valida a planta: tudo o que poderia deixar o jogador
    /// sem saída (zona inalcançável, porta solta, janela sem pátio) vira erro.
    /// </summary>
    public class MapData
    {
        public class Door
        {
            public char id;
            public int cost;
            public List<Vector2Int> tiles = new List<Vector2Int>();
            public int zoneA = -1;
            public int zoneB = -1;
            public bool IsPowerDoor => cost == 0;
        }

        public class Window
        {
            public Vector2Int tile;
            public Vector2Int inside;      // tile de piso do lado de dentro
            public Vector2Int outward;     // direção para fora
            public Vector2Int spawn;       // onde o zumbi nasce (pátio)
            public int zone;
        }

        public class Marker
        {
            public char c;
            public Vector2Int tile;
            public Vector2Int access;      // tile de piso de onde se interage
            public int zone;
        }

        public class Trap
        {
            public List<Vector2Int> tiles = new List<Vector2Int>();
            public Vector2Int lever;
            public Vector2Int leverAccess;
            public int zone;
        }

        public readonly int Width;
        public readonly int Height;
        private readonly char[,] grid;
        private readonly int[,] zone;

        public readonly List<Door> Doors = new List<Door>();
        public readonly List<Window> Windows = new List<Window>();
        public readonly List<Marker> Markers = new List<Marker>();
        public readonly List<Trap> Traps = new List<Trap>();
        public readonly Vector2Int[] ZoneSeeds = new Vector2Int[10];
        public Vector2Int SpawnWest;
        public Vector2Int SpawnEast;

        private readonly List<string> errors = new List<string>();
        public IReadOnlyList<string> Errors => errors;

        private static readonly Vector2Int[] Dir4 =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        };

        public MapData(string[] rows)
        {
            Height = rows.Length;
            Width = rows.Length > 0 ? rows[0].Length : 0;
            grid = new char[Width, Height];
            zone = new int[Width, Height];

            for (int y = 0; y < Height; y++)
            {
                if (rows[y].Length != Width)
                {
                    errors.Add($"linha {y} tem {rows[y].Length} colunas (esperado {Width})");
                }

                for (int x = 0; x < Width; x++)
                {
                    grid[x, y] = x < rows[y].Length ? rows[y][x] : '#';
                    zone[x, y] = -1;
                }
            }

            for (int i = 0; i < ZoneSeeds.Length; i++)
            {
                ZoneSeeds[i] = new Vector2Int(-1, -1);
            }

            FloodZones();
            FindDoors();
            FindWindows();
            FindMarkers();
            FindTraps();
            CheckReachability();
        }

        public static MapData Load() => new MapData(VerrucktMap.Rows);

        // ------------------------------------------------------------ classificação
        public char At(int x, int y) => InBounds(x, y) ? grid[x, y] : '#';
        public char At(Vector2Int t) => At(t.x, t.y);
        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public int ZoneAt(Vector2Int t) => InBounds(t.x, t.y) ? zone[t.x, t.y] : -1;

        public static bool IsFloorChar(char c) =>
            c == '.' || c == '<' || c == '>' || c == '+' || c == '?' || c == '=' || c == '~' || (c >= '0' && c <= '9');

        /// <summary>Objeto sólido em cima do piso (pertence à sala, mas bloqueia).</summary>
        public static bool IsFloorObject(char c) =>
            c == '&' || c == '%' || c == 'L' || c == 'X' || c == '$' || c == 'Z' || VerrucktMap.Perks.ContainsKey(c);

        public static bool IsDoorChar(char c) => (c >= 'a' && c <= 'i') || c == 'p';

        public static bool IsWallChar(char c) =>
            c == '#' || c == 'W' || c == '!' || c == 'P' || VerrucktMap.WallBuys.ContainsKey(c);

        public bool IsRoom(int x, int y)
        {
            char c = At(x, y);
            return IsFloorChar(c) || IsFloorObject(c);
        }

        /// <summary>Piso que zumbis e jogador podem pisar (portas não contam).</summary>
        public bool IsWalkable(int x, int y) => IsFloorChar(At(x, y));

        public bool IsOutside(int x, int y) => At(x, y) == 'o';

        // ------------------------------------------------------------ coordenadas
        /// <summary>Centro do tile em coordenadas de mundo (linha 0 no topo).</summary>
        public Vector2 TileToWorld(Vector2Int t) => new Vector2(t.x + 0.5f, Height - 1 - t.y + 0.5f);

        public Vector2Int WorldToTile(Vector2 p) => new Vector2Int(Mathf.FloorToInt(p.x), Height - 1 - Mathf.FloorToInt(p.y));

        /// <summary>Célula do Tilemap correspondente ao tile.</summary>
        public Vector3Int TileToCell(Vector2Int t) => new Vector3Int(t.x, Height - 1 - t.y, 0);

        /// <summary>Direção de grade (y para baixo) para direção de mundo (y para cima).</summary>
        public static Vector2 GridDirToWorld(Vector2Int d) => new Vector2(d.x, -d.y);

        // ------------------------------------------------------------ análise
        private void FloodZones()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    char c = grid[x, y];
                    if (c >= '0' && c <= '9')
                    {
                        int z = c - '0';
                        if (ZoneSeeds[z].x >= 0)
                        {
                            errors.Add($"zona {z} tem duas sementes");
                        }

                        ZoneSeeds[z] = new Vector2Int(x, y);
                    }
                    else if (c == '<')
                    {
                        SpawnWest = new Vector2Int(x, y);
                    }
                    else if (c == '>')
                    {
                        SpawnEast = new Vector2Int(x, y);
                    }
                }
            }

            for (int z = 0; z < ZoneSeeds.Length; z++)
            {
                Vector2Int s = ZoneSeeds[z];
                if (s.x < 0)
                {
                    errors.Add($"zona {z} ({VerrucktMap.ZoneNames[z]}) não tem semente");
                    continue;
                }

                Queue<Vector2Int> q = new Queue<Vector2Int>();
                q.Enqueue(s);
                zone[s.x, s.y] = z;
                while (q.Count > 0)
                {
                    Vector2Int t = q.Dequeue();
                    foreach (Vector2Int d in Dir4)
                    {
                        Vector2Int n = t + d;
                        if (!InBounds(n.x, n.y) || !IsRoom(n.x, n.y))
                        {
                            continue;
                        }

                        if (zone[n.x, n.y] == -1)
                        {
                            zone[n.x, n.y] = z;
                            q.Enqueue(n);
                        }
                        else if (zone[n.x, n.y] != z)
                        {
                            errors.Add($"zonas {z} e {zone[n.x, n.y]} se encostam sem porta em {n}");
                        }
                    }
                }
            }

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (IsRoom(x, y) && zone[x, y] < 0)
                    {
                        errors.Add($"piso sem zona em ({x},{y})");
                    }
                }
            }

            if (At(SpawnWest) != '<') errors.Add("falta o spawn oeste '<'");
            if (At(SpawnEast) != '>') errors.Add("falta o spawn leste '>'");
        }

        private void FindDoors()
        {
            bool[,] seen = new bool[Width, Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    char c = grid[x, y];
                    if (!IsDoorChar(c) || seen[x, y])
                    {
                        continue;
                    }

                    Door door = new Door { id = c, cost = -1 };
                    foreach (VerrucktMap.DoorDef def in VerrucktMap.Doors)
                    {
                        if (def.id == c) door.cost = def.cost;
                    }

                    if (door.cost < 0)
                    {
                        errors.Add($"porta '{c}' sem preço em VerrucktMap.Doors");
                        door.cost = 1000;
                    }

                    HashSet<int> zones = new HashSet<int>();
                    Queue<Vector2Int> q = new Queue<Vector2Int>();
                    q.Enqueue(new Vector2Int(x, y));
                    seen[x, y] = true;
                    while (q.Count > 0)
                    {
                        Vector2Int t = q.Dequeue();
                        door.tiles.Add(t);
                        foreach (Vector2Int d in Dir4)
                        {
                            Vector2Int n = t + d;
                            if (!InBounds(n.x, n.y)) continue;
                            if (grid[n.x, n.y] == c && !seen[n.x, n.y])
                            {
                                seen[n.x, n.y] = true;
                                q.Enqueue(n);
                            }
                            else if (zone[n.x, n.y] >= 0)
                            {
                                zones.Add(zone[n.x, n.y]);
                            }
                        }
                    }

                    if (zones.Count != 2)
                    {
                        errors.Add($"porta '{c}' encosta em {zones.Count} zona(s), precisa de 2");
                    }

                    int i = 0;
                    foreach (int z in zones)
                    {
                        if (i == 0) door.zoneA = z;
                        else if (i == 1) door.zoneB = z;
                        i++;
                    }

                    Doors.Add(door);
                }
            }
        }

        private void FindWindows()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (grid[x, y] != 'W')
                    {
                        continue;
                    }

                    Window w = new Window { tile = new Vector2Int(x, y), zone = -1 };
                    foreach (Vector2Int d in Dir4)
                    {
                        Vector2Int inn = w.tile + d;
                        Vector2Int outT = w.tile - d;
                        if (IsWalkable(inn.x, inn.y) && IsOutside(outT.x, outT.y))
                        {
                            w.inside = inn;
                            w.outward = -d;
                            w.zone = zone[inn.x, inn.y];
                            w.spawn = w.tile - d * 3;
                            if (!IsOutside(w.spawn.x, w.spawn.y))
                            {
                                w.spawn = w.tile - d * 2;
                            }

                            break;
                        }
                    }

                    if (w.zone < 0)
                    {
                        errors.Add($"janela em ({x},{y}) precisa de piso de um lado e pátio do outro");
                        continue;
                    }

                    if (!IsOutside(w.spawn.x, w.spawn.y))
                    {
                        errors.Add($"janela em ({x},{y}) sem espaço de pátio para os zumbis nascerem");
                        continue;
                    }

                    Windows.Add(w);
                }
            }
        }

        private void FindMarkers()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    char c = grid[x, y];
                    bool wallThing = c == '!' || c == 'P' || VerrucktMap.WallBuys.ContainsKey(c);
                    bool floorThing = c == '+' || c == '?' || IsFloorObject(c);
                    if (!wallThing && !floorThing)
                    {
                        continue;
                    }

                    Marker m = new Marker { c = c, tile = new Vector2Int(x, y), zone = -1 };
                    if (floorThing)
                    {
                        m.zone = zone[x, y];
                        m.access = m.tile;
                        if (IsFloorObject(c))
                        {
                            m.access = FindAccess(m.tile);
                        }
                    }
                    else
                    {
                        m.access = FindAccess(m.tile);
                        m.zone = m.access.x >= 0 ? zone[m.access.x, m.access.y] : -1;
                    }

                    if (c != '&' && c != '%' && (m.zone < 0 || m.access.x < 0))
                    {
                        errors.Add($"marcador '{c}' em ({x},{y}) sem piso livre ao lado para interagir");
                        continue;
                    }

                    Markers.Add(m);
                }
            }
        }

        private Vector2Int FindAccess(Vector2Int t)
        {
            foreach (Vector2Int d in Dir4)
            {
                Vector2Int n = t + d;
                if (IsWalkable(n.x, n.y))
                {
                    return n;
                }
            }

            return new Vector2Int(-1, -1);
        }

        private void FindTraps()
        {
            bool[,] seen = new bool[Width, Height];
            List<Marker> levers = Markers.FindAll(m => m.c == '!');
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (grid[x, y] != '~' || seen[x, y])
                    {
                        continue;
                    }

                    Trap trap = new Trap { zone = zone[x, y] };
                    Queue<Vector2Int> q = new Queue<Vector2Int>();
                    q.Enqueue(new Vector2Int(x, y));
                    seen[x, y] = true;
                    while (q.Count > 0)
                    {
                        Vector2Int t = q.Dequeue();
                        trap.tiles.Add(t);
                        foreach (Vector2Int d in Dir4)
                        {
                            Vector2Int n = t + d;
                            if (InBounds(n.x, n.y) && grid[n.x, n.y] == '~' && !seen[n.x, n.y])
                            {
                                seen[n.x, n.y] = true;
                                q.Enqueue(n);
                            }
                        }
                    }

                    // alavanca mais próxima da faixa
                    Marker best = null;
                    int bestD = int.MaxValue;
                    foreach (Marker l in levers)
                    {
                        foreach (Vector2Int t in trap.tiles)
                        {
                            int d = Mathf.Abs(l.tile.x - t.x) + Mathf.Abs(l.tile.y - t.y);
                            if (d < bestD)
                            {
                                bestD = d;
                                best = l;
                            }
                        }
                    }

                    if (best == null || bestD > 3)
                    {
                        errors.Add($"armadilha em ({x},{y}) sem alavanca '!' a até 3 tiles");
                        continue;
                    }

                    levers.Remove(best);
                    trap.lever = best.tile;
                    trap.leverAccess = best.access;
                    Traps.Add(trap);
                }
            }

            if (levers.Count > 0)
            {
                errors.Add($"{levers.Count} alavanca(s) '!' sem armadilha");
            }
        }

        /// <summary>
        /// Garante que nenhuma zona vire beco sem saída: todas são alcançáveis
        /// pelas portas a partir de qualquer spawn, e dentro de cada zona todo
        /// piso livre é alcançável (camas e mesas não podem fechar caminho).
        /// </summary>
        private void CheckReachability()
        {
            for (int z = 0; z < 10; z++)
            {
                Vector2Int s = ZoneSeeds[z];
                if (s.x < 0) continue;

                bool[,] seen = new bool[Width, Height];
                Queue<Vector2Int> q = new Queue<Vector2Int>();
                q.Enqueue(s);
                seen[s.x, s.y] = true;
                while (q.Count > 0)
                {
                    Vector2Int t = q.Dequeue();
                    foreach (Vector2Int d in Dir4)
                    {
                        Vector2Int n = t + d;
                        if (InBounds(n.x, n.y) && !seen[n.x, n.y] && IsWalkable(n.x, n.y) && zone[n.x, n.y] == z)
                        {
                            seen[n.x, n.y] = true;
                            q.Enqueue(n);
                        }
                    }
                }

                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        if (zone[x, y] == z && IsWalkable(x, y) && !seen[x, y])
                        {
                            errors.Add($"piso isolado em ({x},{y}) na zona {z}");
                        }
                    }
                }
            }

            foreach (int start in new[] { ZoneAt(SpawnWest), ZoneAt(SpawnEast) })
            {
                if (start < 0) continue;
                HashSet<int> reach = new HashSet<int> { start };
                bool grew = true;
                while (grew)
                {
                    grew = false;
                    foreach (Door d in Doors)
                    {
                        if (reach.Contains(d.zoneA) && d.zoneB >= 0 && reach.Add(d.zoneB)) grew = true;
                        if (reach.Contains(d.zoneB) && d.zoneA >= 0 && reach.Add(d.zoneA)) grew = true;
                    }
                }

                if (reach.Count != 10)
                {
                    errors.Add($"a partir da zona {start} só {reach.Count} de 10 zonas são alcançáveis");
                }
            }
        }

        public bool Validate(out string report)
        {
            StringBuilder sb = new StringBuilder();
            if (errors.Count == 0)
            {
                sb.Append($"mapa ok: {Width}x{Height}, {Doors.Count} portas, {Windows.Count} janelas, " +
                          $"{Markers.Count} marcadores, {Traps.Count} armadilhas");
            }
            else
            {
                foreach (string e in errors)
                {
                    sb.AppendLine("- " + e);
                }
            }

            report = sb.ToString();
            return errors.Count == 0;
        }
    }
}
