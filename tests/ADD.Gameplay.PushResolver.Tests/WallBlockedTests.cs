using System.Numerics;

namespace ADD.Gameplay.PushResolver.Tests;

public sealed class WallBlockedTests
{
    [Test]
    public void Solve_ChoosesAlternateDirectionAtBoardWall()
    {
        var fixture = new SolverTestFixture(boardHalfExtent: 2f);
        var items = new[]
        {
            SolverTestFixture.Item(1, 1f, 0f),
            SolverTestFixture.Item(2, 1.4f, 0f)
        };

        var result = fixture.Solve(
            items,
            1,
            Vector2.UnitX,
            ResolveDirectionPolicy.TryAlternates);

        fixture.AssertValidResult(items, 1, result);
        Assert.That(result.Positions[2].X, Is.LessThanOrEqualTo(1.4001f));
        Assert.That(Math.Abs(result.Positions[2].Y), Is.GreaterThan(0.5f));
    }
}
