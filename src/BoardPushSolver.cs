using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    public sealed class BoardPushSolver
    {
        private readonly BoardPushSolverSettings _settings;

        public BoardPushSolver(BoardPushSolverSettings settings = null)
        {
            _settings = settings ?? new BoardPushSolverSettings();
        }

        public PushResult Solve(IReadOnlyList<BoardItem> items, Aabb2 boardBounds, int pinnedId,
            Vector2 preferredDirection, ResolveDirectionPolicy directionPolicy = ResolveDirectionPolicy.Directional)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (!boardBounds.TryGetBounds(out _, out _))
                throw new ArgumentException("Board bounds must be valid.", nameof(boardBounds));
            if (!float.IsFinite(preferredDirection.X) || !float.IsFinite(preferredDirection.Y))
                throw new ArgumentException("Direction must be finite.", nameof(preferredDirection));
            if (directionPolicy != ResolveDirectionPolicy.Directional && directionPolicy != ResolveDirectionPolicy.TryAlternates)
                throw new ArgumentOutOfRangeException(nameof(directionPolicy));

            var snapshot = new List<BoardItem>(items.Count);
            var ids = new HashSet<int>();
            var pinnedIndex = -1;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (!ids.Add(item.Id)) throw new ArgumentException("Item IDs must be unique.", nameof(items));
                if (item.HalfSize.X <= 0f || item.HalfSize.Y <= 0f)
                    throw new ArgumentException("Items must have positive half sizes.", nameof(items));
                if (item.Id == pinnedId)
                {
                    pinnedIndex = i;
                    item = item.Pin();
                }
                snapshot.Add(item);
            }
            if (pinnedIndex < 0 || !snapshot[pinnedIndex].CanMove)
                throw new ArgumentException("The root must identify a movable board item.", nameof(pinnedId));

            var state = new PushContext(snapshot, boardBounds, _settings, directionPolicy);
            PlanningState.EnsurePushPlanCapacity(state);
            PlanningState.RebuildGrid(state);
            var succeeded = PlanningState.FindOldestOverlappingItem(state, pinnedIndex) < 0
                || PushPlanner.TryBuildOccupancyPushPlan(state, pinnedIndex, preferredDirection);
            var positions = new Dictionary<int, Vector2>();
            if (succeeded)
            {
                for (var i = 0; i < snapshot.Count; i++)
                    positions.Add(snapshot[i].Id, PlanningState.GetPlannedPosition(state, i));
            }
            return new PushResult(succeeded, positions);
        }
    }
}
