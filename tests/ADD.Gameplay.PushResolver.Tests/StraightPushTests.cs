using System.Numerics;

namespace ADD.Gameplay.PushResolver.Tests;

public sealed class StraightPushTests
{
    [Test]
    public void Solve_MovesOverlappingItemIntoForwardVacancy()
    {
        var fixture = new SolverTestFixture();
        var items = new[]
        {
            SolverTestFixture.Item(1, 0f, 0f),
            SolverTestFixture.Item(2, 0.6f, 0f)
        };

        var result = fixture.Solve(items, 1, Vector2.UnitX);

        fixture.AssertValidResult(items, 1, result);
        SolverTestFixture.AssertVector(result.Positions[1], items[0].Position);
        Assert.That(result.Positions[2].X, Is.GreaterThan(items[1].Position.X));
        Assert.That(result.Positions[2].Y, Is.EqualTo(items[1].Position.Y).Within(0.0001f));
        Assert.That(items[1].Position, Is.EqualTo(new Vector2(0.6f, 0f)));
    }
}
