using System.Collections.Generic;

namespace Sanatorio
{
    /// <summary>
    /// Planta do sanatório, inspirada em Verrückt (World at War). Um tile = 1 unidade.
    /// A linha 0 é o topo do mapa. Editar estas strings muda o jogo inteiro;
    /// MapData.Validate() acusa qualquer erro na hora de montar.
    ///
    ///  Geometria            Marcadores (sempre sobre o tile indicado)
    ///  '#' parede           'W' janela com barricada (na parede externa)
    ///  '.' piso             '<' '>' spawn do jogador (oeste / leste)
    ///  'o' pátio externo    '0'-'9' semente de zona (piso)
    ///  '=' escada           '+' lâmpada de teto (piso)
    ///  '~' grade elétrica   '?' ficha de paciente (piso)
    ///  'a'-'i' portas       '&' cama   '%' mesa   'L' privada (piso, sólido)
    ///  'p' porta elétrica   '!' alavanca da armadilha (parede)
    ///                       'P' disjuntor geral (parede)
    ///                       '$' caixa misteriosa inicial  'X' outro ponto da caixa (piso)
    ///                       'Z' Soco em Lata (melhoria de armas, piso, sólido)
    ///  Armas na parede: ver WallBuys.  Refrigerantes: ver Perks.
    /// </summary>
    public static class VerrucktMap
    {
        public static readonly string[] Rows =
        {
            "oooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooo",
            "oooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooo",
            "oooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooo",
            "ooo#########W#######S#############P####################W##############O#######W##ooooooooooo",
            "ooo#.L.L.L................?.###.......$......###..............?###..........C...#ooooooooooo",
            "ooo#............&..&........###..............###...............###..............#ooooooooooo",
            "ooo#........................###..............###....+......+...###..............#ooooooooooo",
            "ooo#........................###......+.......iii...............###......+.......Nooooooooooo",
            "oooW..................+.....ccc.........3....iii...%%.....%%...hhh..............#ooooooooooo",
            "ooo#......+....2............ccc..............iii.......9.......hhh......8.......#ooooooooooo",
            "ooo#........................ccc..............###...............hhh..............#ooooooooooo",
            "ooo#........................###.....===......###...............###..............#ooooooooooo",
            "ooo#.......................X###.....===.....?###...............###?.............#ooooooooooo",
            "ooo#........................#########W#####################################ggg###ooooooooooo",
            "ooo###bbb###########T########oooooooooooooooooooooooooooooooooooooooooooo##ggg###ooooooooooo",
            "ooo###bbb######oooooooooooooooooooooooooooooooooooooooooooooooooooooooooo##ggg###ooooooooooo",
            "ooo###bbb######oooooooooooooooooooooooooooooooooooooooooooooooooooooooooo##ggg#######ooooooo",
            "ooo#..........#oooooooooooooooooooooooooooooooooooooooooooooooooooooooooo#..........#ooooooo",
            "ooo#..........#oooooooooooooooooooooooooooooooooooooooooooooooooooooooooo#..........#ooooooo",
            "oooM..........!oooooooooooooooooooooooooooooooooooooooooooooooooooooooooo#....+.....#ooooooo",
            "ooo#~~~~~~~~~~#ooooooooooooooooooooooooooooooooooooooooooooooooooooooooooW..........Tooooooo",
            "ooo#..........#oooooooooooooooooooooooooooooooooooooooooooooooooooooooooo#..........#ooooooo",
            "ooo#....+.....Yoooooooooooooooooooooooooooooooooooooooooooooooooooooooooo#~~~~~~~~~~!ooooooo",
            "ooo#.........U#oooooooooooooooooooooooooooooooooooooooooooooooooooooooooo#..........#ooooooo",
            "ooo#..........#ooooooooooooooooooooooooooooooooooooooooooooooooooooooooooA..........Wooooooo",
            "oooW....1.....#oooooooooooooooooooooooooooooooooooooooooooooooooooooooooo#....7.....#ooooooo",
            "ooo#~~~~~~~~~~#oooooooooooooooooooooooooooooooooooooooooooooooooooooooooo#..........#ooooooo",
            "ooo!..........#oooooooooooooooooooooooooooooooooooooooooooooooooooooooooo!~~~~~~~~~~#ooooooo",
            "ooo#..........Woooooooooooooooooooooooooooooooooooooooooooooooooooooooooo#..........Yooooooo",
            "ooo#....+.....#oooooooooooooooooooooooooooooooooooooooooooooooooooooooooo#..........#ooooooo",
            "oooB.........X#ooooooooooooooooooooooooooooooooooooooooooooooooooooooooooW....+.....#ooooooo",
            "ooo#..........#oooooooooooooooooooooooooooooooooooooooooooooooooooooooooo#..........#ooooooo",
            "ooo#.?........#oooooooooooooooooooooooooooooooooooooooooooooooooooooooooo#?.........#ooooooo",
            "ooo######aaa###oooooooooooooooooooooooooooooooooooooooooooooooooooooooooo####fff#####ooooooo",
            "ooooooo##aaa###oooooooooooooooooooooooooooooooooooooooooooooooooooooooooo####fff#####ooooooo",
            "ooooooo##aaa################W#######################W#########W#######W#H####fff####B####ooo",
            "ooooooo#................................####.......................#....................#ooo",
            "ooooooo#................................####.......................e..........+.........Wooo",
            "ooooooo#..........&..&.........&..&.....####....&..&......&..&.....e..........6.........#ooo",
            "oooooooW................................####.......................e....................Nooo",
            "ooooooo#.................+.........+....####......+...............Q#....................#ooo",
            "ooooooo#................................####.......................#...X................Wooo",
            "ooooooo#................................pppp.......................######################ooo",
            "ooooooo#................................pppp.......................######################ooo",
            "ooooooo#.................0..............pppp...........4...........######################ooo",
            "ooooooo#####..##........................####.......................#...................Z#ooo",
            "ooooooo#.......#.........<..............####...........>...........#...&..&...+...&.....#ooo",
            "ooooooo#.......#........................####.......................d....................#ooo",
            "oooooooG...+...#........................####................+......d..........5.........Aooo",
            "ooooooo#.......#..............%.........####.......................d....................#ooo",
            "ooooooo#.......#......................?.####.......................#...&..&.......&.....#ooo",
            "ooooooo#.J..X..#........................####.......................#...................?#ooo",
            "ooooooo#############K####W####N###W#############F######W######R##########################ooo",
            "oooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooo",
            "oooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooo",
            "oooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooo"
        };

