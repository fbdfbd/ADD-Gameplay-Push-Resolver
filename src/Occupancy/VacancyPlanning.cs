using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal static class VacancyPlanning
    {
        internal static void PrioritizePreparedVacancyDirection(PushContext state,
            PlannedPushRequest request,
            Vector2 sourcePosition)
        {
            if (!request.UseVacancyPath
                || !state.HasPreparedVacancyPath
                || !VacancyRoutes.TryGetPreparedPathDirection(state.OccupancyGrid,
                    sourcePosition,
                    request.RouteIndex,
                    out var vacancyDirection))
            {
                return;
            }

            var vacancyDirectionIndex = PushDirections.GetDirectionIndex(vacancyDirection);
            for (var i = 0; i < state.PushDirections.Length; i++)
            {
                if (PushDirections.GetDirectionIndex(state.PushDirections[i])
                    != vacancyDirectionIndex)
                {
                    continue;
                }

                (state.PushDirections[0], state.PushDirections[i]) =
                    (state.PushDirections[i], state.PushDirections[0]);
                return;
            }
        }

        internal static bool PrepareVacancyPaths(PushContext state, int startCell, int alternateRouteMask)
        {
            state.HasPreparedVacancyPath = false;
            for (var routeIndex = 0;
                 routeIndex < OccupancyGrid.RouteCount;
                 routeIndex++)
            {
                var candidates = state.VacancyCandidatesByRoute[routeIndex];
                if (candidates.Count == 0)
                    continue;

                var candidateIndex = (alternateRouteMask & (1 << routeIndex)) != 0
                    ? 1
                    : 0;
                if (candidateIndex >= candidates.Count
                    || !VacancyRoutes.PreparePath(state.OccupancyGrid,
                        routeIndex,
                        startCell,
                        candidates[candidateIndex]))
                {
                    return false;
                }

                state.HasPreparedVacancyPath = true;
            }

            return state.HasPreparedVacancyPath;
        }

        internal static bool TryPrepareVacancySearch(PushContext state,
            int pinnedIndex,
            out int startCell)
        {
            startCell = -1;
            for (var routeIndex = 0;
                 routeIndex < OccupancyGrid.RouteCount;
                 routeIndex++)
            {
                state.VacancyCandidatesByRoute[routeIndex].Clear();
            }

            if (!state.BoardBounds.TryGetBounds(out var boardMin, out var boardMax))
            {
                return false;
            }

            var pinned = state.Items[pinnedIndex];
            var searchHalfSize = GetSmallestPushableHalfSize(state);
            var searchMargin = Vector2.One * ScalarMath.Max(0f, state.Settings.Padding);
            var pitch = (searchHalfSize * 2f
                         + searchMargin
                         + Vector2.One * 0.001f) * 0.25f;
            var boardPadding = state.Settings.BoardPadding;
            var minCenter = boardMin + searchHalfSize + Vector2.One * boardPadding;
            var maxCenter = boardMax - searchHalfSize - Vector2.One * boardPadding;
            if (!state.OccupancyGrid.Configure(
                    minCenter,
                    maxCenter,
                    pinned.Position,
                    pitch,
                    state.Items.Count))
            {
                return false;
            }

            state.OccupancyGrid.SetMovingHalfSize(searchHalfSize);
            for (var i = 0; i < state.Items.Count; i++)
            {
                if (i == pinnedIndex)
                    continue;

                var item = state.Items[i];
                var margin = PushGeometry.GetSeparationMargin(state, pinned, item);
                if (ItemPolicy.CanBePushed(item))
                {
                    state.OccupancyGrid.InsertItem(
                        i,
                        item.Position,
                        item.HalfSize,
                        margin,
                        item.StableSortId);
                    continue;
                }

                state.OccupancyGrid.MarkBlocked(
                    item.Position,
                    item.HalfSize,
                    searchHalfSize,
                    margin);
            }

            startCell = state.OccupancyGrid.GetNearestCell(pinned.Position);
            if (state.OccupancyGrid.IsFixedBlocked(startCell))
                return false;

            var foundVacancy = false;
            for (var routeIndex = 0;
                 routeIndex < OccupancyGrid.RouteCount;
                 routeIndex++)
            {
                foundVacancy |= VacancyRoutes.CollectReachableVacancies(state.OccupancyGrid,
                    startCell,
                    PushDirections.GetDirection(routeIndex),
                    state.VacancyCandidatesByRoute[routeIndex],
                    BoardPushSolverSettings.PreferredVacancyCandidatesPerRoute,
                    routeIndex);
            }

            return foundVacancy;
        }

        internal static int CountSetBits(int value)
        {
            var count = 0;
            while (value != 0)
            {
                count += value & 1;
                value >>= 1;
            }

            return count;
        }

        internal static Vector2 GetSmallestPushableHalfSize(PushContext state)
        {
            var smallest = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            for (var i = 0; i < state.Items.Count; i++)
            {
                var item = state.Items[i];
                if (!ItemPolicy.CanBePushed(item))
                    continue;

                smallest = Vector2.Min(smallest, item.HalfSize);
            }

            return float.IsPositiveInfinity(smallest.X)
                ? Vector2.One * 0.5f
                : smallest;
        }
    }
}
