using System;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal readonly struct CornerEscapeContext
    {
        public CornerEscapeContext(
            BoardBoundaryMask boundaries,
            Vector2 horizontalOutlet,
            Vector2 verticalOutlet,
            bool preferHorizontal)
        {
            Boundaries = boundaries;
            HorizontalOutlet = horizontalOutlet;
            VerticalOutlet = verticalOutlet;
            PreferHorizontal = preferHorizontal;
        }

        public BoardBoundaryMask Boundaries { get; }
        public Vector2 HorizontalOutlet { get; }
        public Vector2 VerticalOutlet { get; }
        public bool PreferHorizontal { get; }

        public Vector2 ResolveOutlet(
            Vector2 direction,
            bool preferHorizontal)
        {
            var directionIndex = PushDirections.GetDirectionIndex(direction);
            if (directionIndex
                == PushDirections.GetDirectionIndex(HorizontalOutlet))
            {
                return HorizontalOutlet;
            }

            if (directionIndex
                == PushDirections.GetDirectionIndex(VerticalOutlet))
            {
                return VerticalOutlet;
            }

            if (ScalarMath.Abs(direction.X) >= ScalarMath.Abs(direction.Y))
                return VerticalOutlet;
            if (ScalarMath.Abs(direction.Y) > ScalarMath.Abs(direction.X))
                return HorizontalOutlet;

            return preferHorizontal
                ? HorizontalOutlet
                : VerticalOutlet;
        }

        public Vector2 GetCandidateDirection(
            Vector2 requestedDirection,
            bool preferHorizontal,
            int candidateIndex)
        {
            var first = ResolveOutlet(
                requestedDirection,
                preferHorizontal);
            if (candidateIndex == 0)
                return first;

            return PushDirections.GetDirectionIndex(first)
                   == PushDirections.GetDirectionIndex(HorizontalOutlet)
                ? VerticalOutlet
                : HorizontalOutlet;
        }
    }
}
