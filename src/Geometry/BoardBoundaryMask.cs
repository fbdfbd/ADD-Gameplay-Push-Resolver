using System;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    [Flags]
    internal enum BoardBoundaryMask : byte
    {
        None = 0,
        Left = 1 << 0,
        Right = 1 << 1,
        Bottom = 1 << 2,
        Top = 1 << 3
    }
}
