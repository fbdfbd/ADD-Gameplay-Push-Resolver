using System.Numerics;

namespace ADD.Gameplay.PushResolver.Tests;

internal sealed class SolverTestFixture
{
    private const float Tolerance = 0.0001f;

    public Aabb2 Bounds { get; }
    public BoardPushSolverSettings Settings { get; }
    public BoardPushSolver Solver { get; }

    public SolverTestFixture(float boardHalfExtent = 5f, float padding = 0.08f,
        float boardPadding = 0.1f, bool clampToBoardBounds = true)
    {
        Bounds = new Aabb2(
            new Vector2(-boardHalfExtent, -boardHalfExtent),
            new Vector2(boardHalfExtent, boardHalfExtent));
        Settings = new BoardPushSolverSettings(
            padding: padding,
            clampToBoardBounds: clampToBoardBounds,
            boardPadding: boardPadding);
        Solver = new BoardPushSolver(Settings);
    }

    public static BoardItem Item(int id, float x, float y,
        float halfWidth = 0.5f, float halfHeight = 0.5f,
        bool isPinned = false, bool canMove = true)
    {
        return new BoardItem(
            id,
            new Vector2(x, y),
            new Vector2(halfWidth, halfHeight),
            isPinned,
            canMove);
    }

    public PushResult Solve(IReadOnlyList<BoardItem> items, int pinnedId,
        Vector2 preferredDirection,
        ResolveDirectionPolicy directionPolicy = ResolveDirectionPolicy.Directional)
    {
        return Solver.Solve(items, Bounds, pinnedId, preferredDirection, directionPolicy);
    }

    public void AssertValidResult(IReadOnlyList<BoardItem> items, int pinnedId, PushResult result)
    {
        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Positions, Has.Count.EqualTo(items.Count));

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            Assert.That(result.Positions, Contains.Key(item.Id));
            var position = result.Positions[item.Id];

            if (item.Id == pinnedId || item.IsPinned || !item.CanMove)
                AssertVector(position, item.Position);

            if (Settings.ClampToBoardBounds)
            {
                Assert.That(position.X - item.HalfSize.X - Settings.BoardPadding,
                    Is.GreaterThanOrEqualTo(Bounds.Min.X - Tolerance));
                Assert.That(position.X + item.HalfSize.X + Settings.BoardPadding,
                    Is.LessThanOrEqualTo(Bounds.Max.X + Tolerance));
                Assert.That(position.Y - item.HalfSize.Y - Settings.BoardPadding,
                    Is.GreaterThanOrEqualTo(Bounds.Min.Y - Tolerance));
                Assert.That(position.Y + item.HalfSize.Y + Settings.BoardPadding,
                    Is.LessThanOrEqualTo(Bounds.Max.Y + Tolerance));
            }
        }

        for (var i = 0; i < items.Count; i++)
        {
            for (var j = i + 1; j < items.Count; j++)
                AssertSeparated(items[i], result.Positions[items[i].Id], items[j], result.Positions[items[j].Id]);
        }
    }

    public static void AssertVector(Vector2 actual, Vector2 expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.X, Is.EqualTo(expected.X).Within(Tolerance));
            Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(Tolerance));
        });
    }

    private void AssertSeparated(BoardItem first, Vector2 firstPosition,
        BoardItem second, Vector2 secondPosition)
    {
        var ratio = Math.Clamp(Settings.AllowedOverlapRatio, 0f, 1f);
        var margin = ratio <= 0f
            ? new Vector2(Settings.Padding, Settings.Padding)
            : new Vector2(
                -Math.Min(first.HalfSize.X, second.HalfSize.X) * 2f * ratio,
                -Math.Min(first.HalfSize.Y, second.HalfSize.Y) * 2f * ratio);
        var overlapX = first.HalfSize.X + second.HalfSize.X + margin.X
                       - Math.Abs(firstPosition.X - secondPosition.X);
        var overlapY = first.HalfSize.Y + second.HalfSize.Y + margin.Y
                       - Math.Abs(firstPosition.Y - secondPosition.Y);

        Assert.That(overlapX <= Tolerance || overlapY <= Tolerance, Is.True,
            $"Items {first.Id} and {second.Id} overlap at {firstPosition} and {secondPosition}.");
    }
}
