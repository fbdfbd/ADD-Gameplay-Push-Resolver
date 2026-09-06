using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal static class PushGeometry
    {
        internal static bool CanCrossSourceSide(PushContext state,
            PlannedPushRequest request,
            int pinnedIndex,
            SourceSidePolicy sourceSidePolicy,
            Vector2 sourcePosition,
            BoardItem source,
            Vector2 movingPosition,
            BoardItem moving,
            Vector2 direction)
        {
            if (request.MovingIndex == pinnedIndex)
            {
                return false;
            }

            if (sourceSidePolicy
                    == SourceSidePolicy.AllowRootBoundaryEscape
                && request.AllowBoundaryChainMove)
            {
                return !TryGetMinimumSeparationPosition(state,
                    sourcePosition,
                    source,
                    movingPosition,
                    moving,
                    direction,
                    out _);
            }

            if (request.SourceIndex != pinnedIndex)
                return false;

            if (sourceSidePolicy
                == SourceSidePolicy.AllowGeneratedRootReverse)
            {
                return true;
            }

            if (sourceSidePolicy
                != SourceSidePolicy.AllowRootBoundaryEscape)
            {
                return false;
            }

            if (TryGetMinimumSeparationPosition(state,
                    sourcePosition,
                    source,
                    movingPosition,
                    moving,
                    direction,
                    out _))
            {
                return false;
            }

            return TryGetMinimumSeparationPosition(state,
                       sourcePosition,
                       source,
                       movingPosition,
                       moving,
                       request.Direction,
                       out var radialTarget)
                   && !BoardBoundary.IsInsideBoard(state, radialTarget, moving.HalfSize);
        }

        internal static bool WouldCrossSourceSide(
            Vector2 sourcePosition,
            BoardItem source,
            Vector2 movingPosition,
            BoardItem moving,
            Vector2 target,
            Vector2 direction)
        {
            var currentDelta = movingPosition - sourcePosition;
            var targetDelta = target - sourcePosition;
            if (ScalarMath.Abs(direction.X) >= ScalarMath.Abs(direction.Y))
            {
                var tolerance = GetCenterAmbiguityTolerance(
                    source.HalfSize.X,
                    moving.HalfSize.X);
                return ScalarMath.Abs(currentDelta.X) > tolerance
                       && currentDelta.X * targetDelta.X < 0f;
            }

            var verticalTolerance = GetCenterAmbiguityTolerance(
                source.HalfSize.Y,
                moving.HalfSize.Y);
            return ScalarMath.Abs(currentDelta.Y) > verticalTolerance
                   && currentDelta.Y * targetDelta.Y < 0f;
        }

        internal static bool IsWithinLocalMoveLimit(PushContext state,
            Vector2 originalPosition,
            Vector2 target,
            BoardItem source,
            BoardItem moving,
            bool allowSideChange)
        {
            var margin = GetSeparationMargin(state, source, moving);
            var sideChangeMultiplier = allowSideChange ? 2f : 1f;
            const float epsilon = 0.002f;
            var maxDelta = new Vector2(
                (source.HalfSize.X + moving.HalfSize.X + margin.X)
                * sideChangeMultiplier + epsilon,
                (source.HalfSize.Y + moving.HalfSize.Y + margin.Y)
                * sideChangeMultiplier + epsilon);
            var delta = target - originalPosition;
            return ScalarMath.Abs(delta.X) <= maxDelta.X
                   && ScalarMath.Abs(delta.Y) <= maxDelta.Y;
        }

        internal static Vector2 GetAdjacentSeparationPosition(PushContext state,
            Vector2 sourcePosition,
            BoardItem source,
            Vector2 movingPosition,
            BoardItem moving,
            Vector2 direction)
        {
            var margin = GetSeparationMargin(state, source, moving);
            if (ScalarMath.Abs(direction.X) >= ScalarMath.Abs(direction.Y))
            {
                var sign = direction.X >= 0f ? 1f : -1f;
                return new Vector2(
                    sourcePosition.X
                    + sign
                    * (source.HalfSize.X
                       + moving.HalfSize.X
                       + margin.X
                       + BoardPushSolverSettings.SeparationEpsilon),
                    movingPosition.Y);
            }

            var verticalSign = direction.Y >= 0f ? 1f : -1f;
            return new Vector2(
                movingPosition.X,
                sourcePosition.Y
                + verticalSign
                * (source.HalfSize.Y
                   + moving.HalfSize.Y
                   + margin.Y
                   + BoardPushSolverSettings.SeparationEpsilon));
        }

        internal static bool TryGetMinimumSeparationPosition(PushContext state,
            Vector2 sourcePosition,
            BoardItem source,
            Vector2 movingPosition,
            BoardItem moving,
            Vector2 direction,
            out Vector2 target)
        {
            var margin = GetSeparationMargin(state, source, moving);
            var delta = movingPosition - sourcePosition;
            if (ScalarMath.Abs(direction.X) >= ScalarMath.Abs(direction.Y))
            {
                var sign = direction.X >= 0f ? 1f : -1f;
                var centerTolerance = GetCenterAmbiguityTolerance(
                    source.HalfSize.X,
                    moving.HalfSize.X);
                if (ScalarMath.Abs(delta.X) > centerTolerance
                    && delta.X * sign < 0f)
                {
                    target = default;
                    return false;
                }

                var overlapDepth = source.HalfSize.X
                                   + moving.HalfSize.X
                                   + margin.X
                                   - ScalarMath.Abs(delta.X);
                if (overlapDepth <= 0f)
                {
                    target = movingPosition;
                    return true;
                }

                target = movingPosition
                         + Vector2.UnitX
                         * (overlapDepth + BoardPushSolverSettings.SeparationEpsilon)
                         * sign;
                return true;
            }

            var verticalSign = direction.Y >= 0f ? 1f : -1f;
            var verticalCenterTolerance = GetCenterAmbiguityTolerance(
                source.HalfSize.Y,
                moving.HalfSize.Y);
            if (ScalarMath.Abs(delta.Y) > verticalCenterTolerance
                && delta.Y * verticalSign < 0f)
            {
                target = default;
                return false;
            }

            var verticalOverlapDepth = source.HalfSize.Y
                                       + moving.HalfSize.Y
                                       + margin.Y
                                       - ScalarMath.Abs(delta.Y);
            if (verticalOverlapDepth <= 0f)
            {
                target = movingPosition;
                return true;
            }

            target = movingPosition
                     + Vector2.UnitY
                     * (verticalOverlapDepth + BoardPushSolverSettings.SeparationEpsilon)
                     * verticalSign;
            return true;
        }

        internal static float GetCenterAmbiguityTolerance(
            float sourceHalfExtent,
            float movingHalfExtent)
        {
            const float minimumTolerance = 0.001f;
            return ScalarMath.Max(
                minimumTolerance,
                ScalarMath.Min(sourceHalfExtent, movingHalfExtent)
                * BoardPushSolverSettings.CenterAmbiguityRatio);
        }

        internal static bool IsOverlappingAtPositions(PushContext state,
            BoardItem a,
            Vector2 positionA,
            BoardItem b,
            Vector2 positionB)
        {
            var margin = GetSeparationMargin(state, a, b);
            var overlapX = a.HalfSize.X + b.HalfSize.X + margin.X
                           - ScalarMath.Abs(positionA.X - positionB.X);
            var overlapY = a.HalfSize.Y + b.HalfSize.Y + margin.Y
                           - ScalarMath.Abs(positionA.Y - positionB.Y);
            return HasMeaningfulOverlap(overlapX, overlapY);
        }

        internal static bool IsPushConnected(PushContext state,
            BoardItem a,
            BoardItem b,
            float tolerance)
        {
            var margin = GetSeparationMargin(state, a, b);
            return ScalarMath.Abs(a.Position.X - b.Position.X)
                       <= a.HalfSize.X + b.HalfSize.X + margin.X + tolerance
                   && ScalarMath.Abs(a.Position.Y - b.Position.Y)
                       <= a.HalfSize.Y + b.HalfSize.Y + margin.Y + tolerance;
        }

        internal static Vector2 GetSeparationMargin(PushContext state, BoardItem a, BoardItem b)
        {
            var ratio = ScalarMath.Clamp01(state.Settings.AllowedOverlapRatio);
            if (ratio <= 0f)
                return Vector2.One * state.Settings.Padding;

            return new Vector2(
                -ScalarMath.Min(a.HalfSize.X, b.HalfSize.X) * 2f * ratio,
                -ScalarMath.Min(a.HalfSize.Y, b.HalfSize.Y) * 2f * ratio);
        }

        internal static bool HasMeaningfulOverlap(
            float overlapX,
            float overlapY)
        {
            return overlapX > 0f && overlapY > 0f;
        }

        internal static Vector2 GetBroadPhasePadding(PushContext state)
        {
            if (state.Settings.AllowedOverlapRatio > 0f)
                return Vector2.Zero;

            return Vector2.One * ScalarMath.Max(0f, state.Settings.Padding);
        }

        internal static bool IsOverlapping(PushContext state, BoardItem a, BoardItem b)
        {
            var margin = GetSeparationMargin(state, a, b);
            var overlapX = a.HalfSize.X + b.HalfSize.X + margin.X
                           - ScalarMath.Abs(a.Position.X - b.Position.X);
            var overlapY = a.HalfSize.Y + b.HalfSize.Y + margin.Y
                           - ScalarMath.Abs(a.Position.Y - b.Position.Y);
            return HasMeaningfulOverlap(overlapX, overlapY);
        }
    }
}
