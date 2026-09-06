using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal static class PlanValidation
    {
        internal static bool ValidateCornerEscapePlan(PushContext state, int pinnedIndex)
        {
            return (PlanningState.GetPlannedPosition(state, pinnedIndex) - state.Items[pinnedIndex].Position).LengthSquared() < 0.0000000001f
                   && ValidatePlannedPush(state, pinnedIndex);
        }

        internal static bool ValidatePlannedPush(PushContext state, int pinnedIndex)
        {
            if (HasPlannedOverlap(state, pinnedIndex))
                return false;

            for (var i = 0; i < state.PlannedItemIndices.Count; i++)
            {
                var itemIndex = state.PlannedItemIndices[i];
                var item = state.Items[itemIndex];
                if (!BoardBoundary.IsInsideBoard(state,
                        state.PlannedPositions[itemIndex],
                        item.HalfSize)
                    || HasPlannedOverlap(state, itemIndex))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool HasPlannedOverlap(PushContext state, int itemIndex)
        {
            var item = state.Items[itemIndex];
            var position = PlanningState.GetPlannedPosition(state, itemIndex);
            var padding = PushGeometry.GetBroadPhasePadding(state);
            state.PlanningGrid.CollectCandidates(
                itemIndex,
                position - item.HalfSize - padding,
                position + item.HalfSize + padding,
                state.ValidationCandidates);

            for (var i = 0; i < state.ValidationCandidates.Count; i++)
            {
                var otherIndex = state.ValidationCandidates[i];
                if (PushGeometry.IsOverlappingAtPositions(state,
                        item,
                        position,
                        state.Items[otherIndex],
                        PlanningState.GetPlannedPosition(state, otherIndex)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
