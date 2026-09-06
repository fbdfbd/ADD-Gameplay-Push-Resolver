using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal static class VacancyRoutes
    {
        internal static bool CollectReachableVacancies(OccupancyGrid grid,
            int startCell,
            Vector2 preferredDirection,
            List<int> vacancies,
            int maxVacancies,
            int routeIndex)
        {
            vacancies.Clear();
            if (startCell < 0
                || startCell >= grid.CellCount
                || routeIndex < 0
                || routeIndex >= OccupancyGrid.RouteCount
                || maxVacancies <= 0
                || grid._blockedCells[startCell])
            {
                return false;
            }

            ResolveDirections(
                preferredDirection,
                out var forwardX,
                out var forwardY,
                out var sideX,
                out var sideY);

            BeginVisit(grid);
            var read = 0;
            var write = 0;
            var routeOffset = GetRouteOffset(grid, routeIndex);
            grid._visitedStamps[startCell] = grid._visitStamp;
            grid._routeParentCells[routeOffset + startCell] = -1;
            grid._bfsQueue[write++] = startCell;

            while (read < write)
            {
                var cell = grid._bfsQueue[read++];
                if (cell != startCell && grid._blockingCountByCell[cell] == 0)
                {
                    vacancies.Add(cell);
                    if (vacancies.Count >= maxVacancies)
                        break;

                    continue;
                }

                var x = cell % grid._width;
                var y = cell / grid._width;
                TryVisitRoute(grid,
                    x + forwardX,
                    y + forwardY,
                    cell,
                    routeOffset,
                    ref write);
                TryVisitRoute(grid,
                    x + sideX,
                    y + sideY,
                    cell,
                    routeOffset,
                    ref write);
                TryVisitRoute(grid,
                    x - sideX,
                    y - sideY,
                    cell,
                    routeOffset,
                    ref write);
                TryVisitRoute(grid,
                    x - forwardX,
                    y - forwardY,
                    cell,
                    routeOffset,
                    ref write);
            }

            return vacancies.Count > 0;
        }

        internal static bool PreparePath(OccupancyGrid grid,
            int routeIndex,
            int startCell,
            int emptyCell)
        {
            if (routeIndex < 0
                || routeIndex >= OccupancyGrid.RouteCount
                || startCell < 0
                || startCell >= grid.CellCount
                || emptyCell < 0
                || emptyCell >= grid.CellCount)
            {
                return false;
            }

            BeginPreparedPath(grid, routeIndex);
            var routeOffset = GetRouteOffset(grid, routeIndex);
            var pathStamp = grid._preparedPathStamps[routeIndex];
            var cell = emptyCell;
            while (cell != startCell)
            {
                var parent = grid._routeParentCells[routeOffset + cell];
                if (parent < 0 || parent >= grid.CellCount)
                    return false;

                grid._routePathNextCells[routeOffset + parent] = cell;
                grid._routePathStamps[routeOffset + parent] = pathStamp;
                cell = parent;
            }

            return true;
        }

        internal static bool TryGetPreparedPathDirection(OccupancyGrid grid,
            Vector2 position,
            int routeIndex,
            out Vector2 direction)
        {
            direction = default;
            if (routeIndex < 0
                || routeIndex >= OccupancyGrid.RouteCount
                || grid._preparedPathStamps[routeIndex] == 0
                || grid.CellCount == 0)
            {
                return false;
            }

            var routeOffset = GetRouteOffset(grid, routeIndex);
            var pathStamp = grid._preparedPathStamps[routeIndex];
            var nearestCell = grid.GetNearestCell(position);
            var nearestX = nearestCell % grid._width;
            var nearestY = nearestCell / grid._width;
            var bestCell = -1;
            var bestDistance = float.PositiveInfinity;

            const int searchRadius = 2;
            for (var y = ScalarMath.Max(0, nearestY - searchRadius);
                 y <= ScalarMath.Min(grid._height - 1, nearestY + searchRadius);
                 y++)
            {
                for (var x = ScalarMath.Max(0, nearestX - searchRadius);
                     x <= ScalarMath.Min(grid._width - 1, nearestX + searchRadius);
                     x++)
                {
                    var cell = grid.ToCell(x, y);
                    if (grid._routePathStamps[routeOffset + cell] != pathStamp)
                        continue;

                    var distance = (grid.GetCellCenter(cell) - position).LengthSquared();
                    if (distance >= bestDistance)
                        continue;

                    bestDistance = distance;
                    bestCell = cell;
                }
            }

            if (bestCell < 0)
                return false;

            var nextCell = grid._routePathNextCells[routeOffset + bestCell];
            direction = grid.GetCellCenter(nextCell) - grid.GetCellCenter(bestCell);
            return direction.LengthSquared() > ScalarMath.Epsilon;
        }

        internal static void BeginPreparedPath(OccupancyGrid grid, int routeIndex)
        {
            grid._preparedPathStamps[routeIndex]++;
            if (grid._preparedPathStamps[routeIndex] != int.MaxValue)
                return;

            System.Array.Clear(
                grid._routePathStamps,
                GetRouteOffset(grid, routeIndex),
                grid._routeCellCapacity);
            grid._preparedPathStamps[routeIndex] = 1;
        }

        internal static void TryVisitRoute(OccupancyGrid grid,
            int x,
            int y,
            int parent,
            int routeOffset,
            ref int write)
        {
            if (x < 0 || x >= grid._width || y < 0 || y >= grid._height)
                return;

            var cell = grid.ToCell(x, y);
            if (grid._visitedStamps[cell] == grid._visitStamp || grid._blockedCells[cell])
                return;

            grid._visitedStamps[cell] = grid._visitStamp;
            grid._routeParentCells[routeOffset + cell] = parent;
            grid._bfsQueue[write++] = cell;
        }

        internal static void BeginVisit(OccupancyGrid grid)
        {
            grid._visitStamp++;
            if (grid._visitStamp != int.MaxValue)
                return;

            System.Array.Clear(grid._visitedStamps, 0, grid.CellCount);
            grid._visitStamp = 1;
        }

        internal static void ResolveDirections(
            Vector2 direction,
            out int forwardX,
            out int forwardY,
            out int sideX,
            out int sideY)
        {
            if (ScalarMath.Abs(direction.X) >= ScalarMath.Abs(direction.Y))
            {
                forwardX = direction.X >= 0f ? 1 : -1;
                forwardY = 0;
                sideX = 0;
                sideY = 1;
                return;
            }

            forwardX = 0;
            forwardY = direction.Y >= 0f ? 1 : -1;
            sideX = 1;
            sideY = 0;
        }

        internal static int GetRouteOffset(OccupancyGrid grid, int routeIndex)
        {
            return routeIndex * grid._routeCellCapacity;
        }
    }
}
