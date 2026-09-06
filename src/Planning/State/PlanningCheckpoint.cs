using System;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal readonly struct PlanningCheckpoint
    {
        public PlanningCheckpoint(int depth, int requestCount)
        {
            Depth = depth;
            RequestCount = requestCount;
        }

        public int Depth { get; }
        public int RequestCount { get; }
    }
}
