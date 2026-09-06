using System;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal readonly struct CornerPackingState :
        System.IEquatable<CornerPackingState>
    {
        public CornerPackingState(
            int sourceIndex,
            int movingIndex,
            int directionIndex,
            Vector2 sourcePosition,
            Vector2 movingPosition)
        {
            SourceIndex = sourceIndex;
            MovingIndex = movingIndex;
            DirectionIndex = directionIndex;
            SourceX = Quantize(sourcePosition.X);
            SourceY = Quantize(sourcePosition.Y);
            MovingX = Quantize(movingPosition.X);
            MovingY = Quantize(movingPosition.Y);
        }

        private int SourceIndex { get; }
        private int MovingIndex { get; }
        private int DirectionIndex { get; }
        private int SourceX { get; }
        private int SourceY { get; }
        private int MovingX { get; }
        private int MovingY { get; }

        public bool Equals(CornerPackingState other)
        {
            return SourceIndex == other.SourceIndex
                   && MovingIndex == other.MovingIndex
                   && DirectionIndex == other.DirectionIndex
                   && SourceX == other.SourceX
                   && SourceY == other.SourceY
                   && MovingX == other.MovingX
                   && MovingY == other.MovingY;
        }

        public override bool Equals(object obj)
        {
            return obj is CornerPackingState other
                   && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = SourceIndex;
                hash = hash * 397 ^ MovingIndex;
                hash = hash * 397 ^ DirectionIndex;
                hash = hash * 397 ^ SourceX;
                hash = hash * 397 ^ SourceY;
                hash = hash * 397 ^ MovingX;
                return hash * 397 ^ MovingY;
            }
        }

        private static int Quantize(float value)
        {
            return ScalarMath.RoundToInt(value / BoardPushSolverSettings.SeparationEpsilon);
        }
    }
}
