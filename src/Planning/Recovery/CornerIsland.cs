using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal static class CornerIsland
    {
        internal static bool TryGetCornerEscapeContext(PushContext state,
            int pinnedIndex,
            Vector2 preferredDirection,
            out CornerEscapeContext context)
        {
            context = default;
            if (!state.Settings.ClampToBoardBounds
                || !state.BoardBounds.TryGetBounds(out _, out _)
                || PlanningState.FindOldestOverlappingItem(state, pinnedIndex) < 0)
            {
                return false;
            }

            state.BoundaryIslandItems.Clear();
            state.BoundaryIslandSet.Clear();
            state.PushWaveQueue.Clear();

            state.BoundaryIslandItems.Add(pinnedIndex);
            state.BoundaryIslandSet.Add(pinnedIndex);
            state.PushWaveQueue.Enqueue(pinnedIndex);

            const float contactEpsilon = 0.001f;
            while (state.PushWaveQueue.Count > 0)
            {
                var itemIndex = state.PushWaveQueue.Dequeue();
                var item = state.Items[itemIndex];

                var searchPadding = PushGeometry.GetBroadPhasePadding(state)
                                    + Vector2.One * contactEpsilon;
                state.Grid.CollectCandidates(
                    itemIndex,
                    item.Position - item.HalfSize - searchPadding,
                    item.Position + item.HalfSize + searchPadding,
                    state.NeighborCandidates);

                for (var i = 0; i < state.NeighborCandidates.Count; i++)
                {
                    var candidateIndex = state.NeighborCandidates[i];
                    if (state.BoundaryIslandSet.Contains(candidateIndex))
                        continue;

                    var candidate = state.Items[candidateIndex];
                    if (!ItemPolicy.IsOccupancyMovable(candidate)
                        || !PushGeometry.IsPushConnected(state,
                            item,
                            candidate,
                            contactEpsilon))
                    {
                        continue;
                    }

                    state.BoundaryIslandSet.Add(candidateIndex);
                    state.BoundaryIslandItems.Add(candidateIndex);
                    if (state.BoundaryIslandItems.Count > BoardPushSolverSettings.MaxCornerEscapeItems)
                        return false;

                    state.PushWaveQueue.Enqueue(candidateIndex);
                }
            }

            var boundaryMask = BoardBoundaryMask.None;
            var wallTolerance = ScalarMath.Max(
                0.01f,
                state.Settings.Padding + BoardPushSolverSettings.BoardContainmentEpsilon);
            for (var i = 0; i < state.BoundaryIslandItems.Count; i++)
            {
                boundaryMask |= BoardBoundary.GetBoundaryMask(state,
                    state.Items[state.BoundaryIslandItems[i]],
                    wallTolerance);
            }

            var horizontalBoundary = BoardBoundary.SelectHorizontalBoundary(state,
                boundaryMask,
                state.Items[pinnedIndex]);
            var verticalBoundary = BoardBoundary.SelectVerticalBoundary(state,
                boundaryMask,
                state.Items[pinnedIndex]);
            if (horizontalBoundary == BoardBoundaryMask.None
                || verticalBoundary == BoardBoundaryMask.None)
            {
                return false;
            }

            var horizontalOutlet =
                horizontalBoundary == BoardBoundaryMask.Left
                    ? Vector2.UnitX
                    : (-Vector2.UnitX);
            var verticalOutlet =
                verticalBoundary == BoardBoundaryMask.Bottom
                    ? Vector2.UnitY
                    : (-Vector2.UnitY);
            var preferHorizontal =
                ScalarMath.Abs(Vector2.Dot(
                    preferredDirection,
                    horizontalOutlet))
                >= ScalarMath.Abs(Vector2.Dot(
                    preferredDirection,
                    verticalOutlet));
            context = new CornerEscapeContext(
                horizontalBoundary | verticalBoundary,
                horizontalOutlet,
                verticalOutlet,
                preferHorizontal);
            return true;
        }

        internal static void RestoreCornerEscapeIsland(PushContext state, int checkpoint)
        {
            for (var i = state.BoundaryIslandItems.Count - 1;
                 i >= checkpoint;
                 i--)
            {
                state.BoundaryIslandSet.Remove(state.BoundaryIslandItems[i]);
            }

            if (state.BoundaryIslandItems.Count > checkpoint)
            {
                state.BoundaryIslandItems.RemoveRange(
                    checkpoint,
                    state.BoundaryIslandItems.Count - checkpoint);
            }
        }
    }
}
