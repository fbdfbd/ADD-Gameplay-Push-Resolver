using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal static class BoardBoundary
    {
        internal static BoardBoundaryMask GetBoundaryMask(PushContext state,
            BoardItem item,
            float tolerance)
        {
            if (!state.Settings.ClampToBoardBounds
                || !state.BoardBounds.TryGetBounds(out var min, out var max))
            {
                return BoardBoundaryMask.None;
            }

            var padding = state.Settings.BoardPadding;
            var result = BoardBoundaryMask.None;
            if (item.Position.X - item.HalfSize.X - padding - min.X
                <= tolerance)
            {
                result |= BoardBoundaryMask.Left;
            }

            if (max.X - item.Position.X - item.HalfSize.X - padding
                <= tolerance)
            {
                result |= BoardBoundaryMask.Right;
            }

            if (item.Position.Y - item.HalfSize.Y - padding - min.Y
                <= tolerance)
            {
                result |= BoardBoundaryMask.Bottom;
            }

            if (max.Y - item.Position.Y - item.HalfSize.Y - padding
                <= tolerance)
            {
                result |= BoardBoundaryMask.Top;
            }

            return result;
        }

        internal static BoardBoundaryMask SelectHorizontalBoundary(PushContext state,
            BoardBoundaryMask mask,
            BoardItem pinned)
        {
            var hasLeft = (mask & BoardBoundaryMask.Left) != 0;
            var hasRight = (mask & BoardBoundaryMask.Right) != 0;
            if (!hasLeft)
                return hasRight
                    ? BoardBoundaryMask.Right
                    : BoardBoundaryMask.None;
            if (!hasRight)
                return BoardBoundaryMask.Left;

            if (!state.BoardBounds.TryGetBounds(out var min, out var max))
            {
                return BoardBoundaryMask.None;
            }

            return pinned.Position.X - min.X
                   <= max.X - pinned.Position.X
                ? BoardBoundaryMask.Left
                : BoardBoundaryMask.Right;
        }

        internal static BoardBoundaryMask SelectVerticalBoundary(PushContext state,
            BoardBoundaryMask mask,
            BoardItem pinned)
        {
            var hasBottom = (mask & BoardBoundaryMask.Bottom) != 0;
            var hasTop = (mask & BoardBoundaryMask.Top) != 0;
            if (!hasBottom)
                return hasTop
                    ? BoardBoundaryMask.Top
                    : BoardBoundaryMask.None;
            if (!hasTop)
                return BoardBoundaryMask.Bottom;

            if (!state.BoardBounds.TryGetBounds(out var min, out var max))
            {
                return BoardBoundaryMask.None;
            }

            return pinned.Position.Y - min.Y
                   <= max.Y - pinned.Position.Y
                ? BoardBoundaryMask.Bottom
                : BoardBoundaryMask.Top;
        }

        internal static bool IsInsideBoard(PushContext state, Vector2 center, Vector2 halfSize)
        {
            if (!state.Settings.ClampToBoardBounds
                || !state.BoardBounds.TryGetBounds(out var min, out var max))
            {
                return true;
            }

            var padding = state.Settings.BoardPadding;
            return center.X - halfSize.X - padding
                       >= min.X - BoardPushSolverSettings.BoardContainmentEpsilon
                   && center.X + halfSize.X + padding
                       <= max.X + BoardPushSolverSettings.BoardContainmentEpsilon
                   && center.Y - halfSize.Y - padding
                       >= min.Y - BoardPushSolverSettings.BoardContainmentEpsilon
                   && center.Y + halfSize.Y + padding
                       <= max.Y + BoardPushSolverSettings.BoardContainmentEpsilon;
        }
    }
}
