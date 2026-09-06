using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal static class BacktrackingPlanner
    {
        internal static bool TryBuildBacktrackingPushPlan(PushContext state,
            int pinnedIndex,
            bool hasVacancySearch,
            int vacancyStartCell,
            bool allowRootTangents)
        {
            PlanningState.EnsurePushPlanCapacity(state);
            PlanningRollback.EnsureBacktrackingSnapshotCapacity(state);
            for (var directionIndex = 0;
                 directionIndex < state.ResolveAttemptDirections.Length;
                 directionIndex++)
            {
                PlanningState.ResetPlanningState(state);
                state.HasPreparedVacancyPath = hasVacancySearch
                                           && VacancyPlanning.PrepareVacancyPaths(state,
                                               vacancyStartCell,
                                               alternateRouteMask: 0);
                state.BacktrackingRequests.Clear();

                var pinned = state.Items[pinnedIndex];
                PlanningState.CollectPlannedBlockers(state,
                    pinnedIndex,
                    pinned.Position,
                    pinned);
                if (state.PlannedBlockers.Count == 0)
                    return true;

                PlanningState.SortPlannedBlockersNewestFirst(state);
                var rootDirection = state.ResolveAttemptDirections[directionIndex];
                var routeIndex = PushDirections.GetDirectionIndex(rootDirection);
                for (var blockerIndex = 0;
                     blockerIndex < state.PlannedBlockers.Count;
                     blockerIndex++)
                {
                    state.BacktrackingRequests.Add(new PlannedPushRequest(
                        pinnedIndex,
                        state.PlannedBlockers[blockerIndex],
                        rootDirection,
                        routeIndex,
                        forceMove: false,
                        useVacancyPath: true));
                }

                var backtrackingAttemptCount = 0;
                if (TryProcessBacktrackingRequests(state,
                        pinnedIndex,
                        requestIndex: 0,
                        depth: 0,
                        allowRootTangents: allowRootTangents,
                        ref backtrackingAttemptCount))
                {
                    return true;
                }
            }

            PlanningState.ResetPlanningState(state);
            return false;
        }

        internal static bool TryProcessBacktrackingRequests(PushContext state,
            int pinnedIndex,
            int requestIndex,
            int depth,
            bool allowRootTangents,
            ref int backtrackingAttemptCount)
        {
            if (requestIndex >= state.BacktrackingRequests.Count)
                return PlanValidation.ValidatePlannedPush(state, pinnedIndex);

            if (depth >= BoardPushSolverSettings.MaxBacktrackingDepth)
                return false;

            var request = state.BacktrackingRequests[requestIndex];
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
                return TryProcessBacktrackingRequests(state,
                    pinnedIndex,
                    requestIndex + 1,
                    depth + 1,
                    allowRootTangents,
                    ref backtrackingAttemptCount);
            }

            if (!ItemPolicy.CanBePushed(moving))
                return false;

            for (var candidateIndex = 0;
                 candidateIndex < state.PushDirections.Length;
                 candidateIndex++)
            {
                if (!TryGetBacktrackingPushCandidate(state,
                        request,
                        sourcePosition,
                        source,
                        movingPosition,
                        moving,
                        candidateIndex,
                        isRootRequest: request.SourceIndex == pinnedIndex,
                        allowRootTangents: allowRootTangents,
                        out var target,
                        out var pushDirection))
                {
                    break;
                }

                if (candidateIndex > 0
                    && ++backtrackingAttemptCount
                    > BoardPushSolverSettings.MaxBacktrackingAttemptsPerDirection)
                {
                    return false;
                }

                var checkpoint = PlanningRollback.CapturePlanningCheckpoint(state,
                    depth,
                    state.BacktrackingRequests.Count);

                state.PlannedRouteByItem[request.MovingIndex] = request.RouteIndex;
                PlanningState.SetPlannedPosition(state, request.MovingIndex, target);
                PlanningState.SortPlannedBlockersOldestFirst(state);
                for (var blockerIndex = 0;
                     blockerIndex < state.PlannedBlockers.Count;
                     blockerIndex++)
                {
                    state.BacktrackingRequests.Add(new PlannedPushRequest(
                        request.MovingIndex,
                        state.PlannedBlockers[blockerIndex],
                        pushDirection,
                        request.RouteIndex,
                        forceMove: false,
                        useVacancyPath: true));
                }

                if (TryProcessBacktrackingRequests(state,
                        pinnedIndex,
                        requestIndex + 1,
                        depth + 1,
                        allowRootTangents,
                        ref backtrackingAttemptCount))
                {
                    return true;
                }

                PlanningRollback.RestorePlanningCheckpoint(state, checkpoint);
            }

            return false;
        }

        internal static bool TryGetBacktrackingPushCandidate(PushContext state,
            PlannedPushRequest request,
            Vector2 sourcePosition,
            BoardItem source,
            Vector2 movingPosition,
            BoardItem moving,
            int requestedCandidateIndex,
            bool isRootRequest,
            bool allowRootTangents,
            out Vector2 target,
            out Vector2 pushDirection)
        {
            PushDirections.FillPushDirections(state, request.Direction, moving.StableSortId);
            if (isRootRequest)
            {
                state.PushDirections[0] = PushDirections.GetDirection(
                    PushDirections.GetDirectionIndex(request.Direction));
            }
            else
            {
                VacancyPlanning.PrioritizePreparedVacancyDirection(state,
                    request,
                    sourcePosition);
            }

            var validCandidateIndex = 0;

            var directionCount = state.PushDirections.Length;
            if (isRootRequest)
            {
                directionCount = allowRootTangents
                    ? state.PushDirections.Length - 1
                    : 1;
            }
            for (var directionIndex = 0;
                 directionIndex < directionCount;
                 directionIndex++)
            {
                var direction = state.PushDirections[directionIndex];
                if (!PushGeometry.TryGetMinimumSeparationPosition(state,
                        sourcePosition,
                        source,
                        movingPosition,
                        moving,
                        direction,
                        out var candidate)
                    || !BoardBoundary.IsInsideBoard(state, candidate, moving.HalfSize)
                    || !PushGeometry.IsWithinLocalMoveLimit(state,
                        state.Items[request.MovingIndex].Position,
                        candidate,
                        source,
                        moving,
                        allowSideChange: true))
                {
                    continue;
                }

                PlanningState.CollectPlannedBlockers(state,
                    request.MovingIndex,
                    candidate,
                    moving,
                    request.SourceIndex);
                var blockedByPinnedItem = false;
                for (var blockerIndex = 0;
                     blockerIndex < state.PlannedBlockers.Count;
                     blockerIndex++)
                {
                    if (!ItemPolicy.IsFixedObstacle(
                            state.Items[state.PlannedBlockers[blockerIndex]]))
                        continue;

                    blockedByPinnedItem = true;
                    break;
                }

                if (blockedByPinnedItem)
                    continue;

                if (validCandidateIndex++ != requestedCandidateIndex)
                    continue;

                target = candidate;
                pushDirection = direction;
                return true;
            }

            target = default;
            pushDirection = default;
            return false;
        }
    }
}
