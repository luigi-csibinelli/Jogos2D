using System;
using UnityEngine;

namespace Sanatorio
{
    /// <summary>
    /// Recordes, fichas encontradas e opções, em PlayerPrefs (IndexedDB no WebGL).
    /// Toda leitura/escrita é protegida: navegador em modo privado não quebra o jogo.
    /// </summary>
    public static class SaveData
    {
        public static int BestRound(Difficulty d) => GetInt("best_round_" + (int)d, 0);
        public static int BestKills(Difficulty d) => GetInt("best_kills_" + (int)d, 0);
        public static int BestPoints(Difficulty d) => GetInt("best_points_" + (int)d, 0);
        public static int GamesPlayed => GetInt("games", 0);

        public static float MusicVolume { get => GetFloat("vol_music", 0.7f); set => SetFloat("vol_music", value); }
        public static float SfxVolume { get => GetFloat("vol_sfx", 0.85f); set => SetFloat("vol_sfx", value); }
        public static bool Scanlines { get => GetInt("scanlines", 1) == 1; set => SetInt("scanlines", value ? 1 : 0); }
        public static bool ScreenShake { get => GetInt("shake", 1) == 1; set => SetInt("shake", value ? 1 : 0); }
        public static bool AimAssist { get => GetInt("aim_assist", 1) == 1; set => SetInt("aim_assist", value ? 1 : 0); }
        public static bool SeenTutorial { get => GetInt("tutorial", 0) == 1; set => SetInt("tutorial", value ? 1 : 0); }

        public static bool NoteFound(int index) => (GetInt("notes", 0) & (1 << index)) != 0;

        public static int NotesFoundCount
        {
            get
            {
                int mask = GetInt("notes", 0), n = 0;
                for (int i = 0; i < 16; i++)
                {
                    if ((mask & (1 << i)) != 0) n++;
                }

                return n;
            }
        }

        public static void MarkNote(int index)
        {
            SetInt("notes", GetInt("notes", 0) | (1 << index));
            Save();
        }

        /// <summary>Grava a partida. Devolve true se bateu o recorde de rodada.</summary>
        public static bool RecordGame(Difficulty d, int round, int kills, int points)
        {
            bool record = round > BestRound(d);
            if (record) SetInt("best_round_" + (int)d, round);
            if (kills > BestKills(d)) SetInt("best_kills_" + (int)d, kills);
            if (points > BestPoints(d)) SetInt("best_points_" + (int)d, points);
            SetInt("games", GamesPlayed + 1);
            Save();
            return record;
        }

        public static void Save()
        {
            try { PlayerPrefs.Save(); }
            catch (Exception e) { Debug.LogWarning("[SaveData] " + e.Message); }
        }

        private static int GetInt(string k, int def)
        {
            try { return PlayerPrefs.GetInt(k, def); }
            catch (Exception) { return def; }
        }

        private static float GetFloat(string k, float def)
        {
            try { return PlayerPrefs.GetFloat(k, def); }
            catch (Exception) { return def; }
        }

        private static void SetInt(string k, int v)
        {
            try { PlayerPrefs.SetInt(k, v); }
            catch (Exception e) { Debug.LogWarning("[SaveData] " + e.Message); }
        }

        private static void SetFloat(string k, float v)
        {
            try { PlayerPrefs.SetFloat(k, v); }
            catch (Exception e) { Debug.LogWarning("[SaveData] " + e.Message); }
        }
    }
}
