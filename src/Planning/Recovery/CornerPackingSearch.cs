using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal static class CornerPackingSearch
    {
        internal static bool TryProcessCornerPackingState(PushContext state,
            PlannedPushRequest request,
            CornerEscapeContext context,
            bool preferHorizontal,
            ref int processedStateCount,
            int stateBudget)
        {
            var source = state.Items[request.SourceIndex];
            var moving = state.Items[request.MovingIndex];
            var sourcePosition = PlanningState.GetPlannedPosition(state, request.SourceIndex);
            var movingPosition = PlanningState.GetPlannedPosition(state, request.MovingIndex);
            if (!PushGeometry.IsOverlappingAtPositions(state,
                    source,
                    sourcePosition,
                    moving,
                    movingPosition))
            {
                return true;
            }

            if (!ItemPolicy.CanBePushed(moving))
                return false;

            var packingState = new CornerPackingState(
                request.SourceIndex,
                request.MovingIndex,
                PushDirections.GetDirectionIndex(request.Direction),
                sourcePosition,
                movingPosition);
            if (!state.ActiveCornerPackingStates.Add(packingState))
                return false;

            try
            {
                for (var candidateIndex = 0; candidateIndex < 2; candidateIndex++)
                {
                    if (++processedStateCount > stateBudget)
                        return false;

                    var candidateDirection = context.GetCandidateDirection(
                        request.Direction,
                        preferHorizontal,
                        candidateIndex);
                    Vector2 target;
                    if (!PushGeometry.TryGetMinimumSeparationPosition(state,
                            sourcePosition,
                            source,
                            movingPosition,
                            moving,
                            candidateDirection,
                            out target))
                    {
                        target = PushGeometry.GetAdjacentSeparationPosition(state,
                            sourcePosition,
                            source,
                            movingPosition,
                            moving,
                            candidateDirection);
                    }

                    if (!IsMonotonicMove(
                            movingPosition,
                            target,
                            candidateDirection)
                        || !BoardBoundary.IsInsideBoard(state, target, moving.HalfSize)
                        || !PushGeometry.IsWithinLocalMoveLimit(state,
                            moving.Position,
                            target,
                            source,
                            moving,
                            allowSideChange: true))
                    {
                        continue;
                    }

                    var positionCheckpoint = state.BoundaryPositionChanges.Count;
                    var requestCheckpoint = state.BacktrackingRequests.Count;
                    var islandCheckpoint = state.BoundaryIslandItems.Count;
                    PlanningState.CollectPlannedBlockers(state,
                        request.MovingIndex,
                        target,
                        moving,
                        request.SourceIndex);

                    var isBlocked = false;
                    PlanningState.SortPlannedBlockersOldestFirst(state);
                    for (var i = 0; i < state.PlannedBlockers.Count; i++)
                    {
                        var blockerIndex = state.PlannedBlockers[i];
                        if (ItemPolicy.IsFixedObstacle(state.Items[blockerIndex]))
                        {
                            isBlocked = true;
                            break;
                        }

                        if (state.BoundaryIslandSet.Add(blockerIndex))
                        {
                            state.BoundaryIslandItems.Add(blockerIndex);
                            if (state.BoundaryIslandItems.Count
                                > BoardPushSolverSettings.MaxCornerEscapeItems)
                            {
                                isBlocked = true;
                                break;
                            }
                        }

                        state.BacktrackingRequests.Add(new PlannedPushRequest(
                            request.MovingIndex,
                            blockerIndex,
                            candidateDirection,
                            PushDirections.GetDirectionIndex(candidateDirection),
                            forceMove: false,
                            useVacancyPath: false));
                    }

                    if (!isBlocked)
                    {
                        PlanningRollback.SetBoundaryPlannedPosition(state,
                            request.MovingIndex,
                            target);

                        var requestEnd = state.BacktrackingRequests.Count;
                        for (var i = requestCheckpoint;
                             i < requestEnd;
                             i++)
                        {
                            if (!TryProcessCornerPackingState(state,
                                    state.BacktrackingRequests[i],
                                    context,
                                    preferHorizontal,
                                    ref processedStateCount,
                                    stateBudget))
                            {
                                isBlocked = true;
                                break;
                            }
                        }
                    }

                    if (state.BacktrackingRequests.Count > requestCheckpoint)
                    {
                        state.BacktrackingRequests.RemoveRange(
                            requestCheckpoint,
                            state.BacktrackingRequests.Count - requestCheckpoint);
                    }

                    if (!isBlocked
                        && !PushGeometry.IsOverlappingAtPositions(state,
                            source,
                            PlanningState.GetPlannedPosition(state, request.SourceIndex),
                            moving,
                            PlanningState.GetPlannedPosition(state, request.MovingIndex)))
                    {
                        return true;
                    }

                    PlanningRollback.RestoreBoundaryPositionChanges(state, positionCheckpoint);
                    CornerIsland.RestoreCornerEscapeIsland(state, islandCheckpoint);
                }

                return false;
            }
            finally
            {
                state.ActiveCornerPackingStates.Remove(packingState);
            }
        }

        internal static bool IsMonotonicMove(
            Vector2 current,
            Vector2 target,
            Vector2 direction)
        {
            return Vector2.Dot(target - current, direction)
                   > BoardPushSolverSettings.SeparationEpsilon;
        }
    }
}
