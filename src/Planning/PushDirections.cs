using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal static class PushDirections
    {
        internal static Vector2 ResolveOutwardPushDirection(
            Vector2 sourcePosition,
            Vector2 movingPosition,
            Vector2 fallbackDirection)
        {
            var delta = movingPosition - sourcePosition;
            if (delta.LengthSquared() <= 0.000001f)
                return GetDirection(GetDirectionIndex(fallbackDirection));

            return ScalarMath.Abs(delta.X) >= ScalarMath.Abs(delta.Y)
                ? delta.X >= 0f ? Vector2.UnitX : (-Vector2.UnitX)
                : delta.Y >= 0f ? Vector2.UnitY : (-Vector2.UnitY);
        }

        internal static Vector2 ResolveInitialPushDirection(PushContext state,
            int pinnedIndex,
            int movingIndex,
            Vector2 requestedDirection,
            RootDirectionMode mode)
        {
            if (mode == RootDirectionMode.ForcedRecovery)
                return GetDirection(GetDirectionIndex(requestedDirection));

            return ResolveRootPushDirection(state,
                pinnedIndex,
                movingIndex,
                requestedDirection);
        }

        internal static Vector2 ResolveRootPushDirection(PushContext state,
            int pinnedIndex,
            int movingIndex,
            Vector2 fallbackDirection)
        {
            var pinned = state.Items[pinnedIndex];
            var moving = state.Items[movingIndex];
            var delta = moving.Position - pinned.Position;
            var toleranceX = PushGeometry.GetCenterAmbiguityTolerance(
                pinned.HalfSize.X,
                moving.HalfSize.X);
            var toleranceY = PushGeometry.GetCenterAmbiguityTolerance(
                pinned.HalfSize.Y,
                moving.HalfSize.Y);
            if (ScalarMath.Abs(delta.X) <= toleranceX
                && ScalarMath.Abs(delta.Y) <= toleranceY)
            {
                var startIndex = GetDirectionIndex(fallbackDirection);
                var bestIndex = startIndex;
                for (var offset = 1; offset < state.PushDirections.Length; offset++)
                {
                    var directionIndex = (startIndex + offset) % state.PushDirections.Length;
                    if (state.RootPushDirectionCounts[directionIndex]
                        < state.RootPushDirectionCounts[bestIndex])
                    {
                        bestIndex = directionIndex;
                    }
                }

                state.RootPushDirectionCounts[bestIndex]++;
                return GetDirection(bestIndex);
            }

            var horizontalIndex = delta.X >= 0f ? 0 : 1;
            var verticalIndex = delta.Y >= 0f ? 2 : 3;
            var absX = ScalarMath.Abs(delta.X);
            var absY = ScalarMath.Abs(delta.Y);
            int selectedIndex;

            if (absX > absY * 1.1f)
            {
                selectedIndex = horizontalIndex;
            }
            else if (absY > absX * 1.1f)
            {
                selectedIndex = verticalIndex;
            }
            else
            {
                var horizontalCount = state.RootPushDirectionCounts[horizontalIndex];
                var verticalCount = state.RootPushDirectionCounts[verticalIndex];
                if (horizontalCount != verticalCount)
                {
                    selectedIndex = horizontalCount < verticalCount
                        ? horizontalIndex
                        : verticalIndex;
                }
                else
                {
                    selectedIndex = (delta.X >= 0f) == (delta.Y >= 0f)
                        ? horizontalIndex
                        : verticalIndex;
                }
            }

            state.RootPushDirectionCounts[selectedIndex]++;
            return GetDirection(selectedIndex);
        }

        internal static int GetDirectionIndex(Vector2 direction)
        {
            if (ScalarMath.Abs(direction.X) >= ScalarMath.Abs(direction.Y))
                return direction.X >= 0f ? 0 : 1;

            return direction.Y >= 0f ? 2 : 3;
        }

        internal static Vector2 GetDirection(int directionIndex)
        {
            switch (directionIndex)
            {
                case 0: return Vector2.UnitX;
                case 1: return (-Vector2.UnitX);
                case 2: return Vector2.UnitY;
                default: return (-Vector2.UnitY);
            }
        }

        internal static void FillPushDirections(PushContext state, Vector2 forward, int stableSortId)
        {
            var horizontal = ScalarMath.Abs(forward.X) >= ScalarMath.Abs(forward.Y);
            var normalizedForward = horizontal
                ? forward.X >= 0f ? Vector2.UnitX : (-Vector2.UnitX)
                : forward.Y >= 0f ? Vector2.UnitY : (-Vector2.UnitY);
            var positiveSide = horizontal ? Vector2.UnitY : Vector2.UnitX;
            if ((stableSortId & 1) != 0)
                positiveSide = -positiveSide;

            state.PushDirections[0] = normalizedForward;
            state.PushDirections[1] = positiveSide;
            state.PushDirections[2] = -positiveSide;
            state.PushDirections[3] = -normalizedForward;
        }
    }
}
