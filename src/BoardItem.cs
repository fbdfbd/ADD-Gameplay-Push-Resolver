using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    public readonly struct BoardItem
    {
        public int Id { get; }
        public Vector2 Position { get; }
        public Vector2 HalfSize { get; }
        public bool IsPinned { get; }
        public bool CanMove { get; }
        public int StableSortId { get; }
        public PushPlanPolicy PlanPolicy { get; }

        public BoardItem(int id, Vector2 position, Vector2 halfSize, bool isPinned = false,
            bool canMove = true, int? stableSortId = null, PushPlanPolicy planPolicy = PushPlanPolicy.Vacancy)
        {
            if (!float.IsFinite(position.X) || !float.IsFinite(position.Y)
                || !float.IsFinite(halfSize.X) || !float.IsFinite(halfSize.Y)
                || halfSize.X <= 0f || halfSize.Y <= 0f)
                throw new ArgumentException("Item geometry must be finite and have positive half sizes.");
            if (planPolicy != PushPlanPolicy.Vacancy && planPolicy != PushPlanPolicy.MultiRoot)
                throw new ArgumentOutOfRangeException(nameof(planPolicy));
            Id = id;
            Position = position;
            HalfSize = halfSize;
            IsPinned = isPinned;
            CanMove = canMove;
            StableSortId = stableSortId ?? id;
            PlanPolicy = planPolicy;
        }

        internal BoardItem Pin() => new BoardItem(Id, Position, HalfSize, true, CanMove, StableSortId, PlanPolicy);
    }
}
