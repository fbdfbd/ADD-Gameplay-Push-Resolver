using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    public sealed class BoardPushSolverSettings
    {
        internal const int PreferredVacancyCandidatesPerRoute = 2;
        internal const int PreferredVacancyPlanAttempts = 8;
        internal const int MinPlannedStateCount = 128;
        internal const int MaxPlannedStateCount = 2048;
        internal const int MaxBacktrackingAttemptsPerDirection = 32;
        internal const int MaxBacktrackingDepth = 64;
        internal const int MaxWallRootPlanAttempts = 16;
        internal const int MaxWallRootSearchStates = 128;
        internal const int MaxCornerEscapeItems = 12;
        internal const int MaxCornerEscapeStates = 96;
        internal const float CenterAmbiguityRatio = 0.1f;
        internal const float BoardContainmentEpsilon = 0.001f;
        internal const float SeparationEpsilon = 0.001f;

        public float Padding { get; }
        public float AllowedOverlapRatio { get; }
        public bool ClampToBoardBounds { get; }
        public float BoardPadding { get; }
        public float BroadPhaseCellSize { get; }

        public BoardPushSolverSettings(float padding = 0.08f, float allowedOverlapRatio = 0f,
            bool clampToBoardBounds = false, float boardPadding = 0.1f, float broadPhaseCellSize = 1f)
        {
            if (!float.IsFinite(padding) || !float.IsFinite(allowedOverlapRatio)
                || !float.IsFinite(boardPadding) || !float.IsFinite(broadPhaseCellSize))
                throw new ArgumentException("Settings must be finite.");
            Padding = padding;
            AllowedOverlapRatio = allowedOverlapRatio;
            ClampToBoardBounds = clampToBoardBounds;
            BoardPadding = boardPadding;
            BroadPhaseCellSize = broadPhaseCellSize;
        }
    }
}
