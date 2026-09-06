using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal static class PushPlanner
    {
        internal static bool TryBuildOccupancyPushPlan(PushContext state,
            int pinnedIndex,
            Vector2 preferredDirection)
        {
            PushDirections.FillPushDirections(state,
                preferredDirection,
                state.Items[pinnedIndex].StableSortId);
            System.Array.Copy(
                state.PushDirections,
                state.ResolveAttemptDirections,
                state.PushDirections.Length);

            var pinned = state.Items[pinnedIndex];
            if (pinned.PlanPolicy == PushPlanPolicy.MultiRoot)
            {
                PlanningState.InitializePlanningGrid(state);
                return MultiRootPlanner.TryBuildPinnedMultiRootPushPlan(state,
                    pinnedIndex,
                    preferredDirection);
            }

            var hasVacancySearch = VacancyPlanning.TryPrepareVacancySearch(state,
                pinnedIndex,
                out var vacancyStartCell);
            var alternateRouteMask = 0;
            if (hasVacancySearch)
            {
                for (var routeIndex = 0;
                     routeIndex < OccupancyGrid.RouteCount;
                     routeIndex++)
                {
                    if (state.VacancyCandidatesByRoute[routeIndex].Count > 1)
                        alternateRouteMask |= 1 << routeIndex;
                }
            }

            PlanningState.InitializePlanningGrid(state);

            if (TryBuildOccupancyPushPlanForDirection(state,
                    pinnedIndex,
                    preferredDirection,
                    hasVacancySearch,
                    vacancyStartCell,
                    alternateRouteMask,
                    RootDirectionMode.RadialFastPath,
                    SourceSidePolicy.Preserve))
            {
                return true;
            }

            if (MultiRootPlanner.TryBuildWallConstrainedRootPushPlan(state,
                    pinnedIndex,
                    preferredDirection))
            {
                return true;
            }

            if (TryBuildRadialBoundaryEscapeRecovery(state,
                    pinnedIndex,
                    preferredDirection,
                    hasVacancySearch,
                    vacancyStartCell,
                    alternateRouteMask))
            {
                return true;
            }

            if (TryBuildForcedDirectionRecovery(state,
                    pinnedIndex,
                    hasVacancySearch,
                    vacancyStartCell,
                    alternateRouteMask))
            {
                return true;
            }

            if (BacktrackingPlanner.TryBuildBacktrackingPushPlan(state,
                    pinnedIndex,
                    hasVacancySearch,
                    vacancyStartCell,
                    allowRootTangents: false))
            {
                return true;
            }

            return CornerPackingPlanner.TryBuildCornerPackingPlan(state,
                pinnedIndex,
                preferredDirection);
        }

        internal static bool TryBuildRadialBoundaryEscapeRecovery(PushContext state,
            int pinnedIndex,
            Vector2 preferredDirection,
            bool hasVacancySearch,
            int vacancyStartCell,
            int alternateRouteMask)
        {
            return TryBuildOccupancyPushPlanForDirection(state,
                pinnedIndex,
                preferredDirection,
                hasVacancySearch,
                vacancyStartCell,
                alternateRouteMask,
                RootDirectionMode.RadialFastPath,
                SourceSidePolicy.AllowRootBoundaryEscape);
        }

        internal static bool TryBuildForcedDirectionRecovery(PushContext state,
            int pinnedIndex,
            bool hasVacancySearch,
            int vacancyStartCell,
            int alternateRouteMask)
        {
            for (var i = 1; i < state.ResolveAttemptDirections.Length; i++)
            {
                var sourceSidePolicy =
                    i == state.ResolveAttemptDirections.Length - 1
                    && state.ResolveDirectionPolicy
                    == ResolveDirectionPolicy.TryAlternates
                        ? SourceSidePolicy.AllowGeneratedRootReverse
                        : SourceSidePolicy.Preserve;
                if (TryBuildOccupancyPushPlanForDirection(state,
                        pinnedIndex,
                        state.ResolveAttemptDirections[i],
                        hasVacancySearch,
                        vacancyStartCell,
                        alternateRouteMask,
                        RootDirectionMode.ForcedRecovery,
                        sourceSidePolicy))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool TryBuildOccupancyPushPlanForDirection(PushContext state,
            int pinnedIndex,
            Vector2 preferredDirection,
            bool hasVacancySearch,
            int vacancyStartCell,
            int alternateRouteMask,
            RootDirectionMode rootDirectionMode,
            SourceSidePolicy sourceSidePolicy)
        {
            if (PlanningState.FindOldestOverlappingItem(state, pinnedIndex) < 0)
            {
                state.HasPreparedVacancyPath = false;
                return true;
            }

            if (!hasVacancySearch)
            {
                state.HasPreparedVacancyPath = false;
                if (BoundedPushPlanner.TryBuildBoundedPushPlan(state,
                        pinnedIndex,
                        preferredDirection,
                        rootDirectionMode,
                        sourceSidePolicy))
                    return true;

                return LocalCascadePlanner.TryBuildLocalCascadePushPlan(state,
                    pinnedIndex,
                    preferredDirection,
                    allowSideChange:
                        rootDirectionMode == RootDirectionMode.ForcedRecovery
                        || state.ResolveDirectionPolicy == ResolveDirectionPolicy.TryAlternates,
                    rootDirectionMode: rootDirectionMode,
                    sourceSidePolicy: sourceSidePolicy);
            }

            var attemptCount = 0;
            for (var alternateCount = 0;
                 alternateCount <= OccupancyGrid.RouteCount;
                 alternateCount++)
            {
                for (var routeMask = 0;
                     routeMask < 1 << OccupancyGrid.RouteCount;
                     routeMask++)
                {
                    if (VacancyPlanning.CountSetBits(routeMask) != alternateCount
                        || (routeMask & ~alternateRouteMask) != 0)
                    {
                        continue;
                    }

                    if (++attemptCount > BoardPushSolverSettings.PreferredVacancyPlanAttempts)
                        break;

                    if (!VacancyPlanning.PrepareVacancyPaths(state, vacancyStartCell, routeMask))
                        continue;

                    if (BoundedPushPlanner.TryBuildBoundedPushPlan(state,
                            pinnedIndex,
                            preferredDirection,
                            rootDirectionMode,
                            sourceSidePolicy))
                        return true;
                }

                if (attemptCount > BoardPushSolverSettings.PreferredVacancyPlanAttempts)
                    break;
            }

            state.HasPreparedVacancyPath = false;
            if (BoundedPushPlanner.TryBuildBoundedPushPlan(state,
                    pinnedIndex,
                    preferredDirection,
                    rootDirectionMode,
                    sourceSidePolicy))
                return true;

            return LocalCascadePlanner.TryBuildLocalCascadePushPlan(state,
                pinnedIndex,
                preferredDirection,
                allowSideChange:
                    rootDirectionMode == RootDirectionMode.ForcedRecovery
                    || state.ResolveDirectionPolicy == ResolveDirectionPolicy.TryAlternates,
                rootDirectionMode: rootDirectionMode,
                sourceSidePolicy: sourceSidePolicy);
        }
    }
}
