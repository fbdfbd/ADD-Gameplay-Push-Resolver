using System;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal readonly struct WallRootOptions
    {
        public WallRootOptions(
            int movingIndex,
            WallRootCandidate firstCandidate,
            WallRootCandidate secondCandidate,
            WallRootCandidate thirdCandidate,
            int candidateCount)
        {
            MovingIndex = movingIndex;
            FirstCandidate = firstCandidate;
            SecondCandidate = secondCandidate;
            ThirdCandidate = thirdCandidate;
            CandidateCount = candidateCount;
        }

        public int MovingIndex { get; }
        public WallRootCandidate FirstCandidate { get; }
        public WallRootCandidate SecondCandidate { get; }
        public WallRootCandidate ThirdCandidate { get; }
        public int CandidateCount { get; }

        public WallRootCandidate GetCandidate(int index)
        {
            switch (index)
            {
                case 0:
                    return FirstCandidate;
                case 1:
                    return SecondCandidate;
                default:
                    return ThirdCandidate;
            }
        }
    }
}
