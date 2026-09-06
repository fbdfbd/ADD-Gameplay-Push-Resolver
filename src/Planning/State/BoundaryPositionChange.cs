using System;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal readonly struct BoundaryPositionChange
    {
        public BoundaryPositionChange(
            int itemIndex,
            Vector2 previousPosition,
            bool hadPlannedPosition,
            int previousPositionVersion)
        {
            ItemIndex = itemIndex;
            PreviousPosition = previousPosition;
            HadPlannedPosition = hadPlannedPosition;
            PreviousPositionVersion = previousPositionVersion;
        }

        public int ItemIndex { get; }
        public Vector2 PreviousPosition { get; }
        public bool HadPlannedPosition { get; }
        public int PreviousPositionVersion { get; }
    }
}
