namespace IHI.Server.Plugins.Cecer1.IHIPathfinder
{
    internal class Values
    {
        internal readonly ushort[] BinaryHeap;
        internal readonly ushort[] F;
        internal readonly ushort[] G;
        internal readonly ushort[] H;

        internal readonly float MaxDrop;
        internal readonly float MaxJump;
        internal readonly ushort[] Parent;
        internal readonly byte[,] Tiles;
        internal readonly byte[] X;
        internal readonly byte[] Y;
        internal readonly float[,] Z;
        internal ushort Count;
        internal ushort LastID;

        internal ushort Location;

        internal Values(byte[,] collisionMap, float[,] height, float maxDrop, float maxJump)
        {
            Tiles = new byte[collisionMap.GetLength(0),collisionMap.GetLength(1)];

            X = new byte[Tiles.Length];
            Y = new byte[X.Length];
            Z = height;
            H = new ushort[X.Length];
            G = new ushort[X.Length];
            F = new ushort[X.Length];

            Count = 0;
            LastID = 0;

            BinaryHeap = new ushort[X.Length];
            Parent = new ushort[X.Length];

            Location = 0;

            MaxDrop = maxDrop;
            MaxJump = maxJump;
        }
    }
}