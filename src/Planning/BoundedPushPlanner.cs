using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal static class BoundedPushPlanner
    {
        internal static bool TryBuildBoundedPushPlan(PushContext state,
            int pinnedIndex,
            Vector2 preferredDirection,
            RootDirectionMode rootDirectionMode,
            SourceSidePolicy sourceSidePolicy)
        {
            PlanningState.EnsurePushPlanCapacity(state);
            PlanningState.ResetPlanningState(state);

            state.PlannedPushQueue.Clear();
            state.VisitedPlannedPushStates.Clear();
            PlanningState.CollectPlannedBlockers(state,
                pinnedIndex,
                state.Items[pinnedIndex].Position,
                state.Items[pinnedIndex]);
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
                var initialDirection = PushDirections.ResolveInitialPushDirection(state,
                    pinnedIndex,
                    movingIndex,
                    preferredDirection,
                    rootDirectionMode);
                PlanningState.EnqueuePlannedPush(state, new PlannedPushRequest(
                    pinnedIndex,
                    movingIndex,
                    initialDirection,
                    PushDirections.GetDirectionIndex(initialDirection),
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
                if (!TryProcessPlannedPush(state,
                        request,
                        pinnedIndex,
                        sourceSidePolicy))
                    return false;
            }

            return PlanValidation.ValidatePlannedPush(state, pinnedIndex);
        }

        internal static bool TryProcessPlannedPush(PushContext state,
            PlannedPushRequest request,
            int pinnedIndex,
            SourceSidePolicy sourceSidePolicy)
        {
            var source = state.Items[request.SourceIndex];
            var moving = state.Items[request.MovingIndex];
            var sourcePosition = PlanningState.GetPlannedPosition(state, request.SourceIndex);
            var movingPosition = PlanningState.GetPlannedPosition(state, request.MovingIndex);

            if (!request.ForceMove
                && !PushGeometry.IsOverlappingAtPositions(state,
                    source,
                    sourcePosition,
                    moving,
                    movingPosition))
            {
                return true;
            }

            if (!ItemPolicy.CanBePushed(moving))
                return false;

            state.PlannedRouteByItem[request.MovingIndex] = request.RouteIndex;

            if (!PushTarget.TryGetPlannedPushTarget(state,
                    request,
                    pinnedIndex,
                    sourceSidePolicy,
                    sourcePosition,
                    source,
                    movingPosition,
                    moving,
                    out var target,
                    out var pushDirection,
                    out var continueBoundaryEscapeChain))
            {
                return false;
            }

            PlanningState.SetPlannedPosition(state, request.MovingIndex, target);
            PlanningState.SortPlannedBlockersOldestFirst(state);
            for (var blockerIndex = 0;
                 blockerIndex < state.PlannedBlockers.Count;
                 blockerIndex++)
            {
                PlanningState.EnqueuePlannedPush(state, new PlannedPushRequest(
                    request.MovingIndex,
                    state.PlannedBlockers[blockerIndex],
                    pushDirection,
                    request.RouteIndex,
                    forceMove: false,
                    useVacancyPath: true,
                    allowBoundaryChainMove:
                        continueBoundaryEscapeChain));
            }

            return true;
        }
    }
}
