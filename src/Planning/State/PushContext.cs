using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal sealed class PushContext
    {
        internal readonly BoardPushSolverSettings Settings;
        internal readonly List<BoardItem> Items;
        internal readonly SpatialGrid Grid = new();
        internal readonly OccupancyGrid OccupancyGrid = new();
        internal readonly List<int> NeighborCandidates = new(32);
        internal readonly List<int> ValidationCandidates = new(32);
        internal readonly SpatialGrid PlanningGrid = new();
        internal readonly Queue<PlannedPushRequest> PlannedPushQueue = new(4096);
        internal readonly HashSet<ulong> VisitedPlannedPushStates = new(4096);
        internal readonly List<int> PlannedItemIndices = new(512);
        internal readonly List<int> PlannedBlockers = new(16);
        internal readonly List<PlannedPushRequest> BacktrackingRequests = new(512);
        internal readonly List<int>[] VacancyCandidatesByRoute =
        {
            new(BoardPushSolverSettings.PreferredVacancyCandidatesPerRoute),
            new(BoardPushSolverSettings.PreferredVacancyCandidatesPerRoute),
            new(BoardPushSolverSettings.PreferredVacancyCandidatesPerRoute),
            new(BoardPushSolverSettings.PreferredVacancyCandidatesPerRoute)
        };
        internal readonly Vector2[] PushDirections = new Vector2[4];
        internal readonly Vector2[] ResolveAttemptDirections = new Vector2[4];
        internal readonly int[] RootPushDirectionCounts = new int[4];
        internal readonly List<WallRootOptions> WallRootOptions = new(16);
        internal WallRootCandidate[] WallSelectedCandidates =
            new WallRootCandidate[16];
        internal Vector2[] PlannedPositions = new Vector2[512];
        internal bool[] HasPlannedPosition = new bool[512];
        internal int[] PlannedRouteByItem = new int[512];
        internal int[] PlannedPositionVersions = new int[512];
        internal Vector2[] BestBoundaryPositions = new Vector2[512];
        internal Vector2[] BacktrackingPositionSnapshots = System.Array.Empty<Vector2>();
        internal bool[] BacktrackingPositionFlags = System.Array.Empty<bool>();
        internal int[] BacktrackingRouteSnapshots = System.Array.Empty<int>();
        internal int[] BacktrackingVersionSnapshots = System.Array.Empty<int>();
        internal readonly Queue<int> PushWaveQueue = new(512);
        internal readonly List<int> BoundaryIslandItems = new(512);
        internal readonly HashSet<int> BoundaryIslandSet = new(512);
        internal readonly List<int> BestBoundaryItemIndices = new(512);
        internal readonly List<BoundaryPositionChange> BoundaryPositionChanges = new(512);
        internal readonly HashSet<CornerPackingState> ActiveCornerPackingStates =
            new(128);
        internal readonly System.Comparison<int> ComparePlannedOldestFirst;
        internal readonly System.Comparison<int> ComparePlannedNewestFirst;
        internal ResolveDirectionPolicy ResolveDirectionPolicy;
        internal bool HasPreparedVacancyPath;
        internal readonly Aabb2 BoardBounds;
        internal PushContext(List<BoardItem> items, Aabb2 bounds, BoardPushSolverSettings settings, ResolveDirectionPolicy directionPolicy)
        {
            Items = items;
            BoardBounds = bounds;
            Settings = settings;
            ResolveDirectionPolicy = directionPolicy;
            ComparePlannedOldestFirst = (a, b) => PlanningState.ComparePlannedOldestFirst(this, a, b);
            ComparePlannedNewestFirst = (a, b) => PlanningState.ComparePlannedNewestFirst(this, a, b);
        }
    }
}
