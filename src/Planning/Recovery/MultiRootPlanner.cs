using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal static class MultiRootPlanner
    {
        internal static bool TryBuildWallConstrainedRootPushPlan(PushContext state,
            int pinnedIndex,
            Vector2 preferredDirection)
        {
            return TryBuildMultiRootPushPlan(state,
                pinnedIndex,
                preferredDirection,
                requireBoundaryBlockedRoot: true);
        }

        internal static bool TryBuildPinnedMultiRootPushPlan(PushContext state,
            int pinnedIndex,
            Vector2 preferredDirection)
        {
            var pinned = state.Items[pinnedIndex];
            if (pinned.PlanPolicy != PushPlanPolicy.MultiRoot)
            {
                return false;
            }

            return TryBuildMultiRootPushPlan(state,
                pinnedIndex,
                preferredDirection,
                requireBoundaryBlockedRoot: false);
        }

        internal static bool TryBuildMultiRootPushPlan(PushContext state,
            int pinnedIndex,
            Vector2 preferredDirection,
            bool requireBoundaryBlockedRoot)
        {
            if (!ItemPolicy.IsOccupancyMovable(state.Items[pinnedIndex]))
                return false;

            state.HasPreparedVacancyPath = false;
            PlanningState.ResetPlanningState(state);
            if (!WallRootCandidates.CollectWallRootCandidates(state,
                    pinnedIndex,
                    preferredDirection,
                    requireBoundaryBlockedRoot))
            {
                return false;
            }

            WallRootCandidates.EnsureWallSelectedCandidateCapacity(state);
            WallRootCandidates.SortWallRootOptions(state);

            var attemptCount = 0;
            var searchStateCount = 0;
            var maxPlanAttempts = requireBoundaryBlockedRoot
                ? BoardPushSolverSettings.MaxWallRootPlanAttempts
                : ScalarMath.Clamp(
                    state.WallRootOptions.Count
                    * OccupancyGrid.RouteCount,
                    BoardPushSolverSettings.MaxWallRootPlanAttempts,
                    128);
            var maxSearchStates = requireBoundaryBlockedRoot
                ? BoardPushSolverSettings.MaxWallRootSearchStates
                : ScalarMath.Clamp(
                    state.WallRootOptions.Count
                    * OccupancyGrid.RouteCount
                    * OccupancyGrid.RouteCount,
                    BoardPushSolverSettings.MaxWallRootSearchStates,
                    BoardPushSolverSettings.MaxPlannedStateCount);
            return TryBuildWallRootDirectionPlan(state,
                pinnedIndex,
                0,
                maxPlanAttempts,
                maxSearchStates,
                ref attemptCount,
                ref searchStateCount);
        }

        internal static bool TryBuildWallRootDirectionPlan(PushContext state,
            int pinnedIndex,
            int optionIndex,
            int maxPlanAttempts,
            int maxSearchStates,
            ref int attemptCount,
            ref int searchStateCount)
        {
            if (attemptCount >= maxPlanAttempts
                || searchStateCount >= maxSearchStates)
                return false;

            if (optionIndex >= state.WallRootOptions.Count)
            {
                attemptCount++;
                return TryApplyWallRootDirectionPlan(state, pinnedIndex);
            }

            var options = state.WallRootOptions[optionIndex];
            for (var candidateIndex = 0;
                 candidateIndex < options.CandidateCount;
                 candidateIndex++)
            {
                if (++searchStateCount > maxSearchStates)
                    return false;

                var candidate = options.GetCandidate(candidateIndex);
                if (!IsWallRootCandidateCompatible(state,
                        optionIndex,
                        options.MovingIndex,
                        candidate))
                {
                    continue;
                }

                state.WallSelectedCandidates[optionIndex] = candidate;
                if (TryBuildWallRootDirectionPlan(state,
                        pinnedIndex,
                        optionIndex + 1,
                        maxPlanAttempts,
                        maxSearchStates,
                        ref attemptCount,
                        ref searchStateCount))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsWallRootCandidateCompatible(PushContext state,
            int optionIndex,
            int movingIndex,
            WallRootCandidate candidate)
        {
            var moving = state.Items[movingIndex];
            for (var previousIndex = 0;
                 previousIndex < optionIndex;
                 previousIndex++)
            {
                var previousOptions = state.WallRootOptions[previousIndex];
                var previous = state.Items[previousOptions.MovingIndex];
                var previousCandidate =
                    state.WallSelectedCandidates[previousIndex];
                if (PushGeometry.IsOverlappingAtPositions(state,
                        moving,
                        candidate.Target,
                        previous,
                        previousCandidate.Target))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool TryApplyWallRootDirectionPlan(PushContext state, int pinnedIndex)
        {
            PlanningState.EnsurePushPlanCapacity(state);
            PlanningState.ResetPlanningState(state);
            state.PlannedPushQueue.Clear();
            state.VisitedPlannedPushStates.Clear();

            for (var optionIndex = 0;
                 optionIndex < state.WallRootOptions.Count;
                 optionIndex++)
            {
                var options = state.WallRootOptions[optionIndex];
                PlanningState.SetPlannedPosition(state,
                    options.MovingIndex,
                    state.WallSelectedCandidates[optionIndex].Target);
            }

            for (var optionIndex = 0;
                 optionIndex < state.WallRootOptions.Count;
                 optionIndex++)
            {
                var options = state.WallRootOptions[optionIndex];
                var candidate = state.WallSelectedCandidates[optionIndex];
                var moving = state.Items[options.MovingIndex];
                PlanningState.CollectPlannedBlockers(state,
                    options.MovingIndex,
                    candidate.Target,
                    moving,
                    pinnedIndex);
                PlanningState.SortPlannedBlockersOldestFirst(state);
                for (var blockerIndex = 0;
                     blockerIndex < state.PlannedBlockers.Count;
                     blockerIndex++)
                {
                    PlanningState.EnqueuePlannedPush(state, new PlannedPushRequest(
                        options.MovingIndex,
                        state.PlannedBlockers[blockerIndex],
                        candidate.Direction,
                        PushDirections.GetDirectionIndex(candidate.Direction),
                        forceMove: false,
                        useVacancyPath: false));
                }
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

                if (!BoundedPushPlanner.TryProcessPlannedPush(state,
                        state.PlannedPushQueue.Dequeue(),
                        pinnedIndex,
                        SourceSidePolicy.Preserve))
                    return false;
            }

            return PlanValidation.ValidatePlannedPush(state, pinnedIndex);
        }
    }
}
