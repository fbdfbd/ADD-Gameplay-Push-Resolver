using System;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal readonly struct PlannedPushRequest
    {
        public PlannedPushRequest(
            int sourceIndex,
            int movingIndex,
            Vector2 direction,
            int routeIndex,
            bool forceMove,
            bool useVacancyPath,
            bool allowBoundaryChainMove = false)
        {
            SourceIndex = sourceIndex;
            MovingIndex = movingIndex;
            Direction = direction;
            RouteIndex = routeIndex;
            ForceMove = forceMove;
            UseVacancyPath = useVacancyPath;
            AllowBoundaryChainMove = allowBoundaryChainMove;
        }

        public int SourceIndex { get; }
        public int MovingIndex { get; }
        public Vector2 Direction { get; }
        public int RouteIndex { get; }
        public bool ForceMove { get; }
        public bool UseVacancyPath { get; }
        public bool AllowBoundaryChainMove { get; }
    }
}