        /// <summary>Nome de cada zona, indexado pelo dígito semente.</summary>
        public static readonly string[] ZoneNames =
        {
            "Enfermaria Oeste",       // 0 - spawn oeste
            "Varanda Oeste",          // 1
            "Andar Superior Oeste",   // 2
            "Sala do Gerador",        // 3
            "Enfermaria Leste",       // 4 - spawn leste
            "Necrotério",             // 5
            "Corredor Leste",         // 6
            "Varanda Leste",          // 7
            "Andar Superior Leste",   // 8
            "Cozinha",                // 9
        };

        public struct DoorDef
        {
            public char id;
            public int cost;   // 0 = abre com a energia
            public DoorDef(char id, int cost) { this.id = id; this.cost = cost; }
        }

        /// <summary>Preços das portas, iguais aos de Verrückt.</summary>
        public static readonly DoorDef[] Doors =
        {
            new DoorDef('a', 1000),
            new DoorDef('b', 750),
            new DoorDef('c', 1000),
            new DoorDef('d', 750),
            new DoorDef('e', 750),
            new DoorDef('f', 1000),
            new DoorDef('g', 750),
            new DoorDef('h', 1000),
            new DoorDef('i', 1000),
            new DoorDef('p', 0),
        };

        /// <summary>Marcador de parede -> arma (id do WeaponCatalog).</summary>
        public static readonly Dictionary<char, string> WallBuys = new Dictionary<char, string>
        {
            { 'K', "kar98k" },
            { 'G', "gewehr43" },
            { 'N', "grenades" },
            { 'M', "mp40" },
            { 'B', "doublebarrel" },
            { 'Y', "betty" },
            { 'S', "stg44" },
            { 'T', "trenchgun" },
            { 'F', "springfield" },
            { 'R', "garand" },
            { 'H', "thompson" },
            { 'A', "bar" },
            { 'O', "sawedoff" },
        };

        /// <summary>Marcador de piso -> refrigerante.</summary>
        public static readonly Dictionary<char, PerkType> Perks = new Dictionary<char, PerkType>
        {
            { 'J', PerkType.FerroNog },
            { 'Q', PerkType.ReviveJa },
            { 'C', PerkType.TurboCola },
            { 'U', PerkType.GatilhoDuplo },
        };
    }
}
