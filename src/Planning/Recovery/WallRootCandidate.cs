using System;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal readonly struct WallRootCandidate
    {
        public WallRootCandidate(
            Vector2 direction,
            Vector2 target,
            int directionPriority,
            float moveDistanceSqr)
        {
            Direction = direction;
            Target = target;
            DirectionPriority = directionPriority;
            MoveDistanceSqr = moveDistanceSqr;
            IsValid = true;
        }

        public Vector2 Direction { get; }
        public Vector2 Target { get; }
        public int DirectionPriority { get; }
        public float MoveDistanceSqr { get; }
        public bool IsValid { get; }

        public bool IsBetterThan(WallRootCandidate other)
        {
            if (!IsValid)
                return false;
            if (!other.IsValid)
                return true;

            if (DirectionPriority != other.DirectionPriority)
                return DirectionPriority < other.DirectionPriority;

            return MoveDistanceSqr < other.MoveDistanceSqr;
        }
    }
}
