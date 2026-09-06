using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    public readonly struct Aabb2
    {
        public Vector2 Min { get; }
        public Vector2 Max { get; }

        public Aabb2(Vector2 min, Vector2 max)
        {
            if (!float.IsFinite(min.X) || !float.IsFinite(min.Y)
                || !float.IsFinite(max.X) || !float.IsFinite(max.Y)
                || min.X >= max.X || min.Y >= max.Y)
                throw new ArgumentException("Bounds must have finite, ordered corners.");
            Min = min;
            Max = max;
        }

        internal bool TryGetBounds(out Vector2 min, out Vector2 max)
        {
            min = Min;
            max = Max;
            return Min.X < Max.X && Min.Y < Max.Y;
        }
    }
}
