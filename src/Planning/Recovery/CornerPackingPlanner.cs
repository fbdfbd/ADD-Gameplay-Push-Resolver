using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal static class CornerPackingPlanner
    {
        internal static bool TryBuildCornerPackingPlan(PushContext state,
            int pinnedIndex,
            Vector2 preferredDirection)
        {
            if (!CornerIsland.TryGetCornerEscapeContext(state,
                    pinnedIndex,
                    preferredDirection,
                    out var context))
            {
                return false;
            }

            var hasBestPlan = false;
            var bestMoveCount = int.MaxValue;
            var bestMoveDistanceSqr = float.MaxValue;
            state.BestBoundaryItemIndices.Clear();
            var initialIslandCount = state.BoundaryIslandItems.Count;

            for (var outletOrder = 0; outletOrder < 2; outletOrder++)
            {
                CornerIsland.RestoreCornerEscapeIsland(state, initialIslandCount);
                var processedStateCount = 0;
                var preferHorizontal =
                    outletOrder == 0
                        ? context.PreferHorizontal
                        : !context.PreferHorizontal;
                if (!TryBuildCornerPackingPlanForOutletOrder(state,
                        pinnedIndex,
                        context,
                        preferHorizontal,
                        ref processedStateCount,
                        BoardPushSolverSettings.MaxCornerEscapeStates))
                {
                    continue;
                }

                var moveDistanceSqr = 0f;
                for (var itemIndex = 0;
                     itemIndex < state.PlannedItemIndices.Count;
                     itemIndex++)
                {
                    var plannedIndex = state.PlannedItemIndices[itemIndex];
                    moveDistanceSqr += (
                        state.PlannedPositions[plannedIndex]
                        - state.Items[plannedIndex].Position).LengthSquared();
                }

                var moveCount = state.PlannedItemIndices.Count;
                if (hasBestPlan
                    && (moveCount > bestMoveCount
                        || (moveCount == bestMoveCount
                            && moveDistanceSqr >= bestMoveDistanceSqr)))
                {
                    continue;
                }

                hasBestPlan = true;
                bestMoveCount = moveCount;
                bestMoveDistanceSqr = moveDistanceSqr;
                state.BestBoundaryItemIndices.Clear();
                for (var itemIndex = 0;
                     itemIndex < state.PlannedItemIndices.Count;
                     itemIndex++)
                {
                    var plannedIndex = state.PlannedItemIndices[itemIndex];
                    state.BestBoundaryItemIndices.Add(plannedIndex);
                    state.BestBoundaryPositions[plannedIndex] =
                        state.PlannedPositions[plannedIndex];
                }
            }

            CornerIsland.RestoreCornerEscapeIsland(state, initialIslandCount);
            PlanningState.ResetPlanningState(state);
            if (!hasBestPlan)
                return false;

            for (var i = 0; i < state.BestBoundaryItemIndices.Count; i++)
            {
                var itemIndex = state.BestBoundaryItemIndices[i];
                PlanningState.SetPlannedPosition(state,
                    itemIndex,
                    state.BestBoundaryPositions[itemIndex]);
            }

            return PlanValidation.ValidateCornerEscapePlan(state, pinnedIndex);
        }

        internal static bool TryBuildCornerPackingPlanForOutletOrder(PushContext state,
            int pinnedIndex,
            CornerEscapeContext context,
            bool preferHorizontal,
            ref int processedStateCount,
            int stateBudget)
        {
            PlanningState.ResetPlanningState(state);
            state.BoundaryPositionChanges.Clear();
            state.ActiveCornerPackingStates.Clear();
            state.BacktrackingRequests.Clear();

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
            var rootRequests = state.PlannedBlockers.Count;
            for (var i = 0; i < state.PlannedBlockers.Count; i++)
            {
                var movingIndex = state.PlannedBlockers[i];
                if (!ItemPolicy.CanBePushed(state.Items[movingIndex]))
                    return false;

                var radialDirection = PushDirections.ResolveRootPushDirection(state,
                    pinnedIndex,
                    movingIndex,
                    preferHorizontal
                        ? context.HorizontalOutlet
                        : context.VerticalOutlet);
                var rootDirection = context.ResolveOutlet(
                    radialDirection,
                    preferHorizontal);
                state.BacktrackingRequests.Add(new PlannedPushRequest(
                    pinnedIndex,
                    movingIndex,
                    rootDirection,
                    PushDirections.GetDirectionIndex(rootDirection),
                    forceMove: false,
                    useVacancyPath: false));
            }

            for (var i = 0; i < rootRequests; i++)
            {
                if (!CornerPackingSearch.TryProcessCornerPackingState(state,
                        state.BacktrackingRequests[i],
                        context,
                        preferHorizontal,
                        ref processedStateCount,
                        stateBudget))
                {
                    return false;
                }
            }

            return PlanValidation.ValidateCornerEscapePlan(state, pinnedIndex);
        }
    }
}
