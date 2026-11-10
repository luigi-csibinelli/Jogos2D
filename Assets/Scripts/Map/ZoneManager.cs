using System.Collections.Generic;
using UnityEngine;

namespace Sanatorio
{
    /// <summary>
    /// Quais zonas estão abertas. Zumbis só nascem em janelas de zonas abertas,
    /// e a caixa só se muda para pontos em zonas abertas (como em Verrückt).
    /// </summary>
    public class ZoneManager
    {
        private readonly Game game;
        private readonly bool[] unlocked = new bool[10];
        private readonly bool[] visited = new bool[10];

        public ZoneManager(Game game)
        {
            this.game = game;
        }

        public bool IsUnlocked(int zone) => zone >= 0 && zone < unlocked.Length && unlocked[zone];

        public int UnlockedCount
        {
            get
            {
                int n = 0;
                foreach (bool b in unlocked) if (b) n++;
                return n;
            }
        }

        public void Unlock(int zone, bool announce = true)
        {
            if (zone < 0 || zone >= unlocked.Length || unlocked[zone]) return;
            unlocked[zone] = true;
            if (announce)
            {
                GameEvents.RaiseToast(VerrucktMap.ZoneNames[zone]);
            }
        }

        public string SideName(int zone)
        {
            string side = zone == 0 ? "ALA OESTE" : zone == 4 ? "ALA LESTE" : "";
            return side.Length > 0 ? side + " - " + VerrucktMap.ZoneNames[zone] : VerrucktMap.ZoneNames[Mathf.Max(0, zone)];
        }

        /// <summary>Marca a zona como visitada; devolve true na primeira vez.</summary>
        public bool Visit(int zone)
        {
            if (zone < 0 || zone >= visited.Length || visited[zone]) return false;
            visited[zone] = true;
            return true;
        }

        public List<MapData.Window> ActiveWindows()
        {
            List<MapData.Window> list = new List<MapData.Window>();
            foreach (MapData.Window w in game.Map.Windows)
            {
                if (IsUnlocked(w.zone)) list.Add(w);
            }

            return list;
        }
    }
}
