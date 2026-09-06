using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal static class PlanningRollback
    {
        internal static void SetBoundaryPlannedPosition(PushContext state,
            int itemIndex,
            Vector2 position)
        {
            state.BoundaryPositionChanges.Add(new BoundaryPositionChange(
                itemIndex,
                PlanningState.GetPlannedPosition(state, itemIndex),
                state.HasPlannedPosition[itemIndex],
                state.PlannedPositionVersions[itemIndex]));
            PlanningState.SetPlannedPosition(state, itemIndex, position);
        }

        internal static void RestoreBoundaryPositionChanges(PushContext state, int checkpoint)
        {
            for (var i = state.BoundaryPositionChanges.Count - 1;
                 i >= checkpoint;
                 i--)
            {
                var change = state.BoundaryPositionChanges[i];
                var item = state.Items[change.ItemIndex];
                state.PlanningGrid.Insert(
                    change.ItemIndex,
                    change.PreviousPosition - item.HalfSize,
                    change.PreviousPosition + item.HalfSize);
                state.PlannedPositions[change.ItemIndex] = change.PreviousPosition;
                state.HasPlannedPosition[change.ItemIndex] = change.HadPlannedPosition;
                state.PlannedPositionVersions[change.ItemIndex] =
                    change.PreviousPositionVersion;

                if (!change.HadPlannedPosition)
                {
                    var lastIndex = state.PlannedItemIndices.Count - 1;
                    if (lastIndex >= 0
                        && state.PlannedItemIndices[lastIndex] == change.ItemIndex)
                    {
                        state.PlannedItemIndices.RemoveAt(lastIndex);
                    }
                    else
                    {
                        state.PlannedItemIndices.Remove(change.ItemIndex);
                    }
                }
            }

            if (state.BoundaryPositionChanges.Count > checkpoint)
            {
                state.BoundaryPositionChanges.RemoveRange(
                    checkpoint,
                    state.BoundaryPositionChanges.Count - checkpoint);
            }
        }

        internal static PlanningCheckpoint CapturePlanningCheckpoint(PushContext state,
            int depth,
            int requestCount)
        {
            var offset = depth * state.Items.Count;
            for (var i = 0; i < state.Items.Count; i++)
            {
                var snapshotIndex = offset + i;
                state.BacktrackingPositionSnapshots[snapshotIndex] =
                    state.PlannedPositions[i];
                state.BacktrackingPositionFlags[snapshotIndex] =
                    state.HasPlannedPosition[i];
                state.BacktrackingRouteSnapshots[snapshotIndex] =
                    state.PlannedRouteByItem[i];
                state.BacktrackingVersionSnapshots[snapshotIndex] =
                    state.PlannedPositionVersions[i];
            }

            return new PlanningCheckpoint(depth, requestCount);
        }

        internal static void RestorePlanningCheckpoint(PushContext state, PlanningCheckpoint checkpoint)
        {
            var offset = checkpoint.Depth * state.Items.Count;
            state.PlannedItemIndices.Clear();
            state.PlanningGrid.Configure(state.Settings.BroadPhaseCellSize, state.Items.Count);
            state.PlanningGrid.Clear();

            for (var i = 0; i < state.Items.Count; i++)
            {
                var snapshotIndex = offset + i;
                state.PlannedPositions[i] =
                    state.BacktrackingPositionSnapshots[snapshotIndex];
                state.HasPlannedPosition[i] =
                    state.BacktrackingPositionFlags[snapshotIndex];
                state.PlannedRouteByItem[i] =
                    state.BacktrackingRouteSnapshots[snapshotIndex];
                state.PlannedPositionVersions[i] =
                    state.BacktrackingVersionSnapshots[snapshotIndex];

                if (state.HasPlannedPosition[i])
                    state.PlannedItemIndices.Add(i);

                var item = state.Items[i];
                var position = PlanningState.GetPlannedPosition(state, i);
                state.PlanningGrid.Insert(
                    i,
                    position - item.HalfSize,
                    position + item.HalfSize);
            }

            if (state.BacktrackingRequests.Count > checkpoint.RequestCount)
            {
                state.BacktrackingRequests.RemoveRange(
                    checkpoint.RequestCount,
                    state.BacktrackingRequests.Count - checkpoint.RequestCount);
            }
        }

        internal static void EnsureBacktrackingSnapshotCapacity(PushContext state)
        {
            var required = state.Items.Count * BoardPushSolverSettings.MaxBacktrackingDepth;
            if (state.BacktrackingPositionSnapshots.Length >= required)
                return;

            System.Array.Resize(
                ref state.BacktrackingPositionSnapshots,
                required);
            System.Array.Resize(
                ref state.BacktrackingPositionFlags,
                required);
            System.Array.Resize(
                ref state.BacktrackingRouteSnapshots,
                required);
            System.Array.Resize(
                ref state.BacktrackingVersionSnapshots,
                required);
        }
    }
}
