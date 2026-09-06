using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal static class PushTarget
    {
        internal static bool TryGetPlannedPushTarget(PushContext state,
            PlannedPushRequest request,
            int pinnedIndex,
            SourceSidePolicy sourceSidePolicy,
            Vector2 sourcePosition,
            BoardItem source,
            Vector2 movingPosition,
            BoardItem moving,
            out Vector2 target,
            out Vector2 pushDirection,
            out bool continueBoundaryEscapeChain)
        {
            PushDirections.FillPushDirections(state, request.Direction, moving.StableSortId);
            VacancyPlanning.PrioritizePreparedVacancyDirection(state, request, sourcePosition);
            var startsBoundaryEscapeChain =
                sourceSidePolicy
                == SourceSidePolicy.AllowRootBoundaryEscape
                && request.SourceIndex == pinnedIndex
                && PushGeometry.TryGetMinimumSeparationPosition(state,
                    sourcePosition,
                    source,
                    movingPosition,
                    moving,
                    request.Direction,
                    out var radialTarget)
                && !BoardBoundary.IsInsideBoard(state, radialTarget, moving.HalfSize);

            for (var directionIndex = 0;
                 directionIndex < state.PushDirections.Length;
                 directionIndex++)
            {
                var direction = state.PushDirections[directionIndex];
                if (!TryGetPlannedSeparationPosition(state,
                        request,
                        pinnedIndex,
                        sourceSidePolicy,
                        sourcePosition,
                        source,
                        movingPosition,
                        moving,
                        direction,
                        out var candidate,
                        out var crossedSourceSide))
                {
                    continue;
                }

                if (!BoardBoundary.IsInsideBoard(state, candidate, moving.HalfSize))
                    continue;

                var allowsBoundaryCrossingMove =
                    request.AllowBoundaryChainMove
                    || startsBoundaryEscapeChain
                    || crossedSourceSide;
                if (!PushGeometry.IsWithinLocalMoveLimit(state,
                        state.Items[request.MovingIndex].Position,
                        candidate,
                        source,
                        moving,
                        allowSideChange:
                            state.ResolveDirectionPolicy
                            == ResolveDirectionPolicy.TryAlternates
                            || allowsBoundaryCrossingMove))
                {
                    continue;
                }

                PlanningState.CollectPlannedBlockers(state,
                    request.MovingIndex,
                    candidate,
                    moving,
                    request.SourceIndex);

                var isBlockedByPinnedItem = false;
                for (var blockerIndex = 0;
                     blockerIndex < state.PlannedBlockers.Count;
                     blockerIndex++)
                {
                    if (!ItemPolicy.IsFixedObstacle(
                            state.Items[state.PlannedBlockers[blockerIndex]]))
                        continue;

                    isBlockedByPinnedItem = true;
                    break;
                }

                if (isBlockedByPinnedItem)
                    continue;

                target = candidate;
                pushDirection = direction;
                continueBoundaryEscapeChain =
                    request.AllowBoundaryChainMove
                    || startsBoundaryEscapeChain;
                return true;
            }

            target = default;
            pushDirection = default;
            continueBoundaryEscapeChain = false;
            return false;
        }

        internal static bool TryGetPlannedSeparationPosition(PushContext state,
            PlannedPushRequest request,
            int pinnedIndex,
            SourceSidePolicy sourceSidePolicy,
            Vector2 sourcePosition,
            BoardItem source,
            Vector2 movingPosition,
            BoardItem moving,
            Vector2 direction,
            out Vector2 target,
            out bool crossedSourceSide)
        {
            var allowsCandidateDirection =
                sourceSidePolicy == SourceSidePolicy.AllowRootBoundaryEscape
                || PushDirections.GetDirectionIndex(direction)
                == PushDirections.GetDirectionIndex(request.Direction);
            if (PushGeometry.CanCrossSourceSide(state,
                    request,
                    pinnedIndex,
                    sourceSidePolicy,
                    sourcePosition,
                    source,
                    movingPosition,
                    moving,
                    direction)
                && allowsCandidateDirection)
            {
                target = PushGeometry.GetAdjacentSeparationPosition(state,
                    sourcePosition,
                    source,
                    movingPosition,
                    moving,
                    direction);
                crossedSourceSide = true;
                return true;
            }

            crossedSourceSide = false;
            return PushGeometry.TryGetMinimumSeparationPosition(state,
                sourcePosition,
                source,
                movingPosition,
                moving,
                direction,
                out target);
        }
    }
}
