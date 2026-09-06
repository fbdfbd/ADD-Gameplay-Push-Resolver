using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal static class PlanningState
    {
        internal static void EnqueuePlannedPush(PushContext state, PlannedPushRequest request)
        {
            var stateKey = GetPlannedPushStateKey(state, request);
            if (!state.VisitedPlannedPushStates.Add(stateKey))
                return;

            state.PlannedPushQueue.Enqueue(request);
        }

        internal static ulong GetPlannedPushStateKey(PushContext state, PlannedPushRequest request)
        {
            var directionIndex = ScalarMath.Abs(request.Direction.X)
                                 >= ScalarMath.Abs(request.Direction.Y)
                ? request.Direction.X >= 0f ? 0 : 1
                : request.Direction.Y >= 0f ? 2 : 3;
            var relationshipKey = ((ulong)(uint)request.SourceIndex << 35)
                                  | ((ulong)(uint)request.MovingIndex << 5)
                                  | ((ulong)(uint)directionIndex << 3)
                                  | ((ulong)(uint)request.RouteIndex << 1)
                                  | (request.UseVacancyPath ? 1UL : 0UL);
            if (request.AllowBoundaryChainMove)
                relationshipKey ^= 0xA0761D6478BD642FUL;

            var sourceVersion = (ulong)(uint)
                state.PlannedPositionVersions[request.SourceIndex];
            var movingVersion = (ulong)(uint)
                state.PlannedPositionVersions[request.MovingIndex];

            return MixHash(
                relationshipKey
                ^ sourceVersion * 0x9E3779B97F4A7C15UL
                ^ movingVersion * 0xD6E8FEB86659FD93UL);
        }

        internal static void RebuildPlanningGrid(PushContext state)
        {
            state.PlanningGrid.Configure(state.Settings.BroadPhaseCellSize, state.Items.Count);
            state.PlanningGrid.Clear();
            for (var i = 0; i < state.Items.Count; i++)
            {
                var item = state.Items[i];
                state.PlanningGrid.Insert(
                    i,
                    item.Position - item.HalfSize,
                    item.Position + item.HalfSize);
            }
        }

        internal static void InitializePlanningGrid(PushContext state)
        {
            EnsurePushPlanCapacity(state);
            System.Array.Clear(state.HasPlannedPosition, 0, state.Items.Count);
            System.Array.Clear(state.PlannedPositionVersions, 0, state.Items.Count);
            for (var i = 0; i < state.Items.Count; i++)
                state.PlannedRouteByItem[i] = -1;

            state.PlannedItemIndices.Clear();
            RebuildPlanningGrid(state);
        }

        internal static void ResetPlanningState(PushContext state)
        {
            for (var i = 0; i < state.PlannedItemIndices.Count; i++)
            {
                var itemIndex = state.PlannedItemIndices[i];
                var item = state.Items[itemIndex];
                state.PlanningGrid.Insert(
                    itemIndex,
                    item.Position - item.HalfSize,
                    item.Position + item.HalfSize);
                state.HasPlannedPosition[itemIndex] = false;
                state.PlannedRouteByItem[itemIndex] = -1;
                state.PlannedPositionVersions[itemIndex] = 0;
            }

            state.PlannedItemIndices.Clear();
        }

        internal static void CollectPlannedBlockers(PushContext state,
            int movingIndex,
            Vector2 target,
            BoardItem moving,
            int ignoredIndex = -1)
        {
            state.PlannedBlockers.Clear();
            var padding = PushGeometry.GetBroadPhasePadding(state);
            state.PlanningGrid.CollectCandidates(
                movingIndex,
                target - moving.HalfSize - padding,
                target + moving.HalfSize + padding,
                state.NeighborCandidates);

            for (var i = 0; i < state.NeighborCandidates.Count; i++)
            {
                var candidateIndex = state.NeighborCandidates[i];
                if (candidateIndex == ignoredIndex)
                    continue;

                var candidate = state.Items[candidateIndex];
                if (PushGeometry.IsOverlappingAtPositions(state,
                        moving,
                        target,
                        candidate,
                        GetPlannedPosition(state, candidateIndex)))
                {
                    state.PlannedBlockers.Add(candidateIndex);
                }
            }
        }

        internal static void SetPlannedPosition(PushContext state, int itemIndex, Vector2 position)
        {
            var previousPosition = GetPlannedPosition(state, itemIndex);
            if (!state.HasPlannedPosition[itemIndex])
            {
                state.HasPlannedPosition[itemIndex] = true;
                state.PlannedItemIndices.Add(itemIndex);
            }

            state.PlannedPositions[itemIndex] = position;
            if ((position - previousPosition).LengthSquared() > 0.0000001f)
                IncrementPlannedPositionVersion(state, itemIndex);

            var item = state.Items[itemIndex];
            state.PlanningGrid.Insert(
                itemIndex,
                position - item.HalfSize,
                position + item.HalfSize);
        }

        internal static void IncrementPlannedPositionVersion(PushContext state, int itemIndex)
        {
            if (state.PlannedPositionVersions[itemIndex] == int.MaxValue)
            {
                state.PlannedPositionVersions[itemIndex] = 1;
                return;
            }

            state.PlannedPositionVersions[itemIndex]++;
        }

        internal static Vector2 GetPlannedPosition(PushContext state, int itemIndex)
        {
            return state.HasPlannedPosition[itemIndex]
                ? state.PlannedPositions[itemIndex]
                : state.Items[itemIndex].Position;
        }

        internal static void EnsurePushPlanCapacity(PushContext state)
        {
            if (state.PlannedPositions.Length >= state.Items.Count)
                return;

            var capacity = state.PlannedPositions.Length;
            while (capacity < state.Items.Count)
                capacity *= 2;

            System.Array.Resize(ref state.PlannedPositions, capacity);
            System.Array.Resize(ref state.HasPlannedPosition, capacity);
            System.Array.Resize(ref state.PlannedRouteByItem, capacity);
            System.Array.Resize(ref state.PlannedPositionVersions, capacity);
            System.Array.Resize(ref state.BestBoundaryPositions, capacity);
        }

        internal static void SortPlannedBlockersOldestFirst(PushContext state)
        {
            state.PlannedBlockers.Sort(state.ComparePlannedOldestFirst);
        }

        internal static void SortPlannedBlockersNewestFirst(PushContext state)
        {
            state.PlannedBlockers.Sort(state.ComparePlannedNewestFirst);
        }

        internal static int ComparePlannedOldestFirst(PushContext state, int a, int b)
        {
            return state.Items[a].StableSortId.CompareTo(state.Items[b].StableSortId);
        }

        internal static int ComparePlannedNewestFirst(PushContext state, int a, int b)
        {
            return state.Items[b].StableSortId.CompareTo(state.Items[a].StableSortId);
        }

        internal static int FindOldestOverlappingItem(PushContext state, int sourceIndex)
        {
            var source = state.Items[sourceIndex];
            var padding = PushGeometry.GetBroadPhasePadding(state);
            state.Grid.CollectCandidates(
                sourceIndex,
                source.Position - source.HalfSize - padding,
                source.Position + source.HalfSize + padding,
                state.NeighborCandidates);

            var oldestIndex = -1;
            for (var i = 0; i < state.NeighborCandidates.Count; i++)
            {
                var candidateIndex = state.NeighborCandidates[i];
                var candidate = state.Items[candidateIndex];
                if (candidate.IsPinned
                    || !PushGeometry.IsOverlapping(state, source, candidate))
                {
                    continue;
                }

                if (oldestIndex < 0
                    || candidate.StableSortId < state.Items[oldestIndex].StableSortId)
                {
                    oldestIndex = candidateIndex;
                }
            }

            return oldestIndex;
        }

        internal static void RebuildGrid(PushContext state)
        {
            state.Grid.Configure(state.Settings.BroadPhaseCellSize, state.Items.Count);
            state.Grid.Clear();

            for (var i = 0; i < state.Items.Count; i++)
            {
                var item = state.Items[i];
                state.Grid.Insert(
                    i,
                    item.Position - item.HalfSize,
                    item.Position + item.HalfSize);
            }
        }

        internal static ulong MixHash(ulong value)
        {
            value ^= value >> 30;
            value *= 0xBF58476D1CE4E5B9UL;
            value ^= value >> 27;
            value *= 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }
}
