using System.Numerics;

namespace ADD.Gameplay.PushResolver.Tests;

public sealed class BfsDetourTests
{
    [Test]
    public void Solve_UsesSideVacancyWhenForwardRouteIsBlocked()
    {
        var fixture = new SolverTestFixture();
        var items = new[]
        {
            SolverTestFixture.Item(1, 0f, 0f),
            SolverTestFixture.Item(2, 0.65f, 0f),
            SolverTestFixture.Item(3, 1.65f, 0f, canMove: false)
        };

        var result = fixture.Solve(
            items,
            1,
            Vector2.UnitX,
            ResolveDirectionPolicy.TryAlternates);

        fixture.AssertValidResult(items, 1, result);
        Assert.That(Math.Abs(result.Positions[2].Y), Is.GreaterThan(0.5f));
        SolverTestFixture.AssertVector(result.Positions[3], items[2].Position);
    }
}
