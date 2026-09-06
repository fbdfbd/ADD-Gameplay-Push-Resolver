using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal static class LocalCascadePlanner
    {
        internal static bool TryBuildLocalCascadePushPlan(PushContext state,
            int pinnedIndex,
            Vector2 direction,
            bool allowSideChange,
            RootDirectionMode rootDirectionMode,
            SourceSidePolicy sourceSidePolicy)
        {
            PlanningState.EnsurePushPlanCapacity(state);
            PlanningState.ResetPlanningState(state);

            state.PlannedPushQueue.Clear();
            state.VisitedPlannedPushStates.Clear();

            var pinned = state.Items[pinnedIndex];
            PlanningState.CollectPlannedBlockers(state,
                pinnedIndex,
                pinned.Position,
                pinned);
            if (state.PlannedBlockers.Count == 0)
                return true;

            PlanningState.SortPlannedBlockersNewestFirst(state);
            System.Array.Clear(
                state.RootPushDirectionCounts,
                0,
                state.RootPushDirectionCounts.Length);
            for (var i = 0; i < state.PlannedBlockers.Count; i++)
            {
                var movingIndex = state.PlannedBlockers[i];
                var rootDirection = PushDirections.ResolveInitialPushDirection(state,
                    pinnedIndex,
                    movingIndex,
                    direction,
                    rootDirectionMode);
                PlanningState.EnqueuePlannedPush(state, new PlannedPushRequest(
                    pinnedIndex,
                    movingIndex,
                    rootDirection,
                    PushDirections.GetDirectionIndex(rootDirection),
                    forceMove: false,
                    useVacancyPath: false));
            }

            var processedStateCount = 0;
            var maxProcessedStateCount = ScalarMath.Clamp(
                state.Items.Count * OccupancyGrid.RouteCount,
                BoardPushSolverSettings.MinPlannedStateCount,
                BoardPushSolverSettings.MaxPlannedStateCount);
            while (state.PlannedPushQueue.Count > 0)
            {
                if (++processedStateCount > maxProcessedStateCount)
                    return false;

                var request = state.PlannedPushQueue.Dequeue();
                if (!TryProcessLocalCascadePush(state,
                        request,
                        pinnedIndex,
                        allowSideChange,
                        preserveSourceSide:
                            rootDirectionMode
                            == RootDirectionMode.ForcedRecovery,
                        sourceSidePolicy: sourceSidePolicy))
                {
                    return false;
                }
            }

            return PlanValidation.ValidatePlannedPush(state, pinnedIndex);
        }

        internal static bool TryProcessLocalCascadePush(PushContext state,
            PlannedPushRequest request,
            int pinnedIndex,
            bool allowSideChange,
            bool preserveSourceSide,
            SourceSidePolicy sourceSidePolicy)
        {
            var source = state.Items[request.SourceIndex];
            var moving = state.Items[request.MovingIndex];
            var sourcePosition = PlanningState.GetPlannedPosition(state, request.SourceIndex);
            var movingPosition = PlanningState.GetPlannedPosition(state, request.MovingIndex);
            if (!PushGeometry.IsOverlappingAtPositions(state,
                    source,
                    sourcePosition,
                    moving,
                    movingPosition))
            {
                return true;
            }

            if (!ItemPolicy.CanBePushed(moving))
                return false;

            Vector2 target;
            if (allowSideChange)
            {
                target = PushGeometry.GetAdjacentSeparationPosition(state,
                    sourcePosition,
                    source,
                    movingPosition,
                    moving,
                    request.Direction);
            }
            else if (!PushGeometry.TryGetMinimumSeparationPosition(state,
                         sourcePosition,
                         source,
                         movingPosition,
                         moving,
                         request.Direction,
                         out target))
            {
                return false;
            }

            if (!BoardBoundary.IsInsideBoard(state, target, moving.HalfSize))
                return false;

            if (preserveSourceSide
                && PushGeometry.WouldCrossSourceSide(
                    sourcePosition,
                    source,
                    movingPosition,
                    moving,
                    target,
                    request.Direction)
                && !PushGeometry.CanCrossSourceSide(state,
                    request,
                    pinnedIndex,
                    sourceSidePolicy,
                    sourcePosition,
                    source,
                    movingPosition,
                    moving,
                    request.Direction))
            {
                return false;
            }

            if (!PushGeometry.IsWithinLocalMoveLimit(state,
                    state.Items[request.MovingIndex].Position,
                    target,
                    source,
                    moving,
                    allowSideChange))
            {
                return false;
            }

            PlanningState.CollectPlannedBlockers(state,
                request.MovingIndex,
                target,
                moving,
                request.SourceIndex);
            for (var i = 0; i < state.PlannedBlockers.Count; i++)
            {
                if (ItemPolicy.IsFixedObstacle(state.Items[state.PlannedBlockers[i]]))
                    return false;
            }

            PlanningState.SetPlannedPosition(state, request.MovingIndex, target);
            PlanningState.SortPlannedBlockersOldestFirst(state);
            for (var i = 0; i < state.PlannedBlockers.Count; i++)
            {
                var blockerIndex = state.PlannedBlockers[i];
                var nextDirection = allowSideChange
                    ? PushDirections.ResolveOutwardPushDirection(
                        target,
                        PlanningState.GetPlannedPosition(state, blockerIndex),
                        request.Direction)
                    : request.Direction;
                var nextRouteIndex = allowSideChange
                    ? PushDirections.GetDirectionIndex(nextDirection)
                    : request.RouteIndex;
                PlanningState.EnqueuePlannedPush(state, new PlannedPushRequest(
                    request.MovingIndex,
                    blockerIndex,
                    nextDirection,
                    nextRouteIndex,
                    forceMove: false,
                    useVacancyPath: false));
            }

            return true;
        }
    }
}
