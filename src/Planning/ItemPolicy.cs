using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal static class ItemPolicy
    {
        internal static bool IsOccupancyMovable(BoardItem item)
        {
            return item.CanMove;
        }

        internal static bool CanBePushed(BoardItem item)
        {
            return IsOccupancyMovable(item)
                   && !item.IsPinned;
        }

        internal static bool IsFixedObstacle(BoardItem item)
        {
            return item.IsPinned;
        }
    }
}
