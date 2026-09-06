using System;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal enum SourceSidePolicy : byte
    {
        Preserve,
        AllowGeneratedRootReverse,
        AllowRootBoundaryEscape
    }
}
