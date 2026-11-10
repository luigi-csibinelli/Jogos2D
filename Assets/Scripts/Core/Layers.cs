using UnityEngine;

namespace Sanatorio
{
    /// <summary>Camadas de física. A matriz de colisão é aplicada em código (Apply).</summary>
    public static class Layers
    {
        public const int Wall = 8;          // paredes, portas fechadas, móveis
        public const int Zombie = 9;
        public const int Player = 10;
        public const int PlayerBlock = 11;  // janelas: só o jogador colide
        public const int Pickup = 12;       // power-ups (gatilho só do jogador)

        public static int WallMask => 1 << Wall;
        public static int ZombieMask => 1 << Zombie;
        public static int ShotMask => (1 << Wall) | (1 << Zombie);

        public static void Apply()
        {
            for (int a = 0; a < 32; a++)
            {
                Physics2D.IgnoreLayerCollision(PlayerBlock, a, a != Player);
                Physics2D.IgnoreLayerCollision(Pickup, a, a != Player);
            }

            Physics2D.IgnoreLayerCollision(Zombie, Zombie, false);
            Physics2D.IgnoreLayerCollision(Zombie, Player, false);
            Physics2D.IgnoreLayerCollision(Zombie, Wall, false);
            Physics2D.IgnoreLayerCollision(Player, Wall, false);
            Physics2D.gravity = Vector2.zero;
        }
    }

    public static class Order
    {
        public const int Outside = 0;
        public const int Floor = 1;
        public const int Decal = 2;
        public const int FloorObject = 3;
        public const int Item = 4;
        public const int Zombie = 6;
        public const int Player = 7;
        public const int Projectile = 8;
        public const int Wall = 9;
        public const int WallDecor = 10;
        public const int Fx = 12;
        public const int WorldText = 20;
    }
}
