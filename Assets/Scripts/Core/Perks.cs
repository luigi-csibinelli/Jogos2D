namespace Sanatorio
{
    /// <summary>Refrigerantes do sanatório (marcas próprias, mesmas regras de WaW).</summary>
    public enum PerkType
    {
        FerroNog = 0,       // aguenta 5 golpes
        ReviveJa = 1,       // levanta sozinho ao cair (solo)
        TurboCola = 2,      // recarrega e reconstrói 2x mais rápido
        GatilhoDuplo = 3,   // +33% de cadência
    }

    public static class PerkInfo
    {
        public static string Name(PerkType p)
        {
            switch (p)
            {
                case PerkType.FerroNog: return "Ferro-Nog";
                case PerkType.ReviveJa: return "Revive-Já";
                case PerkType.TurboCola: return "Turbo-Cola";
                case PerkType.GatilhoDuplo: return "Gatilho Duplo";
            }

            return p.ToString();
        }

        public static string Effect(PerkType p)
        {
            switch (p)
            {
                case PerkType.FerroNog: return "aguenta mais golpes";
                case PerkType.ReviveJa: return "levanta sozinho ao cair";
                case PerkType.TurboCola: return "recarga mais rápida";
                case PerkType.GatilhoDuplo: return "atira mais rápido";
            }

            return "";
        }

        public static UnityEngine.Color Color(PerkType p)
        {
            switch (p)
            {
                case PerkType.FerroNog: return new UnityEngine.Color32(228, 60, 52, 255);
                case PerkType.ReviveJa: return new UnityEngine.Color32(80, 160, 255, 255);
                case PerkType.TurboCola: return new UnityEngine.Color32(110, 214, 70, 255);
                case PerkType.GatilhoDuplo: return new UnityEngine.Color32(240, 176, 48, 255);
            }

            return UnityEngine.Color.white;
        }

        public static string Sprite(PerkType p) => "Perk" + p;
    }
}
