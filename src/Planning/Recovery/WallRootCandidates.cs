using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal static class WallRootCandidates
    {
        internal static bool CollectWallRootCandidates(PushContext state,
            int pinnedIndex,
            Vector2 preferredDirection,
            bool requireBoundaryBlockedRoot)
        {
            state.WallRootOptions.Clear();
            var pinned = state.Items[pinnedIndex];
            PlanningState.CollectPlannedBlockers(state,
                pinnedIndex,
                pinned.Position,
                pinned);
            if (state.PlannedBlockers.Count == 0)
                return false;

            PlanningState.SortPlannedBlockersNewestFirst(state);
            System.Array.Clear(
                state.RootPushDirectionCounts,
                0,
                state.RootPushDirectionCounts.Length);

            var hasBoundaryBlockedRoot = false;
            for (var i = 0; i < state.PlannedBlockers.Count; i++)
            {
                var movingIndex = state.PlannedBlockers[i];
                var moving = state.Items[movingIndex];
                if (!ItemPolicy.CanBePushed(moving))
                    return false;

                var radialDirection = PushDirections.ResolveRootPushDirection(state,
                    pinnedIndex,
                    movingIndex,
                    preferredDirection);
                if (TryGetWallRootSeparationPosition(state,
                        pinned.Position,
                        pinned,
                        moving.Position,
                        moving,
                        radialDirection,
                        out var radialTarget)
                    && !BoardBoundary.IsInsideBoard(state, radialTarget, moving.HalfSize))
                {
                    hasBoundaryBlockedRoot = true;
                }

                PushDirections.FillPushDirections(state, radialDirection, moving.StableSortId);
                var firstCandidate = default(WallRootCandidate);
                var secondCandidate = default(WallRootCandidate);
                var thirdCandidate = default(WallRootCandidate);
                for (var directionIndex = 0;
                     directionIndex < (requireBoundaryBlockedRoot
                         ? state.PushDirections.Length - 1
                         : state.PushDirections.Length);
                     directionIndex++)
                {
                    var direction = state.PushDirections[directionIndex];
                    if (!TryGetWallRootSeparationPosition(state,
                            pinned.Position,
                            pinned,
                            moving.Position,
                            moving,
                            direction,
                            out var target)
                        || !BoardBoundary.IsInsideBoard(state, target, moving.HalfSize)
                        || PushGeometry.IsOverlappingAtPositions(state,
                            pinned,
                            pinned.Position,
                            moving,
                            target)
                        || !PushGeometry.IsWithinLocalMoveLimit(state,
                            moving.Position,
                            target,
                            pinned,
                            moving,
                            allowSideChange: true))
                    {
                        continue;
                    }

                    var directionPriority =
                        requireBoundaryBlockedRoot
                        && directionIndex != 0
                            ? 1
                            : 0;
                    var candidate = new WallRootCandidate(
                        direction,
                        target,
                        directionPriority,
                        (target - moving.Position).LengthSquared());
                    TrySelectWallRootCandidate(
                        candidate,
                        ref firstCandidate,
                        ref secondCandidate,
                        ref thirdCandidate);
                }

                var candidateCount = firstCandidate.IsValid ? 1 : 0;
                if (secondCandidate.IsValid)
                    candidateCount++;
                if (thirdCandidate.IsValid)
                    candidateCount++;

                if (candidateCount == 0)
                    return false;

                state.WallRootOptions.Add(new WallRootOptions(
                    movingIndex,
                    firstCandidate,
                    secondCandidate,
                    thirdCandidate,
                    candidateCount));
            }

            return !requireBoundaryBlockedRoot
                   || hasBoundaryBlockedRoot;
        }

        internal static bool TryGetWallRootSeparationPosition(PushContext state,
            Vector2 sourcePosition,
            BoardItem source,
            Vector2 movingPosition,
            BoardItem moving,
            Vector2 direction,
            out Vector2 target)
        {
            var margin = PushGeometry.GetSeparationMargin(state, source, moving);
            const float epsilon = 0.001f;
            if (ScalarMath.Abs(direction.X) >= ScalarMath.Abs(direction.Y))
            {
                var sign = direction.X >= 0f ? 1f : -1f;
                var separation = source.HalfSize.X
                                 + moving.HalfSize.X
                                 + margin.X
                                 + epsilon;
                target = new Vector2(
                    sourcePosition.X + separation * sign,
                    movingPosition.Y);
                return true;
            }

            var verticalSign = direction.Y >= 0f ? 1f : -1f;
            var verticalSeparation = source.HalfSize.Y
                                     + moving.HalfSize.Y
                                     + margin.Y
                                     + epsilon;
            target = new Vector2(
                movingPosition.X,
                sourcePosition.Y + verticalSeparation * verticalSign);
            return true;
        }

        internal static void TrySelectWallRootCandidate(
            WallRootCandidate candidate,
            ref WallRootCandidate first,
            ref WallRootCandidate second,
            ref WallRootCandidate third)
        {
            if (!candidate.IsValid
                || IsSameWallRootTarget(candidate, first)
                || IsSameWallRootTarget(candidate, second)
                || IsSameWallRootTarget(candidate, third))
            {
                return;
            }

            if (candidate.IsBetterThan(first))
            {
                third = second;
                second = first;
                first = candidate;
                return;
            }

            if (candidate.IsBetterThan(second))
            {
                third = second;
                second = candidate;
                return;
            }

            if (candidate.IsBetterThan(third))
                third = candidate;
        }

        internal static bool IsSameWallRootTarget(
            WallRootCandidate candidate,
            WallRootCandidate other)
        {
            return candidate.IsValid
                   && other.IsValid
                   && (candidate.Target - other.Target).LengthSquared()
                   <= 0.0000001f;
        }

        internal static void SortWallRootOptions(PushContext state)
        {
            for (var i = 1; i < state.WallRootOptions.Count; i++)
            {
                var current = state.WallRootOptions[i];
                var insertIndex = i;
                while (insertIndex > 0
                       && ComesBefore(state,
                           current,
                           state.WallRootOptions[insertIndex - 1]))
                {
                    state.WallRootOptions[insertIndex] =
                        state.WallRootOptions[insertIndex - 1];
                    insertIndex--;
                }

                state.WallRootOptions[insertIndex] = current;
            }
        }

        internal static void EnsureWallSelectedCandidateCapacity(PushContext state)
        {
            if (state.WallSelectedCandidates.Length
                >= state.WallRootOptions.Count)
            {
                return;
            }

            System.Array.Resize(
                ref state.WallSelectedCandidates,
                state.WallRootOptions.Count);
        }

        internal static bool ComesBefore(PushContext state,
            WallRootOptions candidate,
            WallRootOptions other)
        {
            if (candidate.CandidateCount != other.CandidateCount)
                return candidate.CandidateCount < other.CandidateCount;

            return state.Items[candidate.MovingIndex].StableSortId
                   > state.Items[other.MovingIndex].StableSortId;
        }
    }
}
